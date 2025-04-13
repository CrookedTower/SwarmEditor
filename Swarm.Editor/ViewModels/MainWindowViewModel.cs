using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Swarm.Editor.Views;
using Swarm.Editor.Common.Converters;
using Swarm.Editor.Models.Services;
using Swarm.Editor.Common.Commands;
using Swarm.Editor.ViewModels.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Swarm.Editor.ViewModels.Panels;
using Swarm.Shared.EventBus;
using Swarm.Shared.EventBus.Events;
using Swarm.Lsp.Services;

namespace Swarm.Editor.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        // Services
        private readonly IApplicationService _applicationService;
        private readonly IFileSystemService _fileSystemService;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IEventBus _eventBus;
        private readonly ILspService _lspService;
        
        // View Models
        public CodeEditorViewModel EditorViewModel { get; }
        
        // Rename properties to use 'Panel' and specific ViewModel types
        private LeftPanelViewModel? _leftPanelContent;
        public LeftPanelViewModel? LeftPanelContent
        {
            get => _leftPanelContent;
            private set => SetProperty(ref _leftPanelContent, value);
        }

        private RightPanelViewModel? _rightPanelContent;
        public RightPanelViewModel? RightPanelContent
        {
            get => _rightPanelContent;
            private set => SetProperty(ref _rightPanelContent, value);
        }

        // Document tabs
        private ObservableCollection<DocumentTabViewModel> _documentTabs = new();
        public ObservableCollection<DocumentTabViewModel> DocumentTabs
        {
            get => _documentTabs;
            private set => SetProperty(ref _documentTabs, value);
        }
        
        private DocumentTabViewModel _activeDocument;
        public DocumentTabViewModel ActiveDocument
        {
            get => _activeDocument;
            set
            {
                var newValue = value ?? new DocumentTabViewModel(string.Empty, string.Empty); // Ensure non-null
                Debug.WriteLine($"[DEBUG][ActiveDocument Set] Attempting to set ActiveDocument. Current: '{_activeDocument?.DisplayName ?? "null"}', New: '{newValue.DisplayName ?? "null"}'");

                // If the value is actually changing
                if (!ReferenceEquals(_activeDocument, newValue))
                {
                    Debug.WriteLine("[DEBUG][ActiveDocument Set] Value is changing.");
                    // Save content of the outgoing tab *before* changing the active document
                    if (_activeDocument != null && !_activeDocument.FilePath.Equals(string.Empty)) // Avoid saving the initial empty placeholder
                    {
                        Debug.WriteLine($"[DEBUG][ActiveDocument Set] Saving outgoing content for '{_activeDocument.DisplayName}'.");
                        _activeDocument.Content = EditorViewModel.Text; // Assuming EditorViewModel.Text holds current editor state
                        _activeDocument.IsActive = false;
                         Debug.WriteLine("[DEBUG][ActiveDocument Set] Outgoing content saved.");
                    }
                    else
                    {
                        Debug.WriteLine("[DEBUG][ActiveDocument Set] Skipping save for outgoing document (null or initial empty).");
                    }

                    // Use SetProperty to handle backing field update and notification
                    if (SetProperty(ref _activeDocument, newValue))
                    {
                        Debug.WriteLine("[DEBUG][ActiveDocument Set] SetProperty succeeded. Loading new content.");
                        // Update editor with the incoming tab's content *after* property change notification
                        EditorViewModel.Text = _activeDocument.Content ?? string.Empty;
                        CurrentFilePath = _activeDocument.FilePath ?? string.Empty;
                        _activeDocument.IsActive = true;
                        Debug.WriteLine($"[DEBUG][ActiveDocument Set] New content loaded for '{_activeDocument.DisplayName}'. Editor Text Length: {EditorViewModel.Text?.Length ?? 0}");
                    }
                    else
                    {
                         Debug.WriteLine("[DEBUG][ActiveDocument Set] SetProperty returned false (value might be the same reference after all?).");
                    }
                }
                else
                {
                    Debug.WriteLine("[DEBUG][ActiveDocument Set] New value is same reference as old value. No change.");
                }
                Debug.WriteLine("[DEBUG][ActiveDocument Set] Setter finished.");
            }
        }
        
        // File menu commands
        public ICommand OpenFileCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand SaveFileAsCommand { get; }
        public ICommand NewFileCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand ExitCommand { get; }
        
        // Track current file
        private string _currentFilePath = string.Empty;
        public string CurrentFilePath
        {
            get => _currentFilePath;
            private set => SetProperty(ref _currentFilePath, value ?? string.Empty);
        }
        
        // Constructor
        public MainWindowViewModel(
            IApplicationService? applicationService = null, 
            IFileSystemService? fileSystemService = null,
            IEventBus? eventBus = null,
            ILogger<MainWindowViewModel>? logger = null,
            ILspService? lspService = null,
            CodeEditorViewModel? editorViewModel = null
            // LeftPanelViewModel? leftPanel = null, // TODO: Inject Panels via DI too?
            // RightPanelViewModel? rightPanel = null // TODO: Inject Panels via DI too?
            )
        {
            // Initialize services
            _applicationService = applicationService ?? new ApplicationService();
            _fileSystemService = fileSystemService ?? new FileSystemService();
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? NullLogger<MainWindowViewModel>.Instance;
            _lspService = lspService ?? throw new ArgumentNullException(nameof(lspService));
            
            // Initialize view models
            EditorViewModel = editorViewModel ?? throw new ArgumentNullException(nameof(editorViewModel));
            
            // Instantiate Panel ViewModels, passing required services
            LeftPanelContent = new LeftPanelViewModel(_fileSystemService, _eventBus);
            RightPanelContent = new RightPanelViewModel();
            
            // Initialize active document to avoid CS8618 warning
            // Use a distinct initial empty object
            _activeDocument = new DocumentTabViewModel(string.Empty, string.Empty, "InitialEmpty") { IsActive = true }; 
            
            // Initialize commands
            OpenFileCommand = new DelegateCommand(async () => await OpenFileDialogAsync());
            OpenFolderCommand = new DelegateCommand(async () => await OpenFolderDialogAsync());
            SaveFileCommand = new DelegateCommand(async () => await SaveFileAsync(), CanSaveFile);
            SaveFileAsCommand = new DelegateCommand(async () => await SaveFileAsDialogAsync());
            NewFileCommand = new DelegateCommand(CreateNewDocument);
            CloseTabCommand = new DelegateCommand<DocumentTabViewModel>(CloseDocument);
            ExitCommand = new DelegateCommand(Exit);
            
            // No need to create a document on startup as user can click the + button
            
            // Subscribe to events
            _eventBus.Subscribe<ChatMessageSentEvent>(HandleChatMessageSent);
            _eventBus.Subscribe<FileOpenRequestedEvent>(HandleFileOpenRequestedAsync);
            _logger.LogInformation("MainWindowViewModel initialized and subscribed to events.");
        }

        private Task HandleChatMessageSent(ChatMessageSentEvent sentEvent)
        {
            _logger.LogInformation("MainWindowViewModel received ChatMessageSentEvent: {Message}", sentEvent.Message);
            return Task.CompletedTask;
        }

        // Handler for FileOpenRequestedEvent
        private async Task HandleFileOpenRequestedAsync(FileOpenRequestedEvent ev)
        {
            Debug.WriteLine($"[DEBUG][HandleFileOpenRequestedAsync] Received event for: {ev?.FilePath ?? "null"}");
            if (ev == null || string.IsNullOrEmpty(ev.FilePath))
            {
                 _logger.LogWarning("Received null or empty FileOpenRequestedEvent.");
                 return;
            }
            
            _logger.LogInformation("Handling FileOpenRequestedEvent for: {FilePath}", ev.FilePath);
            try
            {
                await OpenFileAsync(ev.FilePath);
                Debug.WriteLine($"[DEBUG][HandleFileOpenRequestedAsync] OpenFileAsync completed for: {ev.FilePath}");
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error handling FileOpenRequestedEvent for {FilePath}", ev.FilePath);
                 Debug.WriteLine($"[ERROR][HandleFileOpenRequestedAsync] Error processing {ev.FilePath}: {ex.Message}");
                 // Optionally: show an error message to the user
            }
        }
        
        private void CreateNewDocument()
        {
            // Generate a better title for untitled documents, like "Untitled-1", "Untitled-2", etc.
            int untitledCounter = DocumentTabs.Count(d => d.FilePath == null) + 1;
            var displayName = untitledCounter > 1 ? $"Untitled-{untitledCounter}" : "Untitled";
            
            // Create a new document
            var newDocument = new DocumentTabViewModel(null, string.Empty, displayName);
            DocumentTabs.Add(newDocument);
            ActiveDocument = newDocument;
        }
        
        private void CloseDocument(DocumentTabViewModel? document)
        {
            if (document == null || !DocumentTabs.Contains(document)) return;
            
            Debug.WriteLine($"[DEBUG][CloseDocument] Closing tab: {document.DisplayName}");
            // ToDo: Add warning for unsaved changes
            
            int index = DocumentTabs.IndexOf(document);
            bool wasActive = ReferenceEquals(ActiveDocument, document);
            
            DocumentTabs.Remove(document);
            Debug.WriteLine($"[DEBUG][CloseDocument] Tab removed from collection. Count: {DocumentTabs.Count}");
            
            if (wasActive)
            {
                if (DocumentTabs.Count > 0)
                {
                    // Select the previous tab, or the first one if closing the first tab
                    ActiveDocument = DocumentTabs[Math.Max(0, index - 1)];
                     Debug.WriteLine($"[DEBUG][CloseDocument] Setting new active tab: {ActiveDocument.DisplayName}");
                }
                else
                {
                    // No tabs left, set active to the initial empty placeholder
                    // Create a *new* initial empty placeholder to ensure it's a different reference
                    ActiveDocument = new DocumentTabViewModel(string.Empty, string.Empty, "InitialEmpty");
                    EditorViewModel.Text = string.Empty; // Clear editor manually
                    CurrentFilePath = string.Empty;
                    Debug.WriteLine("[DEBUG][CloseDocument] No tabs left, resetting to initial empty state.");
                }
            }
        }
        
        // Command implementations
        private async Task OpenFileDialogAsync()
        {
             Debug.WriteLine("[DEBUG][OpenFileDialogAsync] Entered.");
            try
            {
                var filePath = await _fileSystemService.OpenFileDialogAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                     Debug.WriteLine($"[DEBUG][OpenFileDialogAsync] File selected: {filePath}");
                    await OpenFileAsync(filePath);
                }
                else
                {
                     Debug.WriteLine("[DEBUG][OpenFileDialogAsync] No file selected.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening file dialog.");
                Debug.WriteLine($"[ERROR][OpenFileDialogAsync] Error: {ex.Message}");
                // Optionally: show error message to user
            }
        }
        
        private async Task OpenFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("OpenFileAsync: Invalid or non-existent file path: {FilePath}", filePath);
                return;
            }

            _logger.LogInformation("OpenFileAsync: Opening file {FilePath}", filePath);

            try
            {
                // Check if the file is already open
                var existingTab = DocumentTabs.FirstOrDefault(tab => tab.FilePath == filePath);
                if (existingTab != null)
                {
                    _logger.LogInformation("OpenFileAsync: File already open, activating tab: {FilePath}", filePath);
                    ActiveDocument = existingTab;
                    return;
                }

                // Read file content
                string content = await _fileSystemService.ReadFileAsync(filePath);
                _logger.LogDebug("OpenFileAsync: Read {ContentLength} characters from {FilePath}", content.Length, filePath);
                
                // -------- BEGIN LSP INITIALIZATION --------
                string? languageId = Path.GetExtension(filePath)?.ToLowerInvariant() switch
                {
                    ".cs" => "csharp",
                    // TODO: Add mappings for other languages (e.g., .py -> python, .ts -> typescript)
                    _ => null
                };

                if (!string.IsNullOrEmpty(languageId))
                {
                    try
                    {
                        _logger.LogInformation("Initializing LSP for {FilePath} ({LanguageId})", filePath, languageId);
                        // Note: Pass content to initialize the server's view of the file
                        await _lspService.InitializeForDocumentAsync(filePath, languageId, content);
                        _logger.LogInformation("LSP initialized successfully for {FilePath}", filePath);
                    }
                    catch (Exception lspEx)
                    {
                        // Log the error but continue opening the file in the editor
                        _logger.LogError(lspEx, "Error initializing LSP for {FilePath}", filePath);
                        // TODO: Display a user-facing notification about LSP failure?
                    }
                }
                else
                {
                    _logger.LogInformation("Skipping LSP initialization for unsupported file type: {FilePath}", filePath);
                }
                // -------- END LSP INITIALIZATION --------

                // Create and add the new document tab
                var newDocument = new DocumentTabViewModel(filePath, content)
                {
                    DisplayName = Path.GetFileName(filePath) ?? "Untitled"
                };
                DocumentTabs.Add(newDocument);
                ActiveDocument = newDocument; // Make the new tab active

                _logger.LogInformation("OpenFileAsync: Successfully opened and added tab for {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenFileAsync: Error opening file {FilePath}", filePath);
                // TODO: Show error message to the user
            }
        }
        
        private async Task OpenFolderDialogAsync()
        {
            Debug.WriteLine("[DEBUG][OpenFolderDialogAsync] Entered.");
            try
            {
                var folderPath = await _fileSystemService.OpenFolderDialogAsync();
                if (!string.IsNullOrEmpty(folderPath))
                {
                    Debug.WriteLine($"[DEBUG][OpenFolderDialogAsync] Publishing FolderSelectedEvent for: {folderPath}");
                    await _eventBus.PublishAsync(new FolderSelectedEvent(folderPath));
                     Debug.WriteLine("[DEBUG][OpenFolderDialogAsync] FolderSelectedEvent published.");
                }
                 else
                {
                    Debug.WriteLine("[DEBUG][OpenFolderDialogAsync] No folder selected.");
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error opening folder dialog.");
                 Debug.WriteLine($"[ERROR][OpenFolderDialogAsync] Error: {ex.Message}");
                 // Optionally: show error message to user
            }
        }
        
        private async Task SaveFileAsync()
        {
            if (ActiveDocument == null) return;
             Debug.WriteLine($"[DEBUG][SaveFileAsync] Entered for: {ActiveDocument.FilePath ?? "Untitled"}");

            // Ensure content is up-to-date before saving
            ActiveDocument.Content = EditorViewModel.Text;

            if (string.IsNullOrEmpty(ActiveDocument.FilePath) || !_fileSystemService.FileExists(ActiveDocument.FilePath))
            {
                await SaveFileAsDialogAsync();
            }
            else
            {
                try
                {
                    await _fileSystemService.WriteFileAsync(ActiveDocument.FilePath, ActiveDocument.Content);
                    ActiveDocument.IsModified = false;
                    Debug.WriteLine($"[DEBUG][SaveFileAsync] File saved: {ActiveDocument.FilePath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving file {FilePath}", ActiveDocument.FilePath);
                     Debug.WriteLine($"[ERROR][SaveFileAsync] Error saving {ActiveDocument.FilePath}: {ex.Message}");
                    // Show error message
                }
            }
        }
        
        private bool CanSaveFile()
        {
            // Can save if there is an active document
            return ActiveDocument != null && !ActiveDocument.FilePath.Equals(string.Empty); 
        }
        
        private async Task SaveFileAsDialogAsync()
        {
            if (ActiveDocument == null) return;
             Debug.WriteLine($"[DEBUG][SaveFileAsDialogAsync] Entered.");

            // Ensure content is up-to-date before saving
            ActiveDocument.Content = EditorViewModel.Text;
            bool wasUntitled = string.IsNullOrEmpty(ActiveDocument.FilePath); // Check if it was untitled before saving

            try
            {
                var filePath = await _fileSystemService.SaveFileDialogAsync(ActiveDocument.DisplayName);
                if (!string.IsNullOrEmpty(filePath))
                {
                    ActiveDocument.FilePath = filePath;
                    // Update DisplayName only if it was previously an untitled document
                    if (wasUntitled) 
                    { 
                        ActiveDocument.DisplayName = Path.GetFileName(filePath);
                    }
                    await SaveFileAsync(); // Call save now that we have a path
                     Debug.WriteLine($"[DEBUG][SaveFileAsDialogAsync] File saved as: {filePath}");
                }
                 else
                {
                     Debug.WriteLine("[DEBUG][SaveFileAsDialogAsync] Save As dialog cancelled.");
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error showing Save As dialog.");
                 Debug.WriteLine($"[ERROR][SaveFileAsDialogAsync] Error: {ex.Message}");
                 // Show error message
            }
        }
        
        private void Exit()
        {
            _applicationService.Exit();
        }
    }
} 