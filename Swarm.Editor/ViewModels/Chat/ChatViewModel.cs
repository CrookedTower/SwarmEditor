using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swarm.Editor.Models.Chat;
using Swarm.Agents.EventBus;
using Swarm.Shared.Events;
using System.Linq;
using Swarm.Editor.Common.Commands;
using Swarm.Editor.Models.Events;

namespace Swarm.Editor.ViewModels.Chat
{
    public partial class ChatViewModel : ViewModelBase
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<ChatViewModel> _logger;

        [ObservableProperty]
        private string _inputText = string.Empty;

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isConnected = false;
        
        [ObservableProperty]
        private ObservableCollection<ChatMessage> _messages = new();
        
        public ChatViewModel(IEventBus eventBus, ILogger<ChatViewModel> logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Subscribe to agent events
            _eventBus.Subscribe<WorkflowStartedEvent>(HandleWorkflowStartedAsync);
            _eventBus.Subscribe<ProgressUpdateEvent>(HandleProgressUpdateAsync);
            _eventBus.Subscribe<ResultAvailableEvent>(HandleResultAvailableAsync);
            _eventBus.Subscribe<WorkflowErrorEvent>(HandleWorkflowErrorAsync);
            
            // Add welcome message
            Messages.Add(new ChatMessage
            {
                Content = "Welcome to Swarm Chat! How can I help you today?",
                IsUserMessage = false,
                Timestamp = DateTime.Now
            });
        }
        
        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(InputText))
                return;

            // Add user message
            var message = new ChatMessage
            {
                Content = InputText,
                IsUserMessage = true,
                Timestamp = DateTime.Now
            };
            AddMessage(message);

            // Generate a prompt ID for this request
            string promptId = Guid.NewGuid().ToString();

            // Send message to agent service
            IsProcessing = true;
            StatusMessage = "Processing...";

            // Send prompt event to the agents
            _eventBus.PublishAsync(new SendPromptEvent(
                promptId,
                InputText,
                null // Optional additional context
            ));

            // Clear input
            InputText = string.Empty;
        }
        
        [RelayCommand]
        private void ClearChat()
        {
            if (IsProcessing)
                return;
                
            Messages.Clear();
            
            // Add welcome message again
            Messages.Add(new ChatMessage
            {
                Content = "Chat cleared. How may I assist you?",
                IsUserMessage = false,
                Timestamp = DateTime.Now
            });
        }
        
        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(InputText) && !IsProcessing;
        }
        
        private Task HandleWorkflowStartedAsync(WorkflowStartedEvent startedEvent)
        {
            _logger.LogInformation("Workflow started: {WorkflowId}", startedEvent.WorkflowId);
            
            Dispatcher.UIThread.Post(() => {
                // Update status
                StatusMessage = $"Processing workflow {startedEvent.WorkflowId}...";
                IsProcessing = true;
                
                // Add "thinking" message
                AddAssistantMessage("Thinking...");
            });
            
            return Task.CompletedTask;
        }
        
        private Task HandleProgressUpdateAsync(ProgressUpdateEvent progressEvent)
        {
            _logger.LogInformation("Progress update: {Message}", progressEvent.Message);
            
            Dispatcher.UIThread.Post(() => {
                // Update status with progress percentage
                var percentage = (int)(progressEvent.ProgressPercentage * 100);
                StatusMessage = $"Processing... {percentage}% - {progressEvent.Message}";
                
                // Optionally update the last "thinking" message with progress info
                UpdateLastAssistantMessage($"Thinking... {progressEvent.Message}");
            });
            
            return Task.CompletedTask;
        }
        
        private Task HandleResultAvailableAsync(ResultAvailableEvent resultEvent)
        {
            _logger.LogInformation("Result available: {WorkflowId}", resultEvent.WorkflowId);
            
            Dispatcher.UIThread.Post(() => {
                // Remove the "thinking" message
                RemoveThinkingMessage();
                
                // Add result message
                AddAssistantMessage(resultEvent.Result);
                
                // Reset UI state
                IsProcessing = false;
                StatusMessage = "Ready";
            });
            
            return Task.CompletedTask;
        }
        
        private Task HandleWorkflowErrorAsync(WorkflowErrorEvent errorEvent)
        {
            _logger.LogError("Workflow error: {ErrorMessage}", errorEvent.ErrorMessage);
            
            Dispatcher.UIThread.Post(() => {
                // Remove the "thinking" message
                RemoveThinkingMessage();
                
                // Add error message
                AddAssistantMessage($"Error: {errorEvent.ErrorMessage}");
                
                // Reset UI state
                IsProcessing = false;
                StatusMessage = "Ready";
            });
            
            return Task.CompletedTask;
        }
        
        private void AddAssistantMessage(string message)
        {
            Messages.Add(new ChatMessage
            {
                Content = message,
                IsUserMessage = false,
                Timestamp = DateTime.Now
            });
        }
        
        private void UpdateLastAssistantMessage(string newMessage)
        {
            for (int i = Messages.Count - 1; i >= 0; i--)
            {
                if (!Messages[i].IsUserMessage)
                {
                    Messages[i].Content = newMessage;
                    break;
                }
            }
        }
        
        private void RemoveThinkingMessage()
        {
            var thinkingMessage = Messages.LastOrDefault(m => !m.IsUserMessage && m.Content.StartsWith("Thinking"));
            if (thinkingMessage != null)
            {
                Messages.Remove(thinkingMessage);
            }
        }

        private void AddMessage(ChatMessage message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Messages.Add(message);
            });
        }
    }
} 