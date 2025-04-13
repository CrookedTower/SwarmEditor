using System.IO;
using ReactiveUI;

namespace Swarm.Editor.ViewModels
{
    public class DocumentViewModel : ViewModelBase
    {
        private string? _title;
        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            set => this.RaiseAndSetIfChanged(ref _filePath, value);
        }

        private string? _content;
        public string? Content
        {
            get => _content;
            set 
            {
                // Just call RaiseAndSetIfChanged for now to test compilation
                this.RaiseAndSetIfChanged(ref _content, value);
                
                // Temporarily comment out the IsDirty logic
                // bool changed = this.RaiseAndSetIfChanged(ref _content, value);
                // 
                // // If the value changed, apply the IsDirty logic
                // if (changed)
                // {
                //     if (FilePath != null) // Only mark existing files as dirty initially
                //     {
                //          IsDirty = true;
                //     }
                //     // Note: We might need more nuanced IsDirty logic later,
                //     // e.g., checking if the new content differs from the last saved state.
                // }
            }
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set => this.RaiseAndSetIfChanged(ref _isDirty, value);
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
        }

        // Constructor for new, unsaved documents
        public DocumentViewModel()
        {
             // Initialize properties as needed
             _title = "Untitled";
             _content = string.Empty;
             _filePath = null;
             _isDirty = false; // New files aren't dirty initially
             _isReadOnly = false;
        }

        // Constructor for opening existing documents
        public DocumentViewModel(string filePath)
        {
            _filePath = filePath;
            _title = System.IO.Path.GetFileName(filePath);
            // TODO: Load actual content, determine read-only status etc.
            _content = $"Content for {filePath}"; // Placeholder
            _isReadOnly = false; // Example
            _isDirty = false;
        }
    }
} 