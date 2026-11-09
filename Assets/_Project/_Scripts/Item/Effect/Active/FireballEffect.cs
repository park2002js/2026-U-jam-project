using System.Collections;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>화염구: 낙하 연출 후 착탄 시점에 폭발·피해·화상을 함께 적용한다.</summary>
    public class FireballEffect : ItemEffect
    {
        private readonly float radius, damagePercent, delay, fallDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight, incomingDelay;

        /// <summary>delay는 시전 후 착탄 시각, fallDuration은 이동 시간, Speed는 각 파티클의 재생 배율이다.</summary>
        public FireballEffect(float radius, float damagePercent, float delay, float fallDuration = 1f,
            float incomingSpeed = 1f, float impactSpeed = 1f, float impactLifetime = 1.5f,
            float spawnHeight = 10f, float incomingDelay = 0f)
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
            foreach (var enemy in enemies) { c.Damage(enemy, damagePercent); c.Element(enemy, ElementType.Burn); }
            c.ReportHits(enemies);
        }
    }
}
