using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>과부화: 모든 액티브 사용을 세고 임계 시전의 적중 대상에게 추가 피해 후 감전을 적용한다.</summary>
    public sealed class OverloadEffect : ItemEffect
    {
        private readonly int uses;
        private readonly float damagePercent;
        /// <summary>Catalog에서 발동 주기와 추가 피해 공격력 계수를 지정한다.</summary>
        public OverloadEffect(int uses, float damagePercent) { this.uses = uses; this.damagePercent = damagePercent; }
        /// <summary>장착 시 PlayerStatus의 전체 스킬 사용 카운터를 등록한다.</summary>
        public override void OnEquip(ItemUseContext c) => c.Player.RegisterSkillCounter(c.Item, uses);
        /// <summary>해제 시 해당 아이템의 카운터 집계를 중지한다.</summary>
        public override void OnUnequip(ItemUseContext c) => c.Player.RemoveSkillCounter(c.Item);
        /// <summary>SkillHit에서 호출한다. 동일 시전의 같은 적은 한 번만 보너스를 받는다.</summary>
        public override void Execute(ItemUseContext c)
        {
            if (c.Enemies == null) return;
            foreach (var enemy in c.Enemies)
            {
                if (!c.Player.ClaimSkillBonus(c.Signal.Cast, c.Item, enemy)) continue;
                c.Damage(enemy, damagePercent);
                c.Element(enemy, ItemElement.Shock);
                /* VFX: 과부화 폭발/감전 표현을 이곳에 추가한다. */
            }
        }
    }
}
