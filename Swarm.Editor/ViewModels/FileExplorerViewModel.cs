using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Swarm.Editor.Views;
using Swarm.Editor.Models.Services;
using Swarm.Shared.EventBus;
using Swarm.Shared.EventBus.Events;
using ReactiveUI;
using System.Reactive;

namespace Swarm.Editor.ViewModels
{
    public class FileExplorerViewModel : ViewModelBase
    {
        private readonly IFileSystemService _fileSystemService;
        private readonly IEventBus _eventBus;
        
        // Collection of root items in the file tree
        private ObservableCollection<FileSystemItemViewModel> _rootItems = new();
        public ObservableCollection<FileSystemItemViewModel> RootItems
        {
            get => _rootItems;
            private set => this.RaiseAndSetIfChanged(ref _rootItems, value);
        }
        
        // Selected item in the file tree
        private FileSystemItemViewModel? _selectedItem;
        public FileSystemItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                // Only set the property, logic moved to command
                this.RaiseAndSetIfChanged(ref _selectedItem, value);
            }
        }

        // Command for opening files
        public ReactiveCommand<FileSystemItemViewModel?, Unit> OpenFileCommand { get; }

        // Constructor
        public FileExplorerViewModel(IFileSystemService fileSystemService, IEventBus eventBus)
        {
            _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            _eventBus.Subscribe<FolderSelectedEvent>(HandleFolderSelected);
            
            // Define CanExecute observable based on SelectedItem
            var canOpenFile = this.WhenAnyValue(x => x.SelectedItem,
                                                 (item) => item != null && !item.IsDirectory);

            // Initialize the command
            OpenFileCommand = ReactiveCommand.CreateFromTask<FileSystemItemViewModel?>(ExecuteOpenFileAsync, canOpenFile);

            Debug.WriteLine("[DEBUG][FileExplorerViewModel] Initialized and subscribed to FolderSelectedEvent.");
        }
        
        // Execute logic for the command
        private async Task ExecuteOpenFileAsync(FileSystemItemViewModel? item)
        {
            Debug.WriteLine($"[DEBUG][FileExplorerViewModel] ExecuteOpenFileAsync entered for '{item?.Name ?? "null"}'.");
            if (item == null || item.IsDirectory) // Check if null or is a directory
            {
                Debug.WriteLine("[DEBUG][FileExplorerViewModel] ExecuteOpenFileAsync: Item is null or a directory. Exiting.");
                return;
            }

            if (string.IsNullOrEmpty(item.FullPath))
            {
                Debug.WriteLine("[DEBUG][FileExplorerViewModel] ExecuteOpenFileAsync: Item FullPath is null or empty. Exiting.");
                return;
            }

            try
            {
                var openEvent = new FileOpenRequestedEvent(item.FullPath);
                Debug.WriteLine($"[DEBUG][FileExplorerViewModel] Publishing FileOpenRequestedEvent for: {item.FullPath}");
                await _eventBus.PublishAsync(openEvent);
                Debug.WriteLine($"[DEBUG][FileExplorerViewModel] FileOpenRequestedEvent published successfully for: {item.FullPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR][FileExplorerViewModel] ExecuteOpenFileAsync: Error publishing FileOpenRequestedEvent for {item.FullPath}: {ex.Message}");
                // Log error, maybe show user message
            }
            Debug.WriteLine($"[DEBUG][FileExplorerViewModel] ExecuteOpenFileAsync finished for '{item?.Name ?? "null"}'.");
        }

        // Handler for FolderSelectedEvent
        private async Task HandleFolderSelected(FolderSelectedEvent ev)
        {
            Debug.WriteLine($"[DEBUG][FileExplorerViewModel] HandleFolderSelected received for: {ev?.FolderPath}");
            if (ev == null || string.IsNullOrEmpty(ev.FolderPath) || !_fileSystemService.DirectoryExists(ev.FolderPath))
            {
                Debug.WriteLine("[DEBUG][FileExplorerViewModel] HandleFolderSelected: Invalid folder path or event.");
                // Optionally clear items or show an error
                _rootItems.Clear();
                return;
            }

            try
            {
                await LoadFolderAsync(ev.FolderPath);
                Debug.WriteLine($"[DEBUG][FileExplorerViewModel] HandleFolderSelected: LoadFolderAsync completed for: {ev.FolderPath}");
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"[ERROR][FileExplorerViewModel] HandleFolderSelected: Error loading folder {ev.FolderPath}: {ex.Message}");
                 // Log error, show message?
                 _rootItems.Clear(); // Clear on error?
            }
        }

        // Method to load a folder into the file explorer
        public async Task LoadFolderAsync(string folderPath)
        {
            Debug.WriteLine($"[DEBUG][FileExplorerViewModel] LoadFolderAsync entered for: {folderPath}");
            _rootItems.Clear();
            try
            {
                if (_fileSystemService.DirectoryExists(folderPath))
                {
                    // Create a root item for the folder, indicating it IS a directory
                    var rootItem = new FileSystemItemViewModel(_fileSystemService, folderPath, true);
                    _rootItems.Add(rootItem);
                    Debug.WriteLine($"[DEBUG][FileExplorerViewModel] LoadFolderAsync: Added root item: {rootItem.Name}");
                    // Expand the root item to load its children
                    await rootItem.ExpandDirectoryAsync(); 
                    Debug.WriteLine($"[DEBUG][FileExplorerViewModel] LoadFolderAsync: Expanded root item: {rootItem.Name}");
                }
                else
                {
                    Debug.WriteLine($"[DEBUG][FileExplorerViewModel] LoadFolderAsync: Directory does not exist: {folderPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR][FileExplorerViewModel] LoadFolderAsync: Error processing folder {folderPath}: {ex.Message}");
                // Optionally clear items or show an error message
                _rootItems.Clear();
            }
            Debug.WriteLine($"[DEBUG][FileExplorerViewModel] LoadFolderAsync finished for: {folderPath}");
        }
    }
} 