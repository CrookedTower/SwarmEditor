using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swarm.Editor.Models.Chat;
using System.Linq;
using Swarm.Editor.Common.Commands;
using Swarm.Shared.EventBus;
using Swarm.Shared.EventBus.Events;

namespace Swarm.Editor.ViewModels.Chat
{
    /// <summary>
    /// ViewModel representing a single message in the chat.
    /// </summary>
    public partial class ChatMessageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? _sender;

        [ObservableProperty]
        private string? _text;

        [ObservableProperty]
        private bool _isUserMessage;
    }

    /// <summary>
    /// ViewModel for the Chat Panel.
    /// </summary>
    public partial class ChatViewModel : ObservableObject
    {
        private readonly ILogger<ChatViewModel> _logger;
        private readonly IEventBus _eventBus;

        [ObservableProperty]
        private ObservableCollection<ChatMessageViewModel> _messages = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
        private string? _currentMessage;

        public ChatViewModel(IEventBus eventBus, ILogger<ChatViewModel> logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("ChatViewModel initialized.");

            // Add sample messages for design time or initial state
            Messages.Add(new ChatMessageViewModel { Sender = "Agent", Text = "Hello! How can I assist you today?", IsUserMessage = false });
        }

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(CurrentMessage);

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async Task SendMessage()
        {
            // Redundant check due to CanExecute, but good practice
            if (!CanSendMessage() || CurrentMessage == null) return;

            _logger.LogInformation($"Send button clicked. Message: {CurrentMessage}");
            var userMessage = new ChatMessageViewModel { Sender = "User", Text = CurrentMessage, IsUserMessage = true };
            Messages.Add(userMessage);

            // Publish the event using the shared event bus
            await _eventBus.PublishAsync(new ChatMessageSentEvent(CurrentMessage));
            _logger.LogInformation($"Published ChatMessageSentEvent for: {CurrentMessage}");

            CurrentMessage = string.Empty; // Clear the input box
        }

        // NOTE: Methods related to receiving agent responses (HandlePromptResponseEvent, etc.)
        // have been removed as they are outside the scope of the current task (basic UI event publishing).
        // They will need to be re-added or adapted later.
    }
} 