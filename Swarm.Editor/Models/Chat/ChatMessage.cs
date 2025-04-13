using System;

namespace Swarm.Editor.Models.Chat
{
    public class ChatMessage
    {
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsUserMessage { get; set; }
        public string SenderName => IsUserMessage ? "User" : "Agent";
    }
} 