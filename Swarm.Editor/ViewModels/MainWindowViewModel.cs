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
using Swarm.Agents.EventBus;
using Microsoft.Extensions.Logging;
using Swarm.Editor.Models.Events;
using Microsoft.Extensions.Logging.Abstractions;

using Swarm.Editor.ViewModels;

namespace Swarm.Editor.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        // Services
        private readonly IApplicationService _applicationService;
        private readonly IFileSystemService _fileSystemService;
        private readonly ILogger<ChatViewModel> _logger;
        private readonly IEventBus _eventBus;
        
        // View Models
        public CodeEditorViewModel EditorViewModel { get; }
        
        // Rename properties to use 'Panel'
        private ViewModelBase? _leftPanelContent;
        public ViewModelBase? LeftPanelContent 
        {
            get => _leftPanelContent;
            set => SetProperty(ref _leftPanelContent, value); 
        }

        private ViewModelBase? _rightPanelContent;
        public ViewModelBase? RightPanelContent
        {
            get => _rightPanelContent;
            set => SetProperty(ref _rightPanelContent, value);
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
                if (_activeDocument != value)
                {
                    // Save current content to the outgoing tab
                    if (_activeDocument != null)
                    {
                        _activeDocument.Content = EditorViewModel.Text;
                        _activeDocument.IsActive = false;
                    }
                    
                    // Create a safe non-null reference first
                    var newValue = value ?? new DocumentTabViewModel(string.Empty, string.Empty);
                    
                    // Instead of using SetProperty directly, manually handle the property change
                    var oldValue = _activeDocument;
                    _activeDocument = newValue;
                    OnPropertyChanged(nameof(ActiveDocument));
                    
                    // Update editor with the incoming tab's content
                    EditorViewModel.Text = newValue.Content;
                    CurrentFilePath = newValue.FilePath;
                    newValue.IsActive = true;
                }
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
            ILogger<ChatViewModel>? chatLogger = null,
            // Add parameters for injected VMs
            FileExplorerViewModel? fileExplorerVm = null, 
            ChatViewModel? chatVm = null)
        {
            // Initialize services
            _applicationService = applicationService ?? new ApplicationService();
            _fileSystemService = fileSystemService ?? new FileSystemService();
            _eventBus = eventBus ?? new Swarm.Editor.Models.Events.InMemoryEventBus();
            _logger = chatLogger ?? NullLogger<ChatViewModel>.Instance;
            
            // Initialize view models
            EditorViewModel = new CodeEditorViewModel();
            
            // Assign injected VMs (using Panel names)
            LeftPanelContent = fileExplorerVm;
            RightPanelContent = chatVm;

            // Initialize active document to avoid CS8618 warning
            _activeDocument = new DocumentTabViewModel(string.Empty, string.Empty);
            
            // Subscribe to file explorer events (update to use LeftPanelContent)
            if (LeftPanelContent is FileExplorerViewModel explorerVm)
            {
                explorerVm.FileSelected += OnFileSelected;
            }
            
            // Initialize commands
            OpenFileCommand = new DelegateCommand(async () => await OpenFileDialogAsync());
            OpenFolderCommand = new DelegateCommand(async () => await OpenFolderDialogAsync());
            SaveFileCommand = new DelegateCommand(async () => await SaveFileAsync(), CanSaveFile);
            SaveFileAsCommand = new DelegateCommand(async () => await SaveFileAsDialogAsync());
            NewFileCommand = new DelegateCommand(CreateNewDocument);
            CloseTabCommand = new DelegateCommand<DocumentTabViewModel>(CloseDocument);
            ExitCommand = new DelegateCommand(Exit);
            
            // No need to create a document on startup as user can click the + button
        }

        private void OnFileSelected(object? sender, string filePath)
        {
            // Use Task.Run to avoid UI blocking while allowing async execution
            Task.Run(async () => await OpenFileAsync(filePath));
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
        
        private void CloseDocument(DocumentTabViewModel document)
        {
            if (document == null) return;
            
            // ToDo: Add warning for unsaved changes
            
            int index = DocumentTabs.IndexOf(document);
            DocumentTabs.Remove(document);
            
            // Select a new active document if we closed the active one and there are still tabs
            if (DocumentTabs.Count > 0)
            {
                ActiveDocument = DocumentTabs[Math.Min(index, DocumentTabs.Count - 1)];
            }
            else
            {
                // When no documents are left, reset the editor content but don't create a new tab
                EditorViewModel.Text = string.Empty;
                CurrentFilePath = string.Empty;
                
                // Keep the initialized empty document as the active document
                // This won't be visible in the UI as a tab
            }
        }
        
        // Command implementations
        private async Task OpenFileDialogAsync()
        {
            try
            {
                var filePath = await _fileSystemService.OpenFileDialogAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    await OpenFileAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening file dialog: {ex.Message}");
            }
        }
        
        private async Task OpenFileAsync(string filePath)
        {
            try
            {
                if (_fileSystemService.FileExists(filePath))
                {
                    // Check if the file is already open
                    var existingTab = DocumentTabs.FirstOrDefault(t => t.FilePath == filePath);
                    if (existingTab != null)
                    {
                        ActiveDocument = existingTab;
                        return;
                    }
                    
                    var content = await _fileSystemService.ReadFileAsync(filePath);
                    var document = new DocumentTabViewModel(filePath, content);
                    
                    // Check if there's only one tab and it's an untitled document
                    // Always replace it regardless of content
                    var untitledTab = DocumentTabs.FirstOrDefault(d => 
                        d.FilePath == null && 
                        d.DisplayName.StartsWith("Untitled"));
                    
                    if (untitledTab != null && DocumentTabs.Count == 1)
                    {
                        // Replace the untitled document
                        int index = DocumentTabs.IndexOf(untitledTab);
                        DocumentTabs.RemoveAt(index);
                        DocumentTabs.Insert(index, document);
                    }
                    else
                    {
                        // Just add a new tab
                        DocumentTabs.Add(document);
                    }
                    
                    ActiveDocument = document;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening file: {ex.Message}");
            }
        }
        
        private async Task OpenFolderDialogAsync()
        {
            try
            {
                var folderPath = await _fileSystemService.OpenFolderDialogAsync();
                if (!string.IsNullOrEmpty(folderPath))
                {
                    // Access LoadFolderAsync via LeftPanelContent with checks
                    if (LeftPanelContent is FileExplorerViewModel explorerVm)
                    {
                        await explorerVm.LoadFolderAsync(folderPath);
                    }
                    else
                    {
                        Debug.WriteLine("Error: LeftPanelContent is not a FileExplorerViewModel.");
                        // Optionally log this error or display a user message
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening folder dialog: {ex.Message}");
            }
        }
        
        private async Task SaveFileAsync()
        {
            if (ActiveDocument == null) return;
            
            // Capture the current content from the editor
            ActiveDocument.Content = EditorViewModel.Text;
            
            if (string.IsNullOrEmpty(ActiveDocument.FilePath))
            {
                await SaveFileAsDialogAsync();
            }
            else
            {
                try
                {
                    await _fileSystemService.WriteFileAsync(ActiveDocument.FilePath, ActiveDocument.Content);
                    ActiveDocument.IsModified = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving file: {ex.Message}");
                }
            }
        }
        
        private bool CanSaveFile()
        {
            return ActiveDocument != null && !string.IsNullOrEmpty(ActiveDocument.Content);
        }
        
        private async Task SaveFileAsDialogAsync()
        {
            if (ActiveDocument == null) return;
            
            try
            {
                var filePath = await _fileSystemService.SaveFileDialogAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    // Capture the current content from the editor
                    ActiveDocument.Content = EditorViewModel.Text;
                    
                    await _fileSystemService.WriteFileAsync(filePath, ActiveDocument.Content);
                    ActiveDocument.FilePath = filePath;
                    ActiveDocument.IsModified = false;
                    CurrentFilePath = filePath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing save file dialog: {ex.Message}");
            }
        }
        
        private void Exit()
        {
            _applicationService.Exit();
        }
    }
} 