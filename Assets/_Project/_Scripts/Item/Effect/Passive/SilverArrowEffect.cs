using UJam.Runtime.Enemy;

namespace Ujam.Runtime.Item
{
    /// <summary>은화살: 같은 적 연속 적중 카운트가 임계치에 도달하면 추가 직접 피해를 준다. 미적중은 카운트를 초기화한다.</summary>
    public class SilverArrowEffect : ItemEffect
    {
        private readonly int hits;
        private readonly float damagePercent, vfxDiameter, vfxDuration;
        /// <summary>Catalog에서 연속 횟수와 추가 피해 공격력 계수를 지정한다.</summary>
        public SilverArrowEffect(int hits, float damagePercent, float vfxDiameter = 3.5f, float vfxDuration = 1.5f)
        { this.hits = hits; this.damagePercent = damagePercent; this.vfxDiameter = vfxDiameter; this.vfxDuration = vfxDuration; }
        /// <summary>매 Shooting 이벤트에서 호출한다. 카운트는 PlayerStatus가 아이템별로 관리한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            EnemyBase target = c.Enemies != null && c.Enemies.Count > 0 ? c.Enemies[0] : null;
            if (!c.Player.ConsecutiveHit(c.Item, target, hits)) return;

            var point = ItemVfx.HitPosition(c, target);
            if (c.Damage(target, damagePercent) > 0)
                c.Run(ItemVfx.Spawn(c, vfxDiameter, vfxDuration, position: point));
        }
    }
}
