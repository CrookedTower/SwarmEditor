using System;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Swarm.Lsp.Services
{
    /// <summary>
    /// Defines the contract for the Language Server Protocol service.
    /// Manages language server instances and communication.
    /// </summary>
    public interface ILspService : IDisposable
    {
        /// <summary>
        /// Initializes LSP support for a given document.
        /// Starts the appropriate language server if not already running for the workspace.
        /// </summary>
        /// <param name="documentPath">The full path to the document.</param>
        /// <param name="languageId">The language identifier (e.g., "csharp", "python").</param>
        /// <param name="initialContent">The initial text content of the document.</param>
        Task InitializeForDocumentAsync(string documentPath, string languageId, string initialContent);

        /// <summary>
        /// Notifies the LSP service that a document's content has changed.
        /// </summary>
        /// <param name="documentPath">The full path to the document.</param>
        /// <param name="newContent">The updated text content.</param> // Simplified for now, could use incremental changes later
        Task DocumentDidChangeAsync(string documentPath, string newContent);

        /// <summary>
        /// Notifies the LSP service that a document has been closed.
        /// </summary>
        /// <param name="documentPath">The full path to the document.</param>
        Task DocumentDidCloseAsync(string documentPath);

        /// <summary>
        /// Requests code completion at the specified position in the document.
        /// </summary>
        /// <param name="documentPath">The full path to the document.</param>
        /// <param name="position">The position in the document where completion is requested.</param>
        /// <returns>A task representing the asynchronous operation, with the completion list or null if not available.</returns>
        Task<CompletionList?> RequestCompletionAsync(string documentPath, Position position);

        /// <summary>
        /// Requests hover information at the specified position in the document.
        /// </summary>
        /// <param name="documentPath">The full path to the document.</param>
        /// <param name="position">The position in the document where hover information is requested.</param>
        /// <returns>A task representing the asynchronous operation, with the hover information or null if not available.</returns>
        Task<Hover?> RequestHoverAsync(string documentPath, Position position);

        /// <summary>
        /// Requests the definition location for the symbol at the specified position.
        /// </summary>
        /// <param name="documentPath">The full path to the document.</param>
        /// <param name="position">The position in the document where definition is requested.</param>
        /// <returns>A task representing the asynchronous operation, with the definition locations or null if not available.</returns>
        Task<LocationOrLocationLinks?> RequestDefinitionAsync(string documentPath, Position position);

        /// <summary>
        /// Event raised when diagnostic information is published by the language server.
        /// </summary>
        event EventHandler<DiagnosticsEventArgs> DiagnosticsPublished;
    }

    /// <summary>
    /// Event arguments for diagnostics notifications.
    /// </summary>
    public class DiagnosticsEventArgs : EventArgs
    {
        /// <summary>
        /// The URI of the document the diagnostics are for.
        /// </summary>
        public string DocumentUri { get; }

        /// <summary>
        /// The collection of diagnostics.
        /// </summary>
        public Container<Diagnostic> Diagnostics { get; }

        public DiagnosticsEventArgs(string documentUri, Container<Diagnostic> diagnostics)
        {
            DocumentUri = documentUri;
            Diagnostics = diagnostics;
        }
    }
} 