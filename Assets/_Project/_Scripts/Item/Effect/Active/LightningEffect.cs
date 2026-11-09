using System.Collections;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>낙뢰: 빠른 낙하 전격에 이어 지면 폭발·피해·감전을 같은 시점에 적용한다.</summary>
    public class LightningEffect : ItemEffect
    {
        private readonly float radius, damagePercent, delay, fallDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight, incomingDelay;

        /// <summary>delay는 착탄 시각, incomingDelay는 번개 시작 시각, fallDuration과 Speed는 이동·파티클 속도다.</summary>
        public LightningEffect(float radius, float damagePercent, float delay, float fallDuration = .15f,
            float incomingSpeed = 1f, float impactSpeed = 1f, float impactLifetime = 1.5f,
            float spawnHeight = 10f, float incomingDelay = .85f)
        {
            this.radius = radius; this.damagePercent = damagePercent; this.delay = delay;
            this.fallDuration = fallDuration; this.incomingSpeed = incomingSpeed; this.impactSpeed = impactSpeed;
            this.impactLifetime = impactLifetime; this.spawnHeight = spawnHeight; this.incomingDelay = incomingDelay;
        }

        public override void Execute(ItemUseContext c) => c.Run(Strike(c));
        private IEnumerator Strike(ItemUseContext c)
        {
            ItemCastVfx visual = null;
            yield return ItemCastVfx.Create(c, value => visual = value);
            if (!c.Item.IsEquipped) yield break;
            if (visual != null) visual.BeginIncoming(spawnHeight, fallDuration, incomingSpeed, incomingDelay);
            yield return new WaitForSeconds(delay);
            if (!c.Item.IsEquipped) yield break;

            if (visual != null) visual.Impact(radius * ItemWorld.CellWidth * 2, impactSpeed, impactLifetime);
            var enemies = ItemWorld.Circle(c.Position, radius, c.EnemyMask);
            foreach (var enemy in enemies) { c.Damage(enemy, damagePercent); c.Element(enemy, ElementType.Shock); }
            c.ReportHits(enemies);
        }
    }
}
