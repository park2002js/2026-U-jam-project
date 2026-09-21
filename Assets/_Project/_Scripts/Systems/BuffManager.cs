using System;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UJam.Runtime.Player;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    public enum BuffStat
    {
        AttackDamage, AttackSpeed, MaxHealth, Damage, ElementDamage, BurnCoefficient,
        CooldownReduction, CurrencyGain, MovementSpeed, DamageTaken, Stun
    }

    /// <summary>한 대상에 적용된 합연산 보정. ExpiresAt이 무한대이면 조건 유지형이며 Remove로 해제한다.</summary>
    public sealed class BuffEntry
    {
        public UnityEngine.Object Target { get; internal set; }
        public object Source { get; internal set; }
        public BuffStat Stat { get; internal set; }
        public float Percent { get; internal set; }
        public float ExpiresAt { get; internal set; }
    }

    /// <summary>
    /// 모든 플레이어 버프/적 디버프의 수명 관리자. 같은 대상+Source+Stat은 갱신하며 다른 Source는 합연산한다.
    /// 영구 보유 효과와 장판 B에는 SetCondition, T초짜리 효과에는 SetTimed를 사용한다.
    /// </summary>
    public sealed class BuffManager
    {
        public static BuffManager Instance { get; private set; } = new();
        private readonly List<BuffEntry> playerBuffs = new();
        private readonly List<BuffEntry> enemyBuffs = new();
        public IReadOnlyList<BuffEntry> PlayerBuffs => playerBuffs.AsReadOnly();
        public IReadOnlyList<BuffEntry> EnemyBuffs => enemyBuffs.AsReadOnly();

        /// <summary>플레이어에게 T초간 보정을 적용한다. 감소는 음수 Percent로 전달한다.</summary>
        public void SetTimed(PlayerStatus player, object source, BuffStat stat, float percent, float seconds)
            => Set(playerBuffs, player, source, stat, percent, Deadline(seconds));
        /// <summary>적에게 T초간 디버프를 적용한다. 재호출하면 현재 시점부터 지속 시간을 갱신한다.</summary>
        public void SetTimed(EnemyStatus enemy, object source, BuffStat stat, float percent, float seconds)
            => Set(enemyBuffs, enemy, source, stat, percent, Deadline(seconds));
        /// <summary>보유 중 또는 체력 조건을 만족하는 동안의 플레이어 보정. 해제 시 Remove/RemoveSource가 필요하다.</summary>
        public void SetCondition(PlayerStatus player, object source, BuffStat stat, float percent)
            => Set(playerBuffs, player, source, stat, percent, float.PositiveInfinity);
        /// <summary>장판 B 내부 등 조건 유지형 적 디버프. Exit에서 같은 source로 Remove한다.</summary>
        public void SetCondition(EnemyStatus enemy, object source, BuffStat stat, float percent)
            => Set(enemyBuffs, enemy, source, stat, percent, float.PositiveInfinity);

        private static float Deadline(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            return Time.time + seconds;
        }
        private void Set(List<BuffEntry> list, UnityEngine.Object target, object source, BuffStat stat, float percent, float until)
        {
            if (target == null || source == null || !float.IsFinite(percent)) throw new ArgumentException("버프 인자를 확인하세요.");
            RuntimeTimer.Ensure();
            var entry = list.Find(x => x.Target == target && ReferenceEquals(x.Source, source) && x.Stat == stat);
            if (entry == null)
            {
                entry = new BuffEntry { Target = target, Source = source, Stat = stat };
                list.Add(entry);
            }
            entry.Percent = percent; entry.ExpiresAt = until;
            if (stat == BuffStat.MaxHealth && target is PlayerStatus player) player.RefreshHealth();
        }

        /// <summary>스탯 계산 시 호출한다. 10%와 20%가 있으면 30을 반환한다.</summary>
        public float Percent(UnityEngine.Object target, BuffStat stat)
        {
            var list = target is PlayerStatus ? playerBuffs : enemyBuffs;
            float total = 0;
            foreach (var entry in list)
                if (entry.Target == target && entry.Stat == stat && entry.ExpiresAt > Time.time) total += entry.Percent;
            return total;
        }
        /// <summary>일반 배율 계산용. 음수 보정의 합이 -100%보다 낮아도 배율은 0 미만이 되지 않는다.</summary>
        public float Multiplier(UnityEngine.Object target, BuffStat stat) => Mathf.Max(0, 1 + Percent(target, stat) / 100f);

        /// <summary>특정 장판에서 나왔을 때처럼 한 대상의 한 보정을 해제한다.</summary>
        public void Remove(UnityEngine.Object target, object source, BuffStat stat)
        {
            var list = target is PlayerStatus ? playerBuffs : enemyBuffs;
            int removed = list.RemoveAll(x => x.Target == target && ReferenceEquals(x.Source, source) && x.Stat == stat);
            if (removed > 0 && stat == BuffStat.MaxHealth && target is PlayerStatus player) player.RefreshHealth();
        }

        /// <summary>아이템/장판/속성의 수명이 끝날 때 출처 전체를 제거한다. 다른 출처의 효과는 유지된다.</summary>
        public void RemoveSource(object source)
        {
            var changed = new HashSet<PlayerStatus>();
            foreach (var entry in playerBuffs)
                if (ReferenceEquals(entry.Source, source) && entry.Stat == BuffStat.MaxHealth && entry.Target is PlayerStatus player) changed.Add(player);
            playerBuffs.RemoveAll(x => ReferenceEquals(x.Source, source));
            enemyBuffs.RemoveAll(x => ReferenceEquals(x.Source, source));
            foreach (var player in changed) if (player != null) player.RefreshHealth();
        }

        /// <summary>RuntimeTimer.Update가 호출한다. 만료된 보정과 파괴된 대상의 참조를 정리한다.</summary>
        public void Update()
        {
            var changed = new HashSet<PlayerStatus>();
            foreach (var entry in playerBuffs)
                if (entry.ExpiresAt <= Time.time && entry.Stat == BuffStat.MaxHealth && entry.Target is PlayerStatus player) changed.Add(player);
            playerBuffs.RemoveAll(x => x.Target == null || x.ExpiresAt <= Time.time);
            enemyBuffs.RemoveAll(x => x.Target == null || x.ExpiresAt <= Time.time);
            foreach (var player in changed) if (player != null) player.RefreshHealth();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = new BuffManager();
    }
}
