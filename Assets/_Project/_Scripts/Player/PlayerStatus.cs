using System;
using System.Collections.Generic;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Systems;
using Ujam.Runtime.Item;
using UnityEngine;
using RuntimeItem = Ujam.Runtime.Item.Item;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// 플레이어 기본 수치, 현재 체력, 아이템별 사용/연속 적중/스킬 카운트를 관리한다.
    /// 최종 스탯은 BuffManager의 합연산 보정을 읽어 계산하며 기본 수치를 직접 누적 변경하지 않는다.
    /// </summary>
    public class PlayerStatus : MonoBehaviour
    {
        public static PlayerStatus Instance { get; private set; }
        [SerializeField, Min(0.01f)] private float _attackDamage = 10f;
        [SerializeField, Min(0.01f)] private float _maxHealth = 100f;
        [SerializeField, Min(0.01f)] private float _attackSpeed = 5f;
        private float _currentHealth;
        private bool resolvingDeath;
        private readonly Dictionary<object, int> counters = new();
        private readonly Dictionary<object, (EnemyBase target, int count)> streaks = new();
        private readonly Dictionary<object, (int threshold, int count)> skillCounters = new();
        public event Action<float, float> HealthChanged;
        public float AttackDamage => Mathf.Ceil(_attackDamage * BuffManager.Instance.Multiplier(this, BuffStat.AttackDamage));
        public float AttackSpeed => Mathf.Max(0.01f, _attackSpeed * BuffManager.Instance.Multiplier(this, BuffStat.AttackSpeed));
        public float MaxHealth => Mathf.Max(1, Mathf.Ceil(_maxHealth * BuffManager.Instance.Multiplier(this, BuffStat.MaxHealth)));
        public float CurrentHealth => _currentHealth;
        public float DamageMultiplier => BuffManager.Instance.Multiplier(this, BuffStat.Damage);
        public float ElementDamageMultiplier => BuffManager.Instance.Multiplier(this, BuffStat.ElementDamage);
        public float CooldownMultiplier => Mathf.Clamp01(1 - BuffManager.Instance.Percent(this, BuffStat.CooldownReduction) / 100f);

        private void Awake()
        {
            Instance = this;
            if (!Positive(_attackDamage)) _attackDamage = 10;
            if (!Positive(_maxHealth)) _maxHealth = 100;
            if (!Positive(_attackSpeed)) _attackSpeed = 5;
            _currentHealth = MaxHealth;
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>적 공격 또는 체력 지불 효과에서 호출한다. 사망 직전 이벤트를 먼저 처리하므로 부활이 GameOver보다 우선한다.</summary>
        public float TakeDamage(DamageInfo info)
        {
            if (!Positive(info.Damage) || _currentHealth <= 0 || resolvingDeath) return 0;
            float applied = Mathf.Min(_currentHealth, info.Damage);
            _currentHealth -= applied;
            if (_currentHealth <= 0)
            {
                resolvingDeath = true;
                try { EventManager.Instance.Publish(new ItemEvent(ItemTrigger.BeforeDeath, this)); }
                finally { resolvingDeath = false; }
            }
            RefreshHealth();
            if (_currentHealth <= 0 && GameManager.Instance != null) GameManager.Instance.GameOver();
            return applied;
        }

        /// <summary>일반 회복 효과에서 호출한다. 최대 체력을 넘거나 사망한 플레이어를 부활시키지 않는다.</summary>
        public float Heal(float amount)
        {
            if (!Positive(amount) || _currentHealth <= 0) return 0;
            float before = _currentHealth;
            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
            RefreshHealth();
            return _currentHealth - before;
        }

        /// <summary>BeforeDeath 효과에서만 호출한다. 이번 치명 피해로 인한 사망을 취소하고 최대 체력의 percent만큼 복구한다.</summary>
        public bool Revive(float percent)
        {
            if (!resolvingDeath || _currentHealth > 0 || !Positive(percent)) return false;
            _currentHealth = MaxHealth * Mathf.Clamp01(percent / 100f);
            return true;
        }

        /// <summary>체력 또는 최대 체력 보정이 바뀐 뒤 UI와 체력 조건 패시브에 통지한다.</summary>
        public void RefreshHealth()
        {
            _currentHealth = Mathf.Clamp(_currentHealth, 0, MaxHealth);
            HealthChanged?.Invoke(_currentHealth, MaxHealth);
            EventManager.Instance.Publish(new ItemEvent(ItemTrigger.HealthChanged, this));
        }

        /// <summary>기본 공격력 값을 직접 설정하는 기존 API. 아이템 보정은 BuffManager로 추가한다.</summary>
        public bool SetAttackDamage(float value) { if (!Positive(value)) return false; _attackDamage = value; return true; }

        /// <summary>플레이어 직접 공격 피해를 계산한다. ElementManager는 이 API를 쓰지 않아 Damage%와 속성 Damage%가 분리된다.</summary>
        public float DealDamage(EnemyBase enemy, float attackPercent, string source)
        {
            if (enemy == null || enemy.Status == null || enemy.Status.HP <= 0 || !Positive(attackPercent)) return 0;
            return enemy.TakeDamage(new DamageInfo(AttackDamage * attackPercent / 100f * DamageMultiplier,
                source, DamageSourceKind.Player, this));
        }

        /// <summary>적 처치 보상을 계산한다. 상점 환불에는 사용하지 않는다. 0.5는 올림하는 반올림을 사용한다.</summary>
        public long KillReward(int baseReward) => (long)Math.Round(Math.Max(0, baseReward) *
            (double)BuffManager.Instance.Multiplier(this, BuffStat.CurrencyGain), MidpointRounding.AwayFromZero);

        /// <summary>사용 제한/처치 횟수 패시브에서 호출한다. 아이템 개체별로 독립된 카운트를 증가시킨다.</summary>
        public int Increment(object source) { counters.TryGetValue(source, out int count); return counters[source] = count + 1; }
        /// <summary>주기형 카운터가 효과를 발동했을 때 0으로 돌린다.</summary>
        public void ResetCounter(object source) => counters[source] = 0;
        /// <summary>UI/검사 코드에서 현재 사용 횟수를 읽는다.</summary>
        public int GetCount(object source) => counters.TryGetValue(source, out int count) ? count : 0;

        /// <summary>슈팅 연속 적중 패시브에서 호출한다. null이면 초기화하고 대상 변경 시 1부터 시작한다.</summary>
        public bool ConsecutiveHit(object source, EnemyBase target, int threshold)
        {
            streaks.TryGetValue(source, out var old);
            int count = target == null ? 0 : target == old.target ? old.count + 1 : 1;
            bool ready = count >= Mathf.Max(1, threshold);
            streaks[source] = (target, ready ? 0 : count);
            return ready;
        }

        /// <summary>과부화 같은 모든 액티브 사용 카운터를 장착 시 등록한다.</summary>
        public void RegisterSkillCounter(object source, int threshold)
            => skillCounters[source] = (Mathf.Max(1, threshold), 0);
        /// <summary>해제된 아이템의 스킬 사용 누적을 중지한다.</summary>
        public void RemoveSkillCounter(object source) => skillCounters.Remove(source);

        /// <summary>SkillManager가 유효한 시전 직전에 호출한다. 자기 버프는 임계치에서 대기하고 다음 적 대상 시전에 보너스를 예약한다.</summary>
        public SkillCast BeginSkill(RuntimeItem item)
        {
            var cast = new SkillCast();
            foreach (var source in new List<object>(skillCounters.Keys))
            {
                var counter = skillCounters[source];
                int count = Math.Min(counter.threshold, counter.count + 1);
                if (count == counter.threshold && item.Meta.Target == ItemTarget.Enemies)
                { cast.Bonuses[source] = new HashSet<EnemyBase>(); count = 0; }
                skillCounters[source] = (counter.threshold, count);
            }
            return cast;
        }

        /// <summary>실제 스킬 적중 시 호출한다. 시전 순서로 예약된 보너스를 적마다 한 번만 허용한다.</summary>
        public bool ClaimSkillBonus(SkillCast cast, object source, EnemyBase enemy)
            => cast != null && cast.Bonuses.TryGetValue(source, out var hit) && hit.Add(enemy);

        /// <summary>인벤토리에서 아이템 개체를 완전히 제거할 때 카운터 참조를 정리한다.</summary>
        public void ForgetItem(object source) { counters.Remove(source); streaks.Remove(source); skillCounters.Remove(source); }
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = null;
    }
}
