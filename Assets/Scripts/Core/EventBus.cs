using System;
using System.Collections.Generic;

namespace Daeume.Core
{
    public sealed class EventBus
    {
        private readonly Dictionary<Type, Delegate> handlers = new();

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(T);
            handlers.TryGetValue(eventType, out var existing);
            handlers[eventType] = Delegate.Combine(existing, handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            var eventType = typeof(T);
            if (!handlers.TryGetValue(eventType, out var existing))
            {
                return;
            }

            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
            {
                handlers.Remove(eventType);
                return;
            }

            handlers[eventType] = remaining;
        }

        public void Publish<T>(T message)
        {
            if (handlers.TryGetValue(typeof(T), out var existing))
            {
                ((Action<T>)existing).Invoke(message);
            }
        }

        public void Clear()
        {
            handlers.Clear();
        }
    }
}
