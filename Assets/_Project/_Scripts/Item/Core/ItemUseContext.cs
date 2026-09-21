using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>중앙 이벤트의 인자. Shooting 미적중 Enemies는 null이고 AppliedDamage는 부가 효과까지 누적한 실제 피해다.</summary>
    public sealed class ItemEvent
    {
        public ItemTrigger Trigger { get; }
        public PlayerStatus Player { get; }
        public Vector3 Position { get; }
        public IReadOnlyList<EnemyBase> Enemies { get; }
        public Item SkillItem { get; }
        public SkillCast Cast { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
        public float AppliedDamage { get; set; }
        public bool Executed { get; set; }
        public bool DeathPrevented { get; set; }

        /// <summary>발신 시스템에서 호출한다. 체력 이벤트에는 발생 당시의 현재/최대 체력을 함께 보존한다.</summary>
        public ItemEvent(ItemTrigger trigger, PlayerStatus player, Vector3 position = default,
            IReadOnlyList<EnemyBase> enemies = null, Item skillItem = null, SkillCast cast = null)
        {
            Trigger = trigger; Player = player; Position = position; Enemies = enemies; SkillItem = skillItem; Cast = cast;
            if (player != null) { CurrentHealth = player.CurrentHealth; MaxHealth = player.MaxHealth; }
        }
    }

    /// <summary>Effect가 사용하는 실행 인자. 공유 이벤트와 실행 중인 보유 Item을 분리해 지연 효과의 소유자를 보존한다.</summary>
    public sealed class ItemUseContext
    {
        public Item Item { get; }
        public ItemEvent Signal { get; }
        public PlayerStatus Player => Signal.Player;
        public Vector3 Position => Signal.Position;
        public IReadOnlyList<EnemyBase> Enemies => Signal.Enemies;
        public LayerMask EnemyMask => Item.Runtime.EnemyMask;

        /// <summary>Runtime/검사 코드에서 실행 중인 보유 Item과 중앙 이벤트를 묶는다.</summary>
        public ItemUseContext(Item item, ItemEvent signal) { Item = item; Signal = signal; }

        /// <summary>Effect에서 시간 작업을 시작할 때 사용한다. 장착 해제와 함께 중지된다.</summary>
        public void Run(IEnumerator routine) => Item.Runtime.Run(routine);

        /// <summary>스킬/슈팅 계수 피해를 준다. 공격력과 일반 Damage%를 적용하고 실제 피해량을 반환한다.</summary>
        public float Damage(EnemyBase enemy, float attackPercent)
        {
            if (Player == null) return 0;
            float damage = Player.DealDamage(enemy, attackPercent, Item.ID);
            if (Signal.Trigger == ItemTrigger.Shooting) Signal.AppliedDamage += damage;
            return damage;
        }

        /// <summary>속성을 부여한다. 출혈이 즉시 터진 경우 그 피해도 Shooting 최종 피해에 포함한다.</summary>
        public void Element(EnemyBase enemy, UJam.Runtime.Systems.ElementType element)
        {
            float damage = ElementManager.Instance.Apply(enemy, element, Player);
            if (Signal.Trigger == ItemTrigger.Shooting) Signal.AppliedDamage += damage;
        }

        /// <summary>액티브의 실제 적중 시 호출한다. 과부화 같은 스킬 강화는 이 목록을 받고 VFX/부가 피해는 재통지하지 않는다.</summary>
        public void ReportHits(IReadOnlyList<EnemyBase> enemies)
        {
            if (Signal.Cast == null || Player == null || enemies == null || enemies.Count == 0) return;
            EventManager.Instance.Publish(new ItemEvent(ItemTrigger.SkillHit, Player, Position, enemies, Item, Signal.Cast));
        }
    }

    /// <summary>시전 한 번의 식별 정보. 지연된 스킬도 시전 순서대로 정해진 과부화 보너스와 중복 방지 목록을 보존한다.</summary>
    public sealed class SkillCast
    {
        internal readonly Dictionary<object, HashSet<EnemyBase>> Bonuses = new();
    }
}
