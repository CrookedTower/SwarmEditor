using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Swarm.Editor.ViewModels.Chat
{
    public partial class ChatMessageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _message = string.Empty;
        
        [ObservableProperty]
        private bool _isFromUser;
        
        [ObservableProperty]
        private DateTime _timestamp;

        public string FormattedTime => Timestamp.ToString("t"); // Short time pattern
    }
} 