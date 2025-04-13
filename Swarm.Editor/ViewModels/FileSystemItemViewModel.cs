using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using Swarm.Editor.Common.Commands;
using Swarm.Editor.Views;
using Swarm.Editor.Models.Services;


namespace Swarm.Editor.ViewModels
{
    public class FileSystemItemViewModel : ViewModelBase
    {
        private readonly IFileSystemService _fileSystemService;
        private bool _isExpanded;
        private bool _isLoading;
        private bool _isInitialized;

        public FileSystemItemViewModel(IFileSystemService fileSystemService, string fullPath, bool isDirectory)
        {
            _fileSystemService = fileSystemService;
            FullPath = fullPath;
            IsDirectory = isDirectory;
            
            // Get the filename or directory name
            Name = Path.GetFileName(fullPath);
            
            // If this is a root folder with no filename, use just the root folder name instead of full path
            if (string.IsNullOrEmpty(Name) && isDirectory)
            {
                // Get the last directory segment from the path
                string trimmedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar);
                Name = trimmedPath.Substring(trimmedPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                
                // Fallback to full path if we couldn't extract a name
                if (string.IsNullOrEmpty(Name))
                {
                    Name = fullPath;
                }
            }
            
            Children = new ObservableCollection<FileSystemItemViewModel>();
            
            // If this is a directory, add a dummy child to show the expand arrow
            if (isDirectory)
            {
                // Add a dummy child item to make the directory expandable
                Children.Add(new FileSystemItemViewModel(_fileSystemService, "Loading...", false) { IsLoading = true });
            }
            
            // Create command for expanding directory
            ExpandCommand = new DelegateCommand(async () => await ExpandDirectoryAsync());
        }

        public string Name { get; }
        public string FullPath { get; }
        public bool IsDirectory { get; }
        public ObservableCollection<FileSystemItemViewModel> Children { get; }
        public ICommand ExpandCommand { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (this.RaiseAndSetIfChanged(ref _isExpanded, value) && value && IsDirectory)
                {
                    // When expanded, load children
                    _ = ExpandDirectoryAsync();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        // Method to expand a directory and load its children
        public async Task ExpandDirectoryAsync()
        {
            if (!IsDirectory || _isInitialized || IsLoading)
                return;

            IsLoading = true;

            try
            {
                var items = await _fileSystemService.GetDirectoryContentsAsync(FullPath);
                
                // Clear existing children, including dummy items
                Children.Clear();
                
                foreach (var item in items)
                {
                    bool isDir = item is DirectoryInfo;
                    var childViewModel = new FileSystemItemViewModel(_fileSystemService, item.FullName, isDir);
                    Children.Add(childViewModel);
                }
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                // Handle exceptions (logging, etc.)
                System.Diagnostics.Debug.WriteLine($"Error expanding directory: {ex.Message}");
                
                // If there was an error, restore a dummy child to keep the folder expandable
                if (Children.Count == 0)
                {
                    Children.Add(new FileSystemItemViewModel(_fileSystemService, "Error loading contents", false));
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
} 