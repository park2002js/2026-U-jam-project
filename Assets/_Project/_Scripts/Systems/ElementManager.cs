using System.Collections.Generic;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Player;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    public enum ElementType { Burn, Freeze, Shock, Bleed, Wind }

    /// <summary>적에게 적용된 속성 하나의 독립적인 수명과 출혈 스택.</summary>
    public class ElementState
    {
        public ElementType Type { get; internal set; }
        public int Stacks { get; internal set; }
        public float ExpiresAt { get; internal set; }
        internal float NextTick;
        internal PlayerStatus Player;
        internal GameObject Visual;
    }

    /// <summary>
    /// 속성별 부여/갱신/만료와 실제 효과를 중앙 처리한다. 여러 속성은 독립적으로 유지된다.
    /// 미지정 수치는 5초, 화상 1초 주기, 출혈 5스택/최대 체력 5%/상한 1000으로 정했다.
    /// 속성 조합은 보류하며 바람은 속성 보유만 기록한다.
    /// </summary>
    public class ElementManager
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
        private readonly Dictionary<(EnemyBase Enemy, ElementType Type), ElementState> states = new();

        /// <summary>단일 적에게 속성을 부여한다. player 생략 시 현재 플레이어를 사용하며 즉시 발생한 출혈 피해를 반환한다.</summary>
        public float Apply(EnemyBase enemy, ElementType type, PlayerStatus player = null)
        {
            if (!Alive(enemy)) return 0;

            RuntimeTimer.Ensure();
            player = player != null ? player : PlayerStatus.Instance;

            var key = (enemy, type);
            float duration = Mathf.Max(0.01f, Duration);
            if (!states.TryGetValue(key, out var state) || state.ExpiresAt <= Time.time)
            {
                if (state != null) Clear(state);
                state = new ElementState
                {
                    Type = type, NextTick = Time.time + Mathf.Max(0.01f, BurnInterval),
                    Visual = RuntimeTimer.Instance.CreateElementVfx(enemy, type)
                };
                states[key] = state;
            }

            state.Player = player;
            state.ExpiresAt = Time.time + duration;

            switch (type)
            {
                case ElementType.Freeze:
                    BuffManager.Instance.SetTimed(enemy.Status, state, BuffStat.MovementSpeed, -FreezePercent, duration);
                    BuffManager.Instance.SetTimed(enemy.Status, state, BuffStat.AttackSpeed, -FreezePercent, duration);
                    break;
                case ElementType.Shock:
                    BuffManager.Instance.SetTimed(enemy.Status, state, BuffStat.DamageTaken, ShockPercent, duration);
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

        /// <summary>지정한 속성과 출혈 스택을 조회한다. 다른 속성의 수명에는 영향을 주지 않는다.</summary>
        public bool TryGet(EnemyBase enemy, ElementType type, out ElementState state)
        {
            state = null;
            return Alive(enemy) && states.TryGetValue((enemy, type), out state) && state.ExpiresAt > Time.time;
        }

        /// <summary>속성 종류를 구분하지 않는 기존 호출자를 위해 활성 속성 하나를 반환한다.</summary>
        public bool TryGet(EnemyBase enemy, out ElementState state)
        {
            state = null;
            if (!Alive(enemy)) return false;
            foreach (var pair in states)
                if (pair.Key.Enemy == enemy && pair.Value.ExpiresAt > Time.time) { state = pair.Value; return true; }
            return false;
        }

        /// <summary>RuntimeTimer.Update에서 화상 틱과 속성 만료를 처리한다.</summary>
        public void Update()
        {
            // 피해/사망 이벤트가 다른 적에게 속성을 줄 수 있어 스냅샷을 순회한다.
            foreach (var pair in new List<KeyValuePair<(EnemyBase Enemy, ElementType Type), ElementState>>(states))
            {
                var enemy = pair.Key.Enemy; var state = pair.Value;
                if (Alive(enemy) && state.Type == ElementType.Burn)
                {
                    float lastTick = Mathf.Min(Time.time, state.ExpiresAt);
                    while (state.NextTick <= lastTick && Alive(enemy) &&
                        states.TryGetValue(pair.Key, out var active) && ReferenceEquals(active, state))
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
                    if (states.TryGetValue(pair.Key, out var current) && ReferenceEquals(current, state)) states.Remove(pair.Key);
                    Clear(state);
                }
            }
        }
        private static void Clear(ElementState state)
        {
            BuffManager.Instance.RemoveSource(state);
            if (state.Visual != null) { state.Visual.SetActive(false); Object.Destroy(state.Visual); }
        }
        private static bool Alive(EnemyBase enemy) => enemy != null && enemy.isActiveAndEnabled && enemy.Status != null && enemy.Status.HP > 0;

        // Unity가 실행 시작 시 첫 씬을 불러오기 전에 이 static 메서드를 자동 호출한다.
        // SubsystemRegistration 시점에 정적 상태를 초기화하여 Domain Reload를 꺼도 이전 플레이의 상태가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = new ElementManager();
    }
}
