using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace Swarm.Editor.ViewModels
{
    public partial class DocumentViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private string? _filePath;

        [ObservableProperty]
        private string? _content;

        [ObservableProperty]
        private bool _isDirty;

        [ObservableProperty]
        private bool _isReadOnly;

        // Constructor for new, unsaved documents
        public DocumentViewModel()
        {
             // Header is already "Untitled", Content is Empty, FilePath is null
        }

        // Constructor for opening existing documents
        public DocumentViewModel(string filePath)
        {
            _filePath = filePath;
            _title = System.IO.Path.GetFileName(filePath);
            // TODO: Load actual content, determine read-only status etc.
            _content = $"Content for {filePath}";
            _isReadOnly = false; // Example
            _isDirty = false;
        }

        // Partial method to react to property changes (e.g., mark as dirty)
        partial void OnContentChanged(string? value)
        {
            if (FilePath != null) // Only mark existing files as dirty initially
            {
                 IsDirty = true;
            }
            // For new files, IsDirty can be set upon first save attempt or explicit action
        }
    }
} 