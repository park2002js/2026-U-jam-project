namespace UJam.Runtime.Combat
{
    public interface IElementPayloadReceiver
    {
        // 실제 피해가 적용된 뒤 속성 Payload를 전달
        void ReceiveElement(ElementPayload payload, DamageInfo damageInfo);
    }
}
