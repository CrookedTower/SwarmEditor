using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swarm.Agents.EventBus;

namespace Swarm.Editor.Models.Events
{
    public class InMemoryEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public void Subscribe<TEvent>(Func<TEvent, Task> handler)
        {
            var eventType = typeof(TEvent);
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<Delegate>();
            }
            _handlers[eventType].Add(handler);
        }

        public async Task PublishAsync<TEvent>(TEvent @event)
        {
            var eventType = typeof(TEvent);
            if (!_handlers.ContainsKey(eventType))
            {
                return;
            }

            var tasks = new List<Task>();
            foreach (var handler in _handlers[eventType])
            {
                if (handler is Func<TEvent, Task> typedHandler)
                {
                    tasks.Add(typedHandler(@event));
                }
            }

            await Task.WhenAll(tasks);
        }
    }
} 