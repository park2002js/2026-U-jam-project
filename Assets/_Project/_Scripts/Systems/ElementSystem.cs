using System;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    public enum ElementType { Burn, Freeze, Shock, Wind, Bleed }

    public sealed class ElementSystem
    {
        public const float Duration = 5f;
        public static ElementSystem Instance { get; private set; } = new();
        private readonly Dictionary<EnemyBase, (ElementType element, float until)> states = new();
        private readonly List<EnemyBase> expired = new();
        public event Action<EnemyBase, ElementType?> Changed;

        // 같은 속성은 남은 시간을 5초로 갱신하고, 다른 속성은 즉시 교체한다.
        public void Apply(IReadOnlyList<EnemyBase> enemies, ElementType element)
        {
            if (enemies == null) return;
            RuntimeTimer.Ensure();
            foreach (var enemy in enemies)
            {
                if (!IsAlive(enemy)) continue;
                states[enemy] = (element, Time.time + Duration);
                Changed?.Invoke(enemy, element);
            }
        }
        public bool TryGet(EnemyBase enemy, out ElementType element)
        {
            element = default;
            if (!IsAlive(enemy) || !states.TryGetValue(enemy, out var state) || state.until <= Time.time) return false;
            element = state.element;
            return true;
        }
        public void Update()
        {
            expired.Clear();
            foreach (var pair in states)
                if (!IsAlive(pair.Key) || pair.Value.until <= Time.time) expired.Add(pair.Key);
            foreach (var enemy in expired)
            {
                states.Remove(enemy);
                Changed?.Invoke(enemy, null);
            }
        }
        private static bool IsAlive(EnemyBase enemy) => enemy != null && enemy.isActiveAndEnabled &&
            enemy.Status != null && enemy.Status.HP > 0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = new ElementSystem();
    }
}
