using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>광전사의 분노: 80/60/40/20% 이하의 체력 단계마다 공격력과 공격속도를 강화한다. 회복 시 단계도 내려간다.</summary>
    public sealed class BerserkerRageEffect : ItemEffect
    {
        private readonly float stepPercent;
        /// <summary>Catalog에서 한 체력 단계당 증가율을 지정한다.</summary>
        public BerserkerRageEffect(float stepPercent) => this.stepPercent = stepPercent;
        /// <summary>장착 즉시 현재 체력 단계도 반영한다.</summary>
        public override void OnEquip(ItemUseContext c) => Execute(c);
        /// <summary>HealthChanged에서 호출한다. 체력 비율의 소수점은 올림하여 임계값을 판정한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            int healthPercent = Mathf.CeilToInt(c.Player.CurrentHealth / c.Player.MaxHealth * 100);
            int steps = healthPercent <= 20 ? 4 : healthPercent <= 40 ? 3 : healthPercent <= 60 ? 2 : healthPercent <= 80 ? 1 : 0;
            BuffManager.Instance.SetCondition(c.Player, c.Item, BuffStat.AttackDamage, steps * stepPercent);
            BuffManager.Instance.SetCondition(c.Player, c.Item, BuffStat.AttackSpeed, steps * stepPercent);
            /* VFX: 현재 분노 단계에 맞는 플레이어 표현을 갱신한다. */
        }
    }
}
