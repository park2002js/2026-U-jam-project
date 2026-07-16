using UJam.Runtime.Combat;

namespace UJam.Runtime.Elements
{
    public interface IActiveElementEffect
    {
        // 활성 효과를 식별하는 안정적인 키
        string EffectId { get; }

        // 효과가 종료되어 더 이상 Tick할 수 없는지 여부
        bool IsExpired { get; }

        // 새 Payload로 지속시간과 효과 입력을 갱신
        void Refresh(ElementPayload payload, DamageInfo damageInfo);

        // 경과 시간만큼 효과를 실행
        void Tick(float deltaTime, IElementEffectTarget target);

        // 효과 종료 시 대상에 정리 동작을 전달
        void End(IElementEffectTarget target);
    }
}
