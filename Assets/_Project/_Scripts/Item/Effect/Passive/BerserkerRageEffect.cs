using UJam.Runtime.Systems;

namespace Ujam.Runtime.Item
{
    /// <summary>광전사의 분노: 80/60/40/20% 이하의 체력 단계마다 공격력과 공격속도를 강화한다. 회복 시 단계도 내려간다.</summary>
    public class BerserkerRageEffect : ItemEffect
    {
        private readonly float stepPercent;
        /// <summary>Catalog에서 한 체력 단계당 증가율을 지정한다.</summary>
        public BerserkerRageEffect(float stepPercent) => this.stepPercent = stepPercent;
        /// <summary>장착 즉시 현재 체력 단계도 반영한다.</summary>
        public override void OnEquip(ItemUseContext c) => Execute(c);
        public override void OnUnequip(ItemUseContext c) => ItemScreenVfx.Stop(c.Item);
        /// <summary>HealthChanged에서 호출한다. 비율을 직접 비교해 60% 등의 경계에서 반올림 오차를 피한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            float healthRatio = c.Player.CurrentHealth / c.Player.MaxHealth;
            int steps = healthRatio <= .2f ? 4 : healthRatio <= .4f ? 3 : healthRatio <= .6f ? 2 : healthRatio <= .8f ? 1 : 0;

            BuffManager.Instance.SetCondition(c.Player, c.Item, BuffStat.AttackDamage, steps * stepPercent);
            BuffManager.Instance.SetCondition(c.Player, c.Item, BuffStat.AttackSpeed, steps * stepPercent);
            ItemScreenVfx.SetRageStep(c, steps);
        }
    }
}
