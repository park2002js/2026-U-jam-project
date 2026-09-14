using System;
using UJam.Runtime.Systems;

namespace Ujam.Runtime.Item
{
    // GUID 31: 남은 쿨타임의 감소 속도를 높인다. 새 쿨타임의 길이를 줄이는 효과가 아니다.
    public sealed class AcceleratorEffect : ItemEffect
    {
        public float Percent { get; }
        public float Duration { get; }
        public AcceleratorEffect(float percent, float duration)
        {
            if (!float.IsFinite(percent) || percent < 0 || !float.IsFinite(duration) || duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(percent));
            Percent = percent; Duration = duration;
        }
        public override void Execute(ItemUseContext context) => TemporaryBuffs.Instance.Apply(
            context.Player, this, BuffStat.CooldownRate, Percent, Duration);
        public override void OnUnequip(ItemRuntime runtime) => TemporaryBuffs.Instance.RemoveSource(this);
    }
}
