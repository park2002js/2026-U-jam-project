using System;
using UnityEngine;
using UJam.Runtime.Phase;

namespace UJam.Runtime.Player
{
    public sealed class PlayerStatus : MonoBehaviour
    {
        // Player 기본 공격력
        [SerializeField, Min(0.0001f)] private float _attackDamage = 10f;

        // GameManager에서 전달받을 Phase 시스템
        private PhaseSystem _phaseSystem;

        // Phase 변경으로 입력 권한이 달라졌음을 알리는 이벤트
        public event Action InputAvailabilityChanged;

        // 외부에서 읽는 현재 기본 공격력
        public float AttackDamage
        {
            get
            {
                // 현재 검증된 공격력 반환
                return _attackDamage;
            }
        }

        // 현재 Phase 시스템 연결 여부
        public bool HasPhaseSystem
        {
            get
            {
                // Phase 시스템 존재 여부 반환
                return _phaseSystem != null;
            }
        }

        // 외부에서 읽는 현재 게임 Phase
        public PhaseState CurrentPhase
        {
            get
            {
                // 연결된 Phase 또는 안전한 정비 Phase 반환
                return HasPhaseSystem
                    ? _phaseSystem.CurrentState
                    : PhaseState.Preparation;
            }
        }

        // 현재 좌클릭 공격 허용 여부
        public bool CanAttack
        {
            get
            {
                // 전투 Phase에서만 공격 허용 결과 반환
                return HasPhaseSystem && CurrentPhase == PhaseState.Combat;
            }
        }

        // 현재 스킬 입력 허용 여부
        public bool CanUseSkills
        {
            get
            {
                // 전투 Phase에서만 스킬 허용 결과 반환
                return HasPhaseSystem && CurrentPhase == PhaseState.Combat;
            }
        }

        // 현재 건물 설치 허용 여부
        public bool CanPlace
        {
            get
            {
                // 정비 Phase에서만 설치 허용 결과 반환
                return HasPhaseSystem && CurrentPhase == PhaseState.Preparation;
            }
        }

        // Inspector 공격력 검증
        private void Awake()
        {
            // 잘못된 공격력을 안전한 기본값으로 변경
            if (!IsPositiveFinite(_attackDamage))
            {
                _attackDamage = 1f;
            }
        }

        // Player가 사용할 Phase 시스템 연결
        public void ConfigurePhaseSystem(PhaseSystem phaseSystem)
        {
            // 기존 Phase callback 해제
            if (_phaseSystem != null)
            {
                _phaseSystem.PhaseChanged -= HandlePhaseChanged;
            }

            // 새 Phase 시스템 저장
            _phaseSystem = phaseSystem;

            // 새 Phase callback 연결
            if (_phaseSystem != null)
            {
                _phaseSystem.PhaseChanged += HandlePhaseChanged;
            }

            // 새 Phase의 입력 권한 즉시 통지
            InputAvailabilityChanged?.Invoke();
        }

        // 외부 강화에서 기본 공격력 변경
        public bool SetAttackDamage(float value)
        {
            // 양의 유한 공격력만 허용
            if (!IsPositiveFinite(value))
            {
                // 공격력 변경 실패 반환
                return false;
            }

            // 검증된 공격력 저장
            _attackDamage = value;

            // 공격력 변경 성공 반환
            return true;
        }

        // Component 제거 전 Phase callback 해제
        private void OnDestroy()
        {
            // 연결된 Phase 시스템이 있을 때만 해제
            if (_phaseSystem != null)
            {
                _phaseSystem.PhaseChanged -= HandlePhaseChanged;
            }
        }

        // Phase 변경을 Controller가 읽을 입력 권한 변경으로 전달
        private void HandlePhaseChanged(PhaseState phase)
        {
            // 현재 Phase 기반 권한 재확인 요청
            InputAvailabilityChanged?.Invoke();
        }

        // 양의 유한 float 여부 확인
        private static bool IsPositiveFinite(float value)
        {
            // NaN과 무한대와 0 이하를 제외한 결과 반환
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
