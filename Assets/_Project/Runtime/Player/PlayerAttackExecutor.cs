using UnityEngine;
using UJam.Runtime.Combat;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Player
{
    public sealed class PlayerAttackExecutor : MonoBehaviour, IAttackSource
    {
        // Inspector에서 설정할 원시 공격력
        [SerializeField, Min(0f)] private float _rawDamage = 1f;

        // Inspector에서 설정할 공격 진영 식별자
        [SerializeField] private string _factionId;

        // Inspector에서 설정할 공격 식별자
        [SerializeField] private string _attackId;

        // Inspector에서 설정할 피해 종류 식별자
        [SerializeField] private string _damageTypeId;

        // 선택적으로 전달할 Element 식별자
        [SerializeField] private string _elementId;

        // Element가 있을 때 전달할 양
        [SerializeField] private float _elementMagnitude;

        // Combat 계약에 전달할 공격 진영
        public Faction Faction
        {
            get
            {
                // Inspector 문자열로 Faction 생성
                return new Faction(_factionId);
            }
        }

        // Combat 계약에 전달할 공격 식별자
        public AttackId AttackId
        {
            get
            {
                // Inspector 문자열로 AttackId 생성
                return new AttackId(_attackId);
            }
        }

        // Provider를 통해 대상을 찾고 한 번만 피해를 전달
        public bool TryExecuteAttack(
            IPlayerAttackTargetProvider targetProvider,
            IGridAreaQuery gridAreaQuery)
        {
            // 필수 공격 의존성과 수치를 먼저 검증
            if (targetProvider == null || gridAreaQuery == null || !IsPositiveFinite(_rawDamage))
            {
                // 공격 실행 없이 실패를 반환
                return false;
            }

            // Combat 식별자가 비어 있으면 실행하지 않음
            if (Faction.IsEmpty || AttackId.IsEmpty || string.IsNullOrEmpty(_damageTypeId))
            {
                // 공격 실행 없이 실패를 반환
                return false;
            }

            // 대상 조회 전에 Element 정책을 검증하고 Payload를 준비
            ElementPayload? element = null;
            if (!string.IsNullOrEmpty(_elementId))
            {
                // Element 양은 양수 유한 값이어야 함
                if (!IsPositiveFinite(_elementMagnitude))
                {
                    // 잘못된 Element는 피해를 전달하지 않음
                    return false;
                }

                // 유효한 Element Payload를 생성
                element = new ElementPayload(_elementId, _elementMagnitude);
            }

            // Provider가 허용한 대상만 사용
            HitZoneReceiver target;
            if (!targetProvider.TryGetAttackTarget(gridAreaQuery, out target) || target == null)
            {
                // 대상이 없으면 피해를 전달하지 않음
                return false;
            }

            // Combat 계약에 맞는 DamageInfo를 생성
            DamageInfo damageInfo = new DamageInfo(
                this,
                _rawDamage,
                new DamageType(_damageTypeId),
                element,
                new HitContext(target.Zone),
                DamageFlags.None);

            // 검증된 대상에 정확히 한 번 피해를 전달
            target.TakeDamage(damageInfo);

            // 대상 호출까지 완료된 공격을 성공으로 반환
            return true;
        }

        // 수치가 양수이고 유한한지 검증
        private static bool IsPositiveFinite(float value)
        {
            // NaN, Infinity, 0 이하를 차단
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                // 유효하지 않은 수치 결과를 반환
                return false;
            }

            // 유효한 수치 결과를 반환
            return true;
        }
    }
}
