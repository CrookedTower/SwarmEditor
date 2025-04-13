using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using Swarm.Lsp.Configuration;
using Swarm.Lsp.ServerManagement;

namespace Swarm.Lsp.Services
{
    /// <summary>
    /// Concrete implementation of the ILspService.
    /// Manages LSP client instances and server processes.
    /// </summary>
    public class LspService : ILspService
    {
        private readonly ILogger<LspService> _logger;
        private readonly ILspConfigProvider _configProvider;
        private readonly ILspServerManager _serverManager;

        // Unique key for each server instance (workspace + language)
        private record struct ServerKey(string WorkspaceRoot, string LanguageId);

        // Dictionary to hold active language clients
        private readonly ConcurrentDictionary<ServerKey, ILanguageClient> _languageClients = new();

        // TODO: Use a proper DI container passed in
        private readonly IServiceProvider _serviceProvider; 

        // Event for diagnostics notifications
        public event EventHandler<DiagnosticsEventArgs>? DiagnosticsPublished;

        public LspService(ILogger<LspService> logger, ILspConfigProvider configProvider, ILspServerManager serverManager)
        {
            _logger = logger;
            _configProvider = configProvider;
            _serverManager = serverManager;

            // Placeholder for proper DI - needed for LanguageClient.Create
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace)); // Configure logging for LSP client
            _serviceProvider = serviceCollection.BuildServiceProvider();
            _logger.LogInformation("LspService created.");
        }

        public async Task InitializeForDocumentAsync(string documentPath, string languageId, string initialContent)
        {
            _logger.LogInformation("Initialize requested for: {DocumentPath} ({LanguageId})", documentPath, languageId);
            var settings = _configProvider.GetSettingsForLanguage(languageId);
            if (settings == null)
            {
                _logger.LogWarning("No enabled settings found for language: {LanguageId}", languageId);
                return;
            }

            string workspaceRoot = FindWorkspaceRoot(documentPath) ?? Path.GetDirectoryName(documentPath) ?? ".";
            var serverKey = new ServerKey(workspaceRoot, languageId);

            if (_languageClients.ContainsKey(serverKey))
            {
                _logger.LogInformation("Client already exists for {ServerKey}. Ensuring document is opened.", serverKey);
                var existingClient = _languageClients[serverKey];
                SendDidOpenNotification(existingClient, documentPath, languageId, initialContent);
                return;
            }

            _logger.LogInformation("Attempting to ensure server {ServerName} v{Version} is installed for {LanguageId}", settings.ServerName, settings.Version, languageId);
            string? serverExecutablePath = await _serverManager.EnsureServerInstalledAsync(languageId);

            if (string.IsNullOrEmpty(serverExecutablePath))
            {
                _logger.LogError("Failed to ensure installation of server for language: {LanguageId}", languageId);
                return;
            }

            _logger.LogInformation("Server executable path: {ServerPath}", serverExecutablePath);

            try
            {
                var client = await StartLanguageClientAsync(settings, serverKey, serverExecutablePath);
                if (client != null && _languageClients.TryAdd(serverKey, client))
                {
                    _logger.LogInformation("Successfully started and registered client for {ServerKey}", serverKey);
                    // Send initial didOpen now that client is ready
                    SendDidOpenNotification(client, documentPath, languageId, initialContent);
                }
                else if (client != null)
                {
                     _logger.LogWarning("Client for {ServerKey} was started but already existed in dictionary? Disposing new client.", serverKey);
                     client.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start language client for {ServerKey}", serverKey);
            }
        }

        private async Task<ILanguageClient?> StartLanguageClientAsync(LspServerSettings settings, ServerKey serverKey, string serverExecutablePath)
        {
            _logger.LogInformation("Creating LanguageClient for {ServerKey}", serverKey);

            // Replace placeholders in arguments
            string arguments = settings.Arguments?.Replace("{WorkspaceRoot}", serverKey.WorkspaceRoot) ?? "";
            arguments = arguments.Replace("{HostProcessId}", Process.GetCurrentProcess().Id.ToString());

            // Create ProcessStartInfo beforehand
            var processStartInfo = new ProcessStartInfo
            {
                FileName = serverExecutablePath,
                Arguments = arguments,
                WorkingDirectory = serverKey.WorkspaceRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true // Keep redirecting error stream for logging/debugging
            };
            _logger.LogInformation("Configuring client process: {FileName} {Arguments} in {WorkingDirectory}", serverExecutablePath, arguments, serverKey.WorkspaceRoot);

            // Start the process
            var process = new Process { StartInfo = processStartInfo };
            if (!process.Start())
            {
                _logger.LogError("Failed to start language server process: {FileName}", serverExecutablePath);
                return null;
            }
            _logger.LogInformation("Language server process started with ID: {ProcessId}", process.Id);

            // TODO: Add error handling for process exit
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => 
            {
                _logger.LogWarning("Language server process {ProcessId} exited.", process.Id);
                // TODO: Trigger reconnection or notify the user?
                // Consider removing the client from _languageClients dictionary
            };

            // Capture stderr for logging
            Task.Run(() => LogStreamAsync(process.StandardError, "LSP_ERR"));


            // Configure LanguageClient using Create and stream redirection
            var client = LanguageClient.Create(options =>
            {
                options.WithInput(process.StandardOutput.BaseStream) // Connect to process stdout
                       .WithOutput(process.StandardInput.BaseStream) // Connect to process stdin
                       .WithRootUri(new Uri(serverKey.WorkspaceRoot))
                       .WithRootPath(serverKey.WorkspaceRoot)
                       .WithInitializationOptions(null)
                       .WithServices(services => services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace))) // Keep internal client logging
                       // Register client capabilities
                       .WithCapability(new CompletionCapability 
                       { 
                           DynamicRegistration = true,
                           CompletionItem = new CompletionItemCapabilityOptions 
                           {
                               SnippetSupport = true,
                               CommitCharactersSupport = true,
                               DocumentationFormat = new[] { MarkupKind.Markdown, MarkupKind.PlainText },
                               DeprecatedSupport = true,
                               TagSupport = new CompletionItemTagSupportCapabilityOptions
                               {
                                   ValueSet = new[] { CompletionItemTag.Deprecated }
                               }
                           }
                       })
                       .WithCapability(new HoverCapability 
                       { 
                           DynamicRegistration = true,
                           ContentFormat = new[] { MarkupKind.Markdown, MarkupKind.PlainText }
                       })
                       .WithCapability(new DefinitionCapability { DynamicRegistration = true })
                       // Add more common capabilities
                       .WithCapability(new SignatureHelpCapability { DynamicRegistration = true, SignatureInformation = new SignatureInformationCapabilityOptions { DocumentationFormat = new[] { MarkupKind.Markdown, MarkupKind.PlainText } } })
                       .WithCapability(new ReferenceCapability { DynamicRegistration = true })
                       .WithCapability(new DocumentSymbolCapability { DynamicRegistration = true, SymbolKind = new SymbolKindCapabilityOptions { ValueSet = new Container<SymbolKind>(Enum.GetValues(typeof(SymbolKind)).Cast<SymbolKind>()) } })
                       .WithCapability(new WorkspaceSymbolCapability { DynamicRegistration = true, SymbolKind = new SymbolKindCapabilityOptions { ValueSet = new Container<SymbolKind>(Enum.GetValues(typeof(SymbolKind)).Cast<SymbolKind>()) } })
                       .WithCapability(new CodeActionCapability { DynamicRegistration = true })
                       .WithCapability(new RenameCapability { DynamicRegistration = true, PrepareSupport = true })
                       // Register the diagnostics handler
                       .OnPublishDiagnostics(PublishDiagnosticsHandler);
            });

            try
            {
                _logger.LogInformation("Starting client initialization for {ServerKey}", serverKey);
                // Initialize call after Create, takes only CancellationToken
                await client.Initialize(CancellationToken.None); 
                _logger.LogInformation("Client initialized successfully for {ServerKey}", serverKey);
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during language client initialization for {ServerKey}", serverKey);
                client.Dispose(); // Clean up if initialization fails
                return null;
            }
        }

        private void PublishDiagnosticsHandler(PublishDiagnosticsParams diagnostics)
        {
            _logger.LogDebug(0, null, "Received diagnostics for {Uri} - {Count} diagnostics", diagnostics.Uri.ToString(), diagnostics.Diagnostics.Count());
            
            // Create the event args and raise the event
            var args = new DiagnosticsEventArgs(diagnostics.Uri.ToString(), diagnostics.Diagnostics);
            DiagnosticsPublished?.Invoke(this, args);
        }
        
        private void SendDidOpenNotification(ILanguageClient client, string documentPath, string languageId, string content)
        {
             _logger.LogDebug("Sending textDocument/didOpen for {DocumentPath}", documentPath);
            client.DidOpenTextDocument(new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = DocumentUri.FromFileSystemPath(documentPath),
                    LanguageId = languageId,
                    Version = 1, // Initial version
                    Text = content
                }
            });
        }

        public async Task DocumentDidChangeAsync(string documentPath, string newContent)
        {
            var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot send didChange: Client not found for {DocumentPath}", documentPath);
                return;
            }

            _logger.LogDebug("Sending textDocument/didChange for {DocumentPath}", documentPath);
            client.DidChangeTextDocument(new DidChangeTextDocumentParams
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier
                {
                    Uri = DocumentUri.FromFileSystemPath(documentPath),
                    Version = null
                },
                // Keep explicit array creation
                ContentChanges = new TextDocumentContentChangeEvent[] { new TextDocumentContentChangeEvent { Text = newContent } }
            });
            await Task.CompletedTask; // Placeholder
        }

        public async Task DocumentDidCloseAsync(string documentPath)
        {
            var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot send didClose: Client not found for {DocumentPath}", documentPath);
                return;
            }

            _logger.LogDebug("Sending textDocument/didClose for {DocumentPath}", documentPath);
            client.DidCloseTextDocument(new DidCloseTextDocumentParams
            {
                TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(documentPath))
            });

            // TODO: Add logic to check if other documents are using this server instance
            // and shut down the client/server if it's no longer needed.
            // For now, clients live until the service is disposed.

            await Task.CompletedTask; // Placeholder
        }

        public async Task<CompletionList?> RequestCompletionAsync(string documentPath, Position position)
        {
            var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot request completion: Client not found for {DocumentPath}", documentPath);
                return null;
            }

            _logger.LogDebug("Requesting textDocument/completion for {DocumentPath} at position {Position}", documentPath, position);
            try
            {
                var result = await client.RequestCompletion(new CompletionParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(documentPath)),
                    Position = position,
                    Context = new CompletionContext
                    {
                        TriggerKind = CompletionTriggerKind.Invoked
                    }
                });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting completion for {DocumentPath}", documentPath);
                return null;
            }
        }

        public async Task<Hover?> RequestHoverAsync(string documentPath, Position position)
        {
            var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot request hover: Client not found for {DocumentPath}", documentPath);
                return null;
            }

            _logger.LogDebug("Requesting textDocument/hover for {DocumentPath} at position {Position}", documentPath, position);
            try
            {
                var result = await client.RequestHover(new HoverParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(documentPath)),
                    Position = position
                });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting hover for {DocumentPath}", documentPath);
                return null;
            }
        }

        public async Task<LocationOrLocationLinks?> RequestDefinitionAsync(string documentPath, Position position)
        {
            var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot request definition: Client not found for {DocumentPath}", documentPath);
                return null;
            }

            _logger.LogDebug("Requesting textDocument/definition for {DocumentPath} at position {Position}", documentPath, position);
            try
            {
                // Use client.TextDocument proxy to send the request
                var result = await client.TextDocument.RequestDefinition(new DefinitionParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(documentPath)),
                    Position = position
                }, CancellationToken.None); // Add CancellationToken

                _logger.LogDebug("Definition request successful for {DocumentPath}", documentPath);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting definition for {DocumentPath}", documentPath);
                return null;
            }
        }

        public async Task<SignatureHelp?> RequestSignatureHelpAsync(string documentPath, Position position)
        {
            var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot request signature help: Client not found for {DocumentPath}", documentPath);
                return null;
            }

            _logger.LogDebug("Requesting textDocument/signatureHelp for {DocumentPath} at position {Position}", documentPath, position);
            try
            {
                var result = await client.TextDocument.RequestSignatureHelp(new SignatureHelpParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(documentPath)),
                    Position = position,
                    Context = new SignatureHelpContext { TriggerKind = SignatureHelpTriggerKind.Invoked } // Example context
                }, CancellationToken.None);
                
                _logger.LogDebug("Signature help request successful for {DocumentPath}", documentPath);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting signature help for {DocumentPath}", documentPath);
                return null;
            }
        }

        public async Task<LocationContainer?> RequestReferencesAsync(string documentPath, Position position, ReferenceContext context)
        {
             var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot request references: Client not found for {DocumentPath}", documentPath);
                return null;
            }

            _logger.LogDebug("Requesting textDocument/references for {DocumentPath} at position {Position}", documentPath, position);
            try
            {
                var result = await client.TextDocument.RequestReferences(new ReferenceParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(documentPath)),
                    Position = position,
                    Context = context
                }, CancellationToken.None);

                _logger.LogDebug("References request successful for {DocumentPath}", documentPath);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting references for {DocumentPath}", documentPath);
                return null;
            }
        }
        
        public async Task<SymbolInformationOrDocumentSymbolContainer?> RequestDocumentSymbolsAsync(string documentPath)
        {
            var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot request document symbols: Client not found for {DocumentPath}", documentPath);
                return null;
            }

            _logger.LogDebug("Requesting textDocument/documentSymbol for {DocumentPath}", documentPath);
            try
            {
                var result = await client.TextDocument.RequestDocumentSymbol(new DocumentSymbolParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(documentPath))
                }, CancellationToken.None);

                _logger.LogDebug("Document symbols request successful for {DocumentPath}", documentPath);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting document symbols for {DocumentPath}", documentPath);
                return null;
            }
        }

       public async Task<Container<SymbolInformation>?> RequestWorkspaceSymbolsAsync(string query, CancellationToken cancellationToken = default)
        {
            var symbolInformationResults = new List<SymbolInformation>(); // Store converted results
            var activeClients = _languageClients.Values.ToArray();

            var requestTasks = activeClients.Select(async client =>
            {
                try
                {
                    // Expect Container<WorkspaceSymbol> based on the error
                    return await client.SendRequest(
                        new WorkspaceSymbolParams { Query = query },
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Workspace symbols request failed for client {ClientType}", client.GetType().Name);
                    return null;
                }
            });

            var responses = await Task.WhenAll(requestTasks);
            foreach (var workspaceSymbolContainer in responses.Where(r => r != null))
            {
                // Convert WorkspaceSymbol to SymbolInformation
                foreach (var wsSymbol in workspaceSymbolContainer!)
                {
                    // Handle LocationOrFileLocation
                    Location? location = null;
                    if (wsSymbol.Location.IsLocation)
                    {
                        location = wsSymbol.Location.Location;
                    }
                    else
                    {
                        // Log or handle the case where it's FileLocation only?
                        // For workspace/symbol, it should typically have a range.
                        _logger.LogWarning("WorkspaceSymbol '{Name}' unexpectedly had only FileLocation.", wsSymbol.Name);
                        continue; // Skip this symbol if location is unusable
                    }

                    symbolInformationResults.Add(new SymbolInformation
                    {
                        Name = wsSymbol.Name,
                        Kind = wsSymbol.Kind,
                        Location = location, // Assign the extracted Location
                        ContainerName = wsSymbol.ContainerName,
                        Tags = wsSymbol.Tags
                    });
                }
            }

            return symbolInformationResults.Count > 0 ? new Container<SymbolInformation>(symbolInformationResults) : null;
        }

        public async Task<CommandOrCodeActionContainer?> RequestCodeActionAsync(string documentPath, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range, CodeActionContext context)
        {
            var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot request code actions: Client not found for {DocumentPath}", documentPath);
                return null;
            }

            _logger.LogDebug("Requesting textDocument/codeAction for {DocumentPath} in range {Range}", documentPath, range);
            try
            {
                var result = await client.TextDocument.RequestCodeAction(new CodeActionParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(documentPath)),
                    Range = range,
                    Context = context
                }, CancellationToken.None);

                 _logger.LogDebug("Code action request successful for {DocumentPath}", documentPath);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting code actions for {DocumentPath}", documentPath);
                return null;
            }
        }

        public async Task<WorkspaceEdit?> RequestRenameAsync(string documentPath, Position position, string newName)
        {
             var serverKey = FindServerKeyForDocument(documentPath);
            if (serverKey == null || !_languageClients.TryGetValue(serverKey.Value, out var client))
            {
                _logger.LogWarning("Cannot request rename: Client not found for {DocumentPath}", documentPath);
                return null;
            }

            _logger.LogDebug("Requesting textDocument/rename for {DocumentPath} at {Position} to {NewName}", documentPath, position, newName);
            try
            {
                 var result = await client.TextDocument.RequestRename(new RenameParams
                 {
                     TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(documentPath)),
                     Position = position,
                     NewName = newName
                 }, CancellationToken.None);

                 _logger.LogDebug("Rename request successful for {DocumentPath}", documentPath);
                 return result;
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error requesting rename for {DocumentPath}", documentPath);
                 return null;
            }
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing LspService and shutting down clients...");
            foreach (var kvp in _languageClients)
            {
                _logger.LogInformation("Shutting down client for {ServerKey}", kvp.Key);
                try { kvp.Value.Dispose(); }
                catch (Exception ex) { _logger.LogWarning(ex, "Exception during direct client disposal for {Key}", kvp.Key); }
            }

            _languageClients.Clear();
            _logger.LogInformation("LspService disposed.");
            GC.SuppressFinalize(this);
        }

        // Helper to find the workspace root (e.g., .git folder, solution file)
        // TODO: Make this more robust (search upwards for .git, .sln, etc.)
        private string? FindWorkspaceRoot(string documentPath)
        {
            var directory = Path.GetDirectoryName(documentPath);
            while (!string.IsNullOrEmpty(directory))
            {
                if (Directory.Exists(Path.Combine(directory, ".git")) || Directory.GetFiles(directory, "*.sln").Length > 0)
                {
                    return directory;
                }
                directory = Path.GetDirectoryName(directory);
            }
            return null; // Or fallback to document's directory?
        }

        // Helper to find the server key associated with a document path
        private ServerKey? FindServerKeyForDocument(string documentPath)
        {
            foreach (var kvp in _languageClients)
            {
                // Check if the document is in or under the workspace root
                if (documentPath.StartsWith(kvp.Key.WorkspaceRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        // Helper method to log stream content asynchronously
        private async Task LogStreamAsync(StreamReader streamReader, string prefix)
        {
            try
            {
                while (!streamReader.EndOfStream)
                {
                    var line = await streamReader.ReadLineAsync();
                    if (line != null)
                    {
                        _logger.LogTrace("[{Prefix}] {Line}", prefix, line);
                    }
                }
            }
            catch (ObjectDisposedException) { /* Expected when process exits */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from {Prefix} stream.", prefix);
            }
        }
    }
} 