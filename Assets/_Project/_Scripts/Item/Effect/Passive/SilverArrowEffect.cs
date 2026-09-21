using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>은화살: 같은 적 연속 적중 카운트가 임계치에 도달하면 추가 직접 피해를 준다. 미적중은 카운트를 초기화한다.</summary>
    public sealed class SilverArrowEffect : ItemEffect
    {
        private readonly int hits;
        private readonly float damagePercent;
        /// <summary>Catalog에서 연속 횟수와 추가 피해 공격력 계수를 지정한다.</summary>
        public SilverArrowEffect(int hits, float damagePercent) { this.hits = hits; this.damagePercent = damagePercent; }
        /// <summary>매 Shooting 이벤트에서 호출한다. 카운트는 PlayerStatus가 아이템별로 관리한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            EnemyBase target = c.Enemies != null && c.Enemies.Count > 0 ? c.Enemies[0] : null;
            if (!c.Player.ConsecutiveHit(c.Item, target, hits)) return;
            c.Damage(target, damagePercent);
            /* VFX: 은화살 추가 적중 표현을 이곳에 추가한다. */
        }
    }
}
