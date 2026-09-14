using System;
using System.Collections.Generic;
using Ujam.Runtime.Item;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    public sealed class CombatEvents
    {
        public static CombatEvents Instance { get; private set; } = new();
        private readonly Dictionary<ItemTrigger, Action<ItemUseContext>> handlers = new();
        public void Subscribe(ItemTrigger trigger, Action<ItemUseContext> handler)
        {
            handlers.TryGetValue(trigger, out var current);
            handlers[trigger] = current + handler;
        }
        public void Unsubscribe(ItemTrigger trigger, Action<ItemUseContext> handler)
        {
            if (handlers.TryGetValue(trigger, out var current)) handlers[trigger] = current - handler;
        }
        public void Publish(ItemUseContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!handlers.TryGetValue(context.Trigger, out var current) || current == null) return;
            foreach (Action<ItemUseContext> handler in current.GetInvocationList())
            {
                try { handler(context); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = new CombatEvents();
    }
}
