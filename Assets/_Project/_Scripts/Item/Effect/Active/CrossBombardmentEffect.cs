using System;

namespace Ujam.Runtime.Item
{
    /// <summary>가로·세로 폭격을 동시에 재생한다. 교차점의 적은 각 Box에서 한 번씩 피해를 받는다.</summary>
    public class CrossBombardmentEffect : ItemEffect
    {
        private readonly float thickness, length, delay, damagePercent, fallDuration, sweepDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight;
        public CrossBombardmentEffect(float thickness, float delay, float damagePercent, float length = 10f, float fallDuration = .3f,
            float sweepDuration = .25f, float incomingSpeed = 1f, float impactSpeed = 1f, float impactLifetime = 1.5f, float spawnHeight = 10f)
        {
            HorizontalBombardmentEffect.Validate(thickness, delay, damagePercent, fallDuration, sweepDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight);
            if (!float.IsFinite(length) || length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            this.thickness = thickness; this.length = length; this.delay = delay; this.damagePercent = damagePercent;
            this.fallDuration = fallDuration; this.sweepDuration = sweepDuration; this.incomingSpeed = incomingSpeed;
            this.impactSpeed = impactSpeed; this.impactLifetime = impactLifetime; this.spawnHeight = spawnHeight;
        }
        public override void Execute(ItemUseContext c) => c.Run(HorizontalBombardmentEffect.Bombard(c, thickness, length, true,
            delay, damagePercent, fallDuration, sweepDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight));
    }
}
