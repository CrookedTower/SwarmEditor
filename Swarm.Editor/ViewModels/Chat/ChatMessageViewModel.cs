using System;
using System.Globalization;
using ReactiveUI;

namespace Swarm.Editor.ViewModels.Chat
{
    public class ChatMessageViewModel : ViewModelBase
    {
        private string _message;
        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        private bool _isFromUser;
        public bool IsFromUser
        {
            get => _isFromUser;
            set => this.RaiseAndSetIfChanged(ref _isFromUser, value);
        }

        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get => _timestamp;
            set => this.RaiseAndSetIfChanged(ref _timestamp, value);
        }

        public string FormattedTime => Timestamp.ToString("HH:mm", CultureInfo.InvariantCulture);

        public ChatMessageViewModel(string message, bool isFromUser)
        {
            _message = message;
            _isFromUser = isFromUser;
            _timestamp = DateTime.Now;
        }
    }
} 