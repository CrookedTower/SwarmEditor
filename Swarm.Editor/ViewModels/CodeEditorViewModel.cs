using Swarm.Editor.ViewModels;

namespace Swarm.Editor.ViewModels
{
    public class CodeEditorViewModel : ViewModelBase
    {
        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        // Add other editor-related properties/commands if needed
    }
} 