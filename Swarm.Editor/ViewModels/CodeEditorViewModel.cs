using System;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using Swarm.Editor.ViewModels;

namespace Swarm.Editor.ViewModels
{
    public partial class CodeEditorViewModel : ViewModelBase
    {
        private TextDocument _document = new TextDocument();
        public TextDocument Document
        {
            get => _document;
            set => SetProperty(ref _document, value);
        }

        public string Text
        {
            get => Document.Text;
            set
            {
                if (Document.Text != value)
                {
                    Document.Text = value ?? string.Empty;
                    OnPropertyChanged(); // Notify that Text has changed
                }
            }
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        // Constructor
        public CodeEditorViewModel()
        {
            // Initialize Document with empty text or load from a file if needed
            _document.Text = string.Empty;
            _isReadOnly = false; // Default to editable
        }

        // Add other editor-related properties/commands if needed
    }
} 