using UJam.Runtime.Combat;

namespace UJam.Runtime.Elements
{
    public interface IElementEffectDefinition
    {
        // Provider가 관리하는 안정적인 효과 식별자
        string EffectId { get; }

        // Payload를 이 Provider가 처리할 수 있는지 확인
        bool CanHandle(ElementPayload payload);

        // Payload와 최초 피해 정보를 바탕으로 활성 효과 생성
        IActiveElementEffect CreateActiveEffect(ElementPayload payload, DamageInfo damageInfo);
    }
}
