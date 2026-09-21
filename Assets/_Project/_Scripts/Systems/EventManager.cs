using System;
using System.Collections.Generic;
using Ujam.Runtime.Item;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    /// <summary>플레이어별 인자를 전달하는 중앙 이벤트 허브. 구독은 Item.Equip, 해제는 Item.Unequip이 수행한다.</summary>
    public sealed class EventManager
    {
        public static EventManager Instance { get; private set; } = new();
        private readonly Dictionary<ItemTrigger, Action<ItemEvent>> handlers = new();

        /// <summary>선택된 enum 이벤트에 Execute를 구독한다. 일반적으로 ItemRuntime만 호출한다.</summary>
        public void Subscribe(ItemTrigger trigger, Action<ItemEvent> handler)
        { handlers.TryGetValue(trigger, out var current); handlers[trigger] = current + handler; }

        /// <summary>장착 해제/파괴 시 같은 함수의 구독을 해제한다.</summary>
        public void Unsubscribe(ItemTrigger trigger, Action<ItemEvent> handler)
        { if (handlers.TryGetValue(trigger, out var current)) handlers[trigger] = current - handler; }

        /// <summary>Shooter, SkillManager, PlayerStatus, EnemyBase가 발생한 사실을 알릴 때 호출한다.</summary>
        public void Publish(ItemEvent signal)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            if (!handlers.TryGetValue(signal.Trigger, out var current) || current == null) return;
            foreach (Action<ItemEvent> handler in current.GetInvocationList())
            {
                try { handler(signal); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = new EventManager();
    }
}
