using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using System;
using System.Windows.Input;

namespace Swarm.Editor.ViewModels
{
    public class EditorAreaViewModel : ViewModelBase
    {
        private ObservableCollection<DocumentViewModel> _documents;
        public ObservableCollection<DocumentViewModel> Documents
        {
            get => _documents;
            set => this.RaiseAndSetIfChanged(ref _documents, value ?? new ObservableCollection<DocumentViewModel>());
        }

        private DocumentViewModel? _activeDocument;
        public DocumentViewModel? ActiveDocument
        {
            get => _activeDocument;
            set => this.RaiseAndSetIfChanged(ref _activeDocument, value);
        }

        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AddNewDocumentCommand { get; }
        public ReactiveCommand<string, System.Reactive.Unit> OpenDocumentCommand { get; }
        public ReactiveCommand<DocumentViewModel?, System.Reactive.Unit> CloseDocumentCommand { get; }

        public EditorAreaViewModel()
        {
            _documents = new ObservableCollection<DocumentViewModel>();

            AddNewDocumentCommand = ReactiveCommand.Create(AddNewDocumentInternal);
            OpenDocumentCommand = ReactiveCommand.CreateFromTask<string>(OpenDocumentInternalAsync); 
            CloseDocumentCommand = ReactiveCommand.Create<DocumentViewModel?>(CloseDocumentInternal); 

            AddNewDocumentInternal(); 
        }

        private void AddNewDocumentInternal()
        {
            var newDoc = new DocumentViewModel();
            Documents.Add(newDoc);
            ActiveDocument = newDoc;
        }

        private async Task OpenDocumentInternalAsync(string filePath)
        {
            var existingDoc = Documents.FirstOrDefault(d => d.FilePath == filePath);
            if (existingDoc != null)
            {
                ActiveDocument = existingDoc;
                return;
            }

            await Task.Delay(10);
            string content = $"Content of {filePath}";
            
            var doc = new DocumentViewModel(filePath);
            Documents.Add(doc);
            ActiveDocument = doc;
        }

        private void CloseDocumentInternal(DocumentViewModel? documentToClose)
        {
            var doc = documentToClose ?? ActiveDocument;
            if (doc != null)
            {
                Documents.Remove(doc);
                ActiveDocument = Documents.FirstOrDefault();
            }
        }

        // TODO: Add SaveDocument, SaveAllDocuments commands
    }
} 