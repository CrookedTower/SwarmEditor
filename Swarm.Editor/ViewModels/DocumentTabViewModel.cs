using System.IO;
using Swarm.Editor.ViewModels;

namespace Swarm.Editor.ViewModels
{
    public class DocumentTabViewModel : ViewModelBase
    {
        private string _filePath = string.Empty;
        private string _content = string.Empty;
        private string _displayName = string.Empty;
        private bool _isModified;
        private bool _isActive;
        private bool _hasCustomDisplayName;

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value ?? string.Empty);
        }

        public string Content
        {
            get => _content;
            set
            {
                if (SetProperty(ref _content, value))
                {
                    IsModified = true;
                }
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set 
            { 
                if (SetProperty(ref _displayName, value))
                {
                    _hasCustomDisplayName = true;
                }
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set => SetProperty(ref _isModified, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string TabTitle => IsModified ? DisplayName + "*" : DisplayName;

        public DocumentTabViewModel()
        {
            _filePath = string.Empty;
            _content = string.Empty;
            _displayName = "Untitled";
            IsModified = false;
            IsActive = false;
        }

        public DocumentTabViewModel(string? filePath, string content, string? displayName = null)
        {
            _filePath = filePath ?? string.Empty;
            _content = content ?? string.Empty;
            _displayName = displayName ?? Path.GetFileName(filePath ?? string.Empty);
            
            if (string.IsNullOrEmpty(_displayName))
            {
                _displayName = "Untitled";
            }
            IsModified = false;
            IsActive = false;
            _hasCustomDisplayName = displayName != null;
        }

        public DocumentTabViewModel(string? filePath, string content, string customDisplayName, bool useCustomName)
        {
            _filePath = filePath ?? string.Empty;
            _content = content ?? string.Empty;
            _displayName = customDisplayName ?? "Untitled";
            IsModified = false;
            IsActive = false;
            _hasCustomDisplayName = true;
        }
    }
} 