using UJam.Runtime.Combat;
using UnityEngine;

namespace UJam.Runtime.Elements
{
    public sealed class CombatElementEffectTarget : MonoBehaviour, IElementEffectTarget
    {
        // Inspector에서 연결할 Combat 피해 수신기
        [SerializeField] private DamageReceiver _damageReceiver;

        // Effect 피해를 명시적으로 Combat DamageReceiver로 전달
        public DamageResult ApplyEffectDamage(DamageInfo damageInfo)
        {
            // DamageReceiver가 없으면 들어온 HitZone을 보존한 차단 결과 반환
            if (_damageReceiver == null)
            {
                // 연결되지 않은 Combat 수신기는 안전하게 피해를 차단
                return new DamageResult(0f, true, false, damageInfo.HitContext.Zone);
            }

            // 내부 구현에 직접 접근하지 않고 Combat 수신기에 피해 위임
            return _damageReceiver.TakeDamage(damageInfo);
        }
    }
}
