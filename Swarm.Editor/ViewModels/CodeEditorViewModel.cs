using System;
using AvaloniaEdit.Document;
using Swarm.Editor.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Swarm.Lsp.Services;
using ReactiveUI;

namespace Swarm.Editor.ViewModels
{
    public class CodeEditorViewModel : ViewModelBase
    {
        private readonly ILogger<CodeEditorViewModel> _logger;

        private TextDocument _document = new TextDocument();
        public TextDocument Document
        {
            get => _document;
            set => this.RaiseAndSetIfChanged(ref _document, value);
        }

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set
            {
                this.RaiseAndSetIfChanged(ref _text, value ?? string.Empty);
                
                if (Document.Text != _text) 
                { 
                    Document.Text = _text;
                }
            }
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
        }

        // Constructor
        public CodeEditorViewModel(ILogger<CodeEditorViewModel>? logger = null)
        {
            _logger = logger ?? NullLogger<CodeEditorViewModel>.Instance;
            _logger.LogInformation("CodeEditorViewModel initialized.");

            // Initialize Document with empty text or load from a file if needed
            _document.Text = string.Empty;
            _isReadOnly = false; // Default to editable
        }

        public event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

        // Add other editor-related properties/commands if needed
    }
} 