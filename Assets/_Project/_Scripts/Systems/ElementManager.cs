using System.Collections.Generic;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Player;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    public enum ElementType { Burn, Freeze, Shock, Bleed, Wind }

    /// <summary>적 하나의 속성과 스택. 한 적은 하나의 속성만 보유한다.</summary>
    public sealed class ElementState
    {
        public ElementType Type { get; internal set; }
        public int Stacks { get; internal set; }
        public float ExpiresAt { get; internal set; }
        internal float NextTick;
        internal PlayerStatus Player;
    }

    /// <summary>
    /// 속성 부여/교체/갱신과 실제 효과를 중앙 처리한다. 호출자는 적과 속성만 전달하면 된다.
    /// 미지정 수치는 5초, 화상 1초 주기, 출혈 5스택/최대 체력 5%/상한 1000으로 정했다.
    /// 속성 조합은 보류하며 바람은 속성 보유만 기록한다.
    /// </summary>
    public sealed class ElementManager
    {
        public static ElementManager Instance { get; private set; } = new();
        public float Duration { get; set; } = 5f;
        public float BurnAttackPercent { get; set; } = 111f;
        public float BurnInterval { get; set; } = 1f;
        public float FreezePercent { get; set; } = 10f;
        public float ShockPercent { get; set; } = 10f;
        public int BleedThreshold { get; set; } = 5;
        public float BleedMaxHealthPercent { get; set; } = 5f;
        public float BleedDamageCap { get; set; } = 1000f;
        private readonly Dictionary<EnemyBase, ElementState> states = new();

        /// <summary>단일 적에게 속성을 부여한다. player 생략 시 현재 플레이어를 사용하며 즉시 발생한 출혈 피해를 반환한다.</summary>
        public float Apply(EnemyBase enemy, ElementType type, PlayerStatus player = null)
        {
            if (!Alive(enemy)) return 0;
            RuntimeTimer.Ensure();
            player = player != null ? player : PlayerStatus.Instance;
            if (!states.TryGetValue(enemy, out var state) || state.Type != type || state.ExpiresAt < Time.time)
            {
                if (state != null) BuffManager.Instance.RemoveSource(state);
                state = new ElementState { Type = type, NextTick = Time.time + Mathf.Max(0.01f, BurnInterval) };
                states[enemy] = state;
            }
            state.Player = player;
            state.ExpiresAt = Time.time + Mathf.Max(0.01f, Duration);
            switch (type)
            {
                case ElementType.Freeze:
                    BuffManager.Instance.SetTimed(enemy.Status, state, BuffStat.MovementSpeed, -FreezePercent, Duration);
                    BuffManager.Instance.SetTimed(enemy.Status, state, BuffStat.AttackSpeed, -FreezePercent, Duration);
                    break;
                case ElementType.Shock:
                    BuffManager.Instance.SetTimed(enemy.Status, state, BuffStat.DamageTaken, ShockPercent, Duration);
                    break;
                case ElementType.Bleed:
                    if (++state.Stacks >= Mathf.Max(1, BleedThreshold))
                    {
                        state.Stacks = 0;
                        float damage = Mathf.Min(BleedDamageCap, enemy.Status.MaxHealth * BleedMaxHealthPercent / 100f *
                            (player != null ? player.ElementDamageMultiplier : 1f));
                        return enemy.TakeDamage(new DamageInfo(damage, "출혈", DamageSourceKind.Player, player, true));
                    }
                    break;
            }
            return 0;
        }

        /// <summary>여러 적에게 같은 속성을 줄 때 사용한다. null 목록은 미적중으로 처리한다.</summary>
        public void Apply(IReadOnlyList<EnemyBase> enemies, ElementType type, PlayerStatus player = null)
        { if (enemies != null) foreach (var enemy in enemies) Apply(enemy, type, player); }

        /// <summary>UI나 추가 효과에서 현재 속성과 출혈 스택을 조회할 때 사용한다.</summary>
        public bool TryGet(EnemyBase enemy, out ElementState state)
        {
            state = null;
            return Alive(enemy) && states.TryGetValue(enemy, out state) && state.ExpiresAt > Time.time;
        }

        /// <summary>RuntimeTimer.Update에서 화상 틱과 속성 만료를 처리한다.</summary>
        public void Update()
        {
            // 피해/사망 이벤트가 다른 적에게 속성을 줄 수 있어 스냅샷을 순회한다.
            foreach (var pair in new List<KeyValuePair<EnemyBase, ElementState>>(states))
            {
                var enemy = pair.Key; var state = pair.Value;
                if (Alive(enemy) && state.Type == ElementType.Burn)
                {
                    float lastTick = Mathf.Min(Time.time, state.ExpiresAt);
                    while (state.NextTick <= lastTick && Alive(enemy) &&
                        states.TryGetValue(enemy, out var active) && ReferenceEquals(active, state))
                    {
                        state.NextTick += Mathf.Max(0.01f, BurnInterval);
                        var player = state.Player;
                        if (player == null) continue;
                        float coefficient = BurnAttackPercent + BuffManager.Instance.Percent(player, BuffStat.BurnCoefficient);
                        float damage = player.AttackDamage * Mathf.Max(0, coefficient) / 100f * player.ElementDamageMultiplier;
                        enemy.TakeDamage(new DamageInfo(damage, "화상", DamageSourceKind.Player, player));
                    }
                }
                if (!Alive(enemy) || state.ExpiresAt <= Time.time)
                {
                    if (states.TryGetValue(enemy, out var current) && ReferenceEquals(current, state)) states.Remove(enemy);
                    BuffManager.Instance.RemoveSource(state);
                }
            }
        }
        private static bool Alive(EnemyBase enemy) => enemy != null && enemy.isActiveAndEnabled && enemy.Status != null && enemy.Status.HP > 0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = new ElementManager();
    }
}
