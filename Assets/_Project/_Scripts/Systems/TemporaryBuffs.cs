using System;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    public enum BuffStat { AttackDamage, AttackSpeed, CooldownRate, DamageTaken, MovementSpeed }

    // 원래 스탯을 덮어쓰지 않고 계산 시 합산한다. 같은 source는 갱신, 다른 source는 합산한다.
    public sealed class TemporaryBuffs
    {
        private sealed class Entry { public float Amount; public float Until; }
        public static TemporaryBuffs Instance { get; private set; } = new();
        private readonly Dictionary<(UnityEngine.Object target, object source, BuffStat stat), Entry> entries = new();
        private readonly List<(UnityEngine.Object target, object source, BuffStat stat)> expired = new();

        public void Apply(UnityEngine.Object target, object source, BuffStat stat, float percent, float duration)
        {
            if (target == null || source == null || !float.IsFinite(percent) || !float.IsFinite(duration) || duration <= 0)
                throw new ArgumentException("버프 대상, 출처, 수치, 지속 시간을 확인하세요.");
            RuntimeTimer.Ensure();
            entries[(target, source, stat)] = new Entry { Amount = percent / 100f, Until = Time.time + duration };
        }

        public float Multiplier(UnityEngine.Object target, BuffStat stat)
        {
            float value = 1f;
            foreach (var pair in entries)
                if (pair.Key.target == target && pair.Key.stat == stat && pair.Value.Until > Time.time)
                    value += pair.Value.Amount;
            return Mathf.Max(0f, value);
        }

        public void Remove(UnityEngine.Object target, object source, BuffStat stat) => entries.Remove((target, source, stat));
        public void RemoveSource(object source)
        {
            expired.Clear();
            foreach (var pair in entries) if (ReferenceEquals(pair.Key.source, source)) expired.Add(pair.Key);
            foreach (var key in expired) entries.Remove(key);
        }
        public void Update()
        {
            expired.Clear();
            foreach (var pair in entries)
                if (pair.Key.target == null || pair.Value.Until <= Time.time) expired.Add(pair.Key);
            foreach (var key in expired) entries.Remove(key);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = new TemporaryBuffs();
    }
}
