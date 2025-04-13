using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Swarm.Shared.EventBus;

/// <summary>
/// A simple in-memory event bus implementation using weak references.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<WeakReference<Delegate>>> _subscriptions = new();

    public void Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);
        var handlerWrapper = new Func<object, Task>(async msg => await handler((TEvent)msg));
        var weakHandler = new WeakReference<Delegate>(handlerWrapper);

        _subscriptions.AddOrUpdate(eventType,
            _ => new List<WeakReference<Delegate>> { weakHandler },
            (_, existingHandlers) =>
            {
                lock (existingHandlers) // Basic locking for list modification
                {
                    // Clean up dead references before adding - explicitly type 'out'
                    existingHandlers.RemoveAll(wr => !wr.TryGetTarget(out Delegate? _)); 
                    existingHandlers.Add(weakHandler);
                }
                return existingHandlers;
            });
    }

    public async Task PublishAsync<TEvent>(TEvent @event)
    {
        var eventType = typeof(TEvent);
        if (_subscriptions.TryGetValue(eventType, out var handlers))
        {
            List<WeakReference<Delegate>> handlersSnapshot;
            lock (handlers) // Lock to get a snapshot and clean up
            {
                handlers.RemoveAll(wr => !wr.TryGetTarget(out _)); 
                handlersSnapshot = handlers.ToList(); // Create a copy for safe iteration
            }

            // TODO: Consider parallel execution or error handling strategy (e.g., AggregateException)
            foreach (var weakHandler in handlersSnapshot)
            {
                if (weakHandler.TryGetTarget(out var handlerDelegate))
                {
                    try
                    {
                        await ((Func<object, Task>)handlerDelegate)(@event!);
                    }
                    catch (Exception ex)
                    {
                        // Log error - Consider how to handle subscriber exceptions
                        Console.Error.WriteLine($"[InMemoryEventBus] Error in subscriber for {eventType.Name}: {ex}");
                    }
                }
            }
        }
    }

    // Optional: Implement Unsubscribe if needed later
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);
        if (_subscriptions.TryGetValue(eventType, out var handlers))
        {
            lock (handlers)
            {
                // Find the specific weak reference wrapping the original handler's target method
                var targetMethod = handler.Method;
                handlers.RemoveAll(wr => 
                    !wr.TryGetTarget(out Delegate? handlerDelegate) || // Remove dead references - explicitly type 'out'
                    (handlerDelegate is Func<object, Task> wrapper && 
                     wrapper.Method.DeclaringType == typeof(InMemoryEventBus) && // Ensure it's our wrapper
                     wrapper.Target is object targetInstance && // Check if closure target matches
                     targetInstance.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                        .Any(f => f.Name == "handler" && f.GetValue(targetInstance) == (object)handler)) // Check if the captured handler matches
                     // Note: This comparison logic might need refinement depending on how delegates/closures are captured.
                );
            }
        }
    }
} 