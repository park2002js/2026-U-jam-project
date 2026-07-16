using UJam.Runtime.Combat;

namespace UJam.Runtime.Elements
{
    public interface IElementEffectTarget
    {
        // Element 효과가 만든 피해를 Combat 경계로 전달
        DamageResult ApplyEffectDamage(DamageInfo damageInfo);
    }
}
