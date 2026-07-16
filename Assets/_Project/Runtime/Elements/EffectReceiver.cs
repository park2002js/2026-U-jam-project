using System.Collections.Generic;
using UJam.Runtime.Combat;
using UnityEngine;

namespace UJam.Runtime.Elements
{
    public sealed class EffectReceiver : MonoBehaviour, IElementPayloadReceiver
    {
        // Inspector 순서대로 연결할 Element 효과 Provider 목록
        [SerializeField] private MonoBehaviour[] _providers = new MonoBehaviour[0];

        // Inspector에서 연결할 Element 효과 대상
        [SerializeField] private MonoBehaviour _target;

        // 효과 식별자별 단일 활성 상태 저장소
        private readonly Dictionary<string, IActiveElementEffect> _activeEffects = new Dictionary<string, IActiveElementEffect>();

        // Combat port에서 전달받은 Payload를 Provider에 위임
        public void ReceiveElement(ElementPayload payload, DamageInfo damageInfo)
        {
            // 대상과 입력의 기본 유효성 확인
            IElementEffectTarget effectTarget = _target as IElementEffectTarget;
            if (effectTarget == null || payload.IsEmpty || !IsPositiveFinite(payload.Magnitude))
            {
                // 연결 또는 입력이 유효하지 않으면 무시
                return;
            }

            // CanHandle을 통과한 첫 Provider 선택
            IElementEffectDefinition definition = FindDefinition(payload);
            if (definition == null || string.IsNullOrEmpty(definition.EffectId))
            {
                // 처리 가능한 Provider가 없으면 종료
                return;
            }

            // 같은 효과는 두 번째 타이머 없이 Refresh
            if (_activeEffects.TryGetValue(definition.EffectId, out IActiveElementEffect activeEffect))
            {
                // RefreshDuration 정책으로 기존 상태 갱신
                activeEffect.Refresh(payload, damageInfo);

                // 새 효과를 만들지 않고 종료
                return;
            }

            // Provider가 새 활성 효과 생성
            IActiveElementEffect createdEffect = definition.CreateActiveEffect(payload, damageInfo);
            if (createdEffect == null || string.IsNullOrEmpty(createdEffect.EffectId))
            {
                // 유효한 효과가 아니면 무시
                return;
            }

            // 효과 식별자당 하나의 활성 상태 저장
            _activeEffects[createdEffect.EffectId] = createdEffect;
        }

        // 프레임 경과에 따라 활성 효과 실행
        private void Update()
        {
            // 현재 대상만 인터페이스로 변환
            IElementEffectTarget effectTarget = _target as IElementEffectTarget;
            if (_activeEffects.Count == 0)
            {
                // 활성 효과가 없으면 갱신하지 않음
                return;
            }

            // Dictionary 순회 중 삭제하지 않도록 만료 키 수집
            List<string> expiredIds = new List<string>();
            foreach (KeyValuePair<string, IActiveElementEffect> entry in _activeEffects)
            {
                // 활성 효과에 프레임 시간을 전달
                entry.Value.Tick(Time.deltaTime, effectTarget);

                // 만료 효과를 종료 목록에 기록
                if (entry.Value.IsExpired)
                {
                    // 종료 후 제거할 식별자 저장
                    expiredIds.Add(entry.Key);
                }
            }

            // 만료 효과를 종료하고 저장소에서 제거
            for (int index = 0; index < expiredIds.Count; index++)
            {
                // 현재 만료 효과 조회
                string effectId = expiredIds[index];
                IActiveElementEffect expiredEffect = _activeEffects[effectId];

                // 종료를 한 번 호출
                expiredEffect.End(effectTarget);

                // 종료된 효과 제거
                _activeEffects.Remove(effectId);
            }
        }

        // Inspector Provider 중 인터페이스와 CanHandle을 만족하는 항목 탐색
        private IElementEffectDefinition FindDefinition(ElementPayload payload)
        {
            // Provider 배열이 없으면 찾을 수 없음
            if (_providers == null)
            {
                // Provider 부재 반환
                return null;
            }

            // Inspector의 명시적 순서를 유지하며 탐색
            for (int index = 0; index < _providers.Length; index++)
            {
                // 현재 Component를 Provider 인터페이스로 변환
                IElementEffectDefinition definition = _providers[index] as IElementEffectDefinition;
                if (definition == null)
                {
                    // 인터페이스 없는 Component 건너뜀
                    continue;
                }

                // Payload를 처리할 수 있는 첫 Provider 선택
                if (definition.CanHandle(payload))
                {
                    // 일치 Provider 반환
                    return definition;
                }
            }

            // 일치 Provider 없음
            return null;
        }

        // 양수이고 NaN이나 Infinity가 아닌 입력인지 확인
        private static bool IsPositiveFinite(float value)
        {
            // 유효한 Element magnitude 확인
            bool isValid = value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);

            // 입력 유효성 반환
            return isValid;
        }

        // Component 비활성화 시 모든 활성 효과를 한 번씩 종료
        private void OnDisable()
        {
            // 대상이 없어도 종료 상태 정리는 수행
            IElementEffectTarget effectTarget = _target as IElementEffectTarget;
            foreach (IActiveElementEffect activeEffect in _activeEffects.Values)
            {
                // 비활성화 시 활성 효과 종료
                activeEffect.End(effectTarget);
            }

            // 종료된 효과 참조 제거
            _activeEffects.Clear();
        }
    }
}
