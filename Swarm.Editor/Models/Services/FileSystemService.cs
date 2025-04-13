using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;

namespace Swarm.Editor.Models.Services
{
    public interface IFileSystemService
    {
        // Navigation and browsing
        Task<IEnumerable<FileSystemInfo>> GetDirectoryContentsAsync(string path);
        bool DirectoryExists(string path);
        bool FileExists(string path);
        
        // File operations
        Task<string> ReadFileAsync(string filePath);
        Task WriteFileAsync(string filePath, string content);
        
        // Dialog operations
        Task<string?> OpenFileDialogAsync(string title = "Open File", string? initialDirectory = null, IEnumerable<string>? fileTypeFilter = null);
        Task<string?> SaveFileDialogAsync(string title = "Save File", string? initialDirectory = null, string? defaultFileName = null, IEnumerable<string>? fileTypeFilter = null);
        Task<string?> OpenFolderDialogAsync(string title = "Open Folder", string? initialDirectory = null);
    }

    public class FileSystemService : IFileSystemService
    {
        // Helper to get the top level window for dialogs
        private Window? GetMainWindow()
        {
            return Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public async Task<IEnumerable<FileSystemInfo>> GetDirectoryContentsAsync(string path)
        {
            return await Task.Run(() =>
            {
                var directoryInfo = new DirectoryInfo(path);
                
                // Get directories first, then files, and order alphabetically
                var directories = directoryInfo.GetDirectories()
                    .OrderBy(d => d.Name)
                    .Cast<FileSystemInfo>();
                
                var files = directoryInfo.GetFiles()
                    .OrderBy(f => f.Name)
                    .Cast<FileSystemInfo>();
                
                // Combine the two collections
                return directories.Concat(files);
            });
        }

        public async Task<string> ReadFileAsync(string filePath)
        {
            return await Task.Run(() => File.ReadAllText(filePath));
        }

        public async Task WriteFileAsync(string filePath, string content)
        {
            await Task.Run(() => File.WriteAllText(filePath, content));
        }

        public async Task<string?> OpenFileDialogAsync(string title = "Open File", string? initialDirectory = null, IEnumerable<string>? fileTypeFilter = null)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null)
                return null;

            var filters = new List<FilePickerFileType>();
            
            // Add default filter for all files
            filters.Add(new FilePickerFileType("All Files")
            {
                Patterns = new[] { "*.*" }
            });
            
            // Add custom filters if provided
            if (fileTypeFilter != null && fileTypeFilter.Any())
            {
                filters.Add(new FilePickerFileType("Supported Files")
                {
                    Patterns = fileTypeFilter.ToArray()
                });
            }
            
            // Configure the file picker
            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = filters
            };

            // Show the file picker
            var result = await mainWindow.StorageProvider.OpenFilePickerAsync(filePickerOptions);
            
            // Return the path if a file was selected
            return result.Count > 0 ? result[0].Path.LocalPath : null;
        }

        public async Task<string?> SaveFileDialogAsync(string title = "Save File", string? initialDirectory = null, string? defaultFileName = null, IEnumerable<string>? fileTypeFilter = null)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null)
                return null;

            var filters = new List<FilePickerFileType>();
            
            // Add default filter for all files
            filters.Add(new FilePickerFileType("All Files")
            {
                Patterns = new[] { "*.*" }
            });
            
            // Add custom filters if provided
            if (fileTypeFilter != null && fileTypeFilter.Any())
            {
                filters.Add(new FilePickerFileType("Supported Files")
                {
                    Patterns = fileTypeFilter.ToArray()
                });
            }
            
            // Configure the file picker
            var filePickerOptions = new FilePickerSaveOptions
            {
                Title = title,
                FileTypeChoices = filters,
                SuggestedFileName = defaultFileName
            };

            // Show the file picker
            var result = await mainWindow.StorageProvider.SaveFilePickerAsync(filePickerOptions);
            
            // Return the path if a file was selected
            return result?.Path.LocalPath;
        }

        public async Task<string?> OpenFolderDialogAsync(string title = "Open Folder", string? initialDirectory = null)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null)
                return null;

            // Configure the folder picker
            var folderPickerOptions = new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            };

            // Show the folder picker
            var result = await mainWindow.StorageProvider.OpenFolderPickerAsync(folderPickerOptions);
            
            // Return the path if a folder was selected
            return result.Count > 0 ? result[0].Path.LocalPath : null;
        }
    }
} 