namespace Swarm.Shared.EventBus; // Adjusted namespace

/// <summary>
/// Defines the contract for the event bus used for communication between Agent components and the Editor.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribes a handler to an event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The asynchronous handler delegate.</param>
    void Subscribe<TEvent>(Func<TEvent, Task> handler);

    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event object.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TEvent>(TEvent @event);
} 