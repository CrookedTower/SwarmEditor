using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks; // Assuming file operations might be async

namespace Swarm.Editor.ViewModels
{
    public partial class EditorAreaViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<DocumentViewModel> _documents;

        [ObservableProperty]
        private DocumentViewModel? _activeDocument;

        public EditorAreaViewModel()
        {
            Documents = new ObservableCollection<DocumentViewModel>();
            // Add a default empty document or load previously opened files
            AddNewDocument(); 
        }

        [RelayCommand]
        private void AddNewDocument()
        {
            var newDoc = new DocumentViewModel();
            Documents.Add(newDoc);
            ActiveDocument = newDoc;
        }

        [RelayCommand]
        private async Task OpenDocument(string filePath)
        {
            // Check if already open
            var existingDoc = Documents.FirstOrDefault(d => d.FilePath == filePath);
            if (existingDoc != null)
            {
                ActiveDocument = existingDoc;
                return;
            }

            // TODO: Replace with actual async file reading service/logic
            await Task.Delay(10); // Placeholder for async read
            string content = $"Content of {filePath}"; // Replace with actual file content
            
            var doc = new DocumentViewModel(filePath);
            doc.Content = content; // Set content after construction
            Documents.Add(doc);
            ActiveDocument = doc;
        }

        [RelayCommand]
        private void CloseDocument(DocumentViewModel? documentToClose)
        {
            var doc = documentToClose ?? ActiveDocument;
            if (doc != null)
            {
                 // TODO: Add check for IsDirty and prompt to save
                 
                Documents.Remove(doc);
                ActiveDocument = Documents.FirstOrDefault(); // Select next available or null
            }
        }

        // TODO: Add SaveDocument, SaveAllDocuments commands
    }
} 