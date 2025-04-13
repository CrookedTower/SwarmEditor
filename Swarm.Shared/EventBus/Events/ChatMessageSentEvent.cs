namespace Swarm.Shared.EventBus.Events;

/// <summary>
/// Event published when a chat message is sent from the UI.
/// </summary>
/// <param name="Message">The content of the message.</param>
public record ChatMessageSentEvent(string Message); 