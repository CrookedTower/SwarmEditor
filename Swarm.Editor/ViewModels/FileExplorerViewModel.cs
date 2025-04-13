using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Swarm.Editor.Common.Commands;
using Swarm.Editor.Views;
using Swarm.Editor.Models.Services;
namespace Swarm.Editor.ViewModels
{
    public class FileExplorerViewModel : ViewModelBase
    {
        private readonly IFileSystemService _fileSystemService;
        
        // Collection of root items in the file tree
        private ObservableCollection<FileSystemItemViewModel> _rootItems = new();
        public ObservableCollection<FileSystemItemViewModel> RootItems
        {
            get => _rootItems;
            private set => SetProperty(ref _rootItems, value);
        }
        
        // Selected item in the file tree
        private FileSystemItemViewModel? _selectedItem;
        public FileSystemItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value) && value != null && !value.IsDirectory)
                {
                    // If a file is selected, notify about it
                    FileSelected?.Invoke(this, value.FullPath);
                }
            }
        }

        // Event to notify when a file is selected
        public event EventHandler<string>? FileSelected;

        // Constructor
        public FileExplorerViewModel(IFileSystemService fileSystemService)
        {
            _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
            
            // Initialize collections - now redundant since we initialize at declaration
            RootItems = new ObservableCollection<FileSystemItemViewModel>();
        }
        
        // Method to load a folder into the file explorer
        public async Task LoadFolderAsync(string folderPath)
        {
            try
            {
                if (_fileSystemService.DirectoryExists(folderPath))
                {
                    // Create a root item for the folder
                    var rootItem = new FileSystemItemViewModel(_fileSystemService, folderPath, true);
                    
                    // Clear existing items and add new root
                    RootItems.Clear();
                    RootItems.Add(rootItem);
                    
                    // Expand the root item to load its children
                    await rootItem.ExpandDirectoryAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading folder: {ex.Message}");
            }
        }
    }
} 