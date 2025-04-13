using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Threading;
using ReactiveUI;
using Swarm.Editor.Models.Chat;
using System.Linq;
using Swarm.Shared.EventBus;
using Swarm.Shared.EventBus.Events;

namespace Swarm.Editor.ViewModels.Chat
{
    /// <summary>
    /// ViewModel for the Chat Panel.
    /// </summary>
    public class ChatViewModel : ViewModelBase
    {
        private readonly ILogger<ChatViewModel> _logger;
        private readonly IEventBus _eventBus;

        private ObservableCollection<ChatMessageViewModel> _messages = new();
        public ObservableCollection<ChatMessageViewModel> Messages
        {
            get => _messages;
            set => this.RaiseAndSetIfChanged(ref _messages, value ?? new ObservableCollection<ChatMessageViewModel>());
        }

        private string? _currentMessage;
        public string? CurrentMessage
        {
            get => _currentMessage;
            set => this.RaiseAndSetIfChanged(ref _currentMessage, value);
        }

        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SendMessageCommand { get; }

        public ChatViewModel(IEventBus eventBus, ILogger<ChatViewModel> logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("ChatViewModel initialized.");

            // Add sample messages for design time or initial state
            Messages.Add(new ChatMessageViewModel("Hello! How can I assist you today?", false));

            var canSendMessage = this.WhenAnyValue(x => x.CurrentMessage, 
                                                 (msg) => !string.IsNullOrWhiteSpace(msg));

            SendMessageCommand = ReactiveCommand.CreateFromTask(SendMessageAsync, canSendMessage);
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentMessage))
            {
                _logger.LogWarning("SendMessageAsync called with null or whitespace message.");
                return;
            }
            
            string messageToSend = CurrentMessage;
            CurrentMessage = string.Empty;

            _logger.LogInformation($"Sending message: {messageToSend}");
            var userMessage = new ChatMessageViewModel(messageToSend, true);
            Messages.Add(userMessage);

            try
            {
                await _eventBus.PublishAsync(new ChatMessageSentEvent(messageToSend));
                _logger.LogInformation($"Published ChatMessageSentEvent for: {messageToSend}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing ChatMessageSentEvent for: {Message}", messageToSend);
            }
        }

        // NOTE: Methods related to receiving agent responses (HandlePromptResponseEvent, etc.)
        // have been removed as they are outside the scope of the current task (basic UI event publishing).
        // They will need to be re-added or adapted later.
    }
} 