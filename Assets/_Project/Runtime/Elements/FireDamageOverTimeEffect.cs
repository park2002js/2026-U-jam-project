using UJam.Runtime.Combat;
using UnityEngine;

namespace UJam.Runtime.Elements
{
    public sealed class FireDamageOverTimeEffect : MonoBehaviour, IElementEffectDefinition
    {
        // Fire 효과의 기본 지속시간
        [SerializeField] private float _duration = 3f;

        // Fire 효과의 기본 Tick 간격
        [SerializeField] private float _tickInterval = 1f;

        // 이 Provider의 안정적인 효과 식별자
        public string EffectId
        {
            get
            {
                // Fire 효과 식별자 반환
                return "fire";
            }
        }

        // lowercase fire와 유효한 양의 피해량만 처리
        public bool CanHandle(ElementPayload payload)
        {
            // Fire 식별자와 유한한 양수 Tick 피해량 확인
            bool canHandle = payload.ElementId == EffectId && IsPositiveFinite(payload.Magnitude);

            // Provider 처리 가능 여부 반환
            return canHandle;
        }

        // 유효한 Fire Payload를 활성 DoT 상태로 변환
        public IActiveElementEffect CreateActiveEffect(ElementPayload payload, DamageInfo damageInfo)
        {
            // 잘못된 Payload는 효과를 만들지 않음
            if (!CanHandle(payload))
            {
                // 안전한 실패 반환
                return null;
            }

            // 잘못된 Inspector 값은 정책 기본값으로 보정
            float duration = IsPositiveFinite(_duration) ? _duration : 3f;
            float tickInterval = IsPositiveFinite(_tickInterval) ? _tickInterval : 1f;

            // 최초 피해 문맥과 Tick 피해량을 보존한 상태 생성
            return new ActiveFireEffect(EffectId, duration, tickInterval, payload.Magnitude, damageInfo);
        }

        // 양수이고 NaN이나 Infinity가 아닌지 확인
        private static bool IsPositiveFinite(float value)
        {
            // 유효한 수치 입력 확인
            bool isValid = value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);

            // 수치 유효성 반환
            return isValid;
        }

        private sealed class ActiveFireEffect : IActiveElementEffect
        {
            // 효과 식별자
            private readonly string _effectId;

            // Refresh 때 다시 시작할 지속시간
            private readonly float _duration;

            // Tick 사이의 시간 간격
            private readonly float _tickInterval;

            // 현재 남은 지속시간
            private float _remainingDuration;

            // 다음 Tick까지 누적한 시간
            private float _elapsedSinceTick;

            // 현재 Tick 피해량
            private float _tickDamage;

            // 다음 Tick이 보존할 원본 Combat 정보
            private DamageInfo _originalDamageInfo;

            // End 중복 호출 방지 상태
            private bool _ended;

            // Fire 활성 상태 초기화
            public ActiveFireEffect(string effectId, float duration, float tickInterval, float tickDamage, DamageInfo damageInfo)
            {
                // 생성자 입력 저장
                _effectId = effectId;
                _duration = duration;
                _tickInterval = tickInterval;
                _remainingDuration = duration;
                _elapsedSinceTick = 0f;
                _tickDamage = tickDamage;
                _originalDamageInfo = damageInfo;
                _ended = false;
            }

            // 활성 효과 식별자 반환
            public string EffectId
            {
                get
                {
                    // 저장된 식별자 반환
                    return _effectId;
                }
            }

            // 지속시간 만료 또는 종료 상태 반환
            public bool IsExpired
            {
                get
                {
                    // 종료 플래그와 남은 시간을 함께 확인
                    bool expired = _ended || _remainingDuration <= 0f;

                    // 만료 상태 반환
                    return expired;
                }
            }

            // 지속시간과 현재 Tick 피해량을 교체
            public void Refresh(ElementPayload payload, DamageInfo damageInfo)
            {
                // 종료된 효과는 다시 활성화하지 않음
                if (_ended)
                {
                    // 종료 상태 유지
                    return;
                }

                // RefreshDuration 정책으로 시간과 피해량 초기화
                _remainingDuration = _duration;
                _elapsedSinceTick = 0f;
                _tickDamage = payload.Magnitude;
                _originalDamageInfo = damageInfo;
            }

            // 경과 시간에 맞춰 비재귀 Fire Tick 전달
            public void Tick(float deltaTime, IElementEffectTarget target)
            {
                // 종료 상태나 잘못된 시간은 무시
                if (IsExpired || !IsPositiveFinite(deltaTime))
                {
                    // 유효하지 않은 Tick 입력 무시
                    return;
                }

                // 활성 상태였던 시간만 계산
                float activeDelta = Mathf.Min(deltaTime, _remainingDuration);

                // 지속시간과 Tick 누적값 갱신
                _remainingDuration -= activeDelta;
                _elapsedSinceTick += activeDelta;

                // 누적 간격만큼 Tick 실행
                while (_elapsedSinceTick >= _tickInterval)
                {
                    // 다음 간격 제거
                    _elapsedSinceTick -= _tickInterval;

                    // Element가 null인 비재귀 Tick DamageInfo 생성
                    DamageInfo tickInfo = new DamageInfo(
                        _originalDamageInfo.Source,
                        _tickDamage,
                        new DamageType("element.fire.dot"),
                        null,
                        _originalDamageInfo.HitContext,
                        _originalDamageInfo.Flags);

                    // 대상이 있을 때만 Combat 경계로 Tick 피해 전달
                    if (target != null)
                    {
                        // Combat은 Element Target 경계 너머에서만 호출
                        target.ApplyEffectDamage(tickInfo);
                    }
                }

                // 지속시간 만료 시 0으로 고정
                if (_remainingDuration <= 0f)
                {
                    // 부동소수점 오차 제거
                    _remainingDuration = 0f;
                }
            }

            // 활성 Fire 효과를 한 번만 종료
            public void End(IElementEffectTarget target)
            {
                // 이미 종료된 효과는 중복 정리하지 않음
                if (_ended)
                {
                    // 중복 종료 호출 무시
                    return;
                }

                // 이 slice에는 별도 정리 동작이 없으므로 종료 상태만 기록
                _ended = true;
            }
        }
    }
}
