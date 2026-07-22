using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UJam.Runtime.Player
{
    public sealed class PlayerInput : MonoBehaviour
    {
        // 발사에 사용할 Input Action 참조
        [SerializeField] private InputActionReference _attackActionReference;

        // 스킬 슬롯 순서대로 사용할 Input Action 참조
        [SerializeField] private InputActionReference[] _skillActionReferences = Array.Empty<InputActionReference>();

        // 설치 확정에 사용할 Input Action 참조
        [SerializeField] private InputActionReference _placementConfirmActionReference;

        // 설치 취소에 사용할 Input Action 참조
        [SerializeField] private InputActionReference _placementCancelActionReference;

        // 현재 전투 입력 허용 상태
        private bool _combatInputEnabled;

        // 현재 설치 입력 허용 상태
        private bool _placementInputEnabled;

        // 스킬 Action별 해제에 사용할 callback
        private Action<InputAction.CallbackContext>[] _skillCallbacks = Array.Empty<Action<InputAction.CallbackContext>>();

        // 좌클릭 발사 요청
        public event Action AttackRequested;

        // 입력 순서에 해당하는 스킬 요청
        public event Action<int> SkillRequested;

        // 현재 설치 위치 확정 요청
        public event Action PlacementConfirmRequested;

        // 현재 설치 취소 요청
        public event Action PlacementCancelRequested;

        // Component 활성화 시 모든 입력 callback 연결
        private void OnEnable()
        {
            // 발사 callback 연결
            InputAction attackAction = GetAction(_attackActionReference);
            if (attackAction != null)
            {
                attackAction.performed += OnAttackPerformed;
            }

            // 스킬 callback 배열 준비
            _skillCallbacks = new Action<InputAction.CallbackContext>[_skillActionReferences.Length];
            for (int index = 0; index < _skillActionReferences.Length; index += 1)
            {
                // 현재 슬롯 Action 확인
                InputAction skillAction = GetAction(_skillActionReferences[index]);
                if (skillAction == null)
                {
                    // 다음 스킬 슬롯 확인
                    continue;
                }

                // callback에서 사용할 슬롯 번호 보관
                int slot = index;
                // 해제 가능한 슬롯별 callback 생성
                Action<InputAction.CallbackContext> callback = context => OnSkillPerformed(context, slot);
                _skillCallbacks[index] = callback;
                skillAction.performed += callback;
            }

            // 설치 확정 callback 연결
            InputAction confirmAction = GetAction(_placementConfirmActionReference);
            if (confirmAction != null)
            {
                confirmAction.performed += OnPlacementConfirmPerformed;
            }

            // 설치 취소 callback 연결
            InputAction cancelAction = GetAction(_placementCancelActionReference);
            if (cancelAction != null)
            {
                cancelAction.performed += OnPlacementCancelPerformed;
            }

            // 현재 권한에 맞춰 Action 활성화
            ApplyInputAvailability();
        }

        // Component 비활성화 시 모든 입력 callback 해제
        private void OnDisable()
        {
            // 발사 callback과 Action 해제
            InputAction attackAction = GetAction(_attackActionReference);
            if (attackAction != null)
            {
                attackAction.performed -= OnAttackPerformed;
                attackAction.Disable();
            }

            // 연결된 스킬 callback과 Action 해제
            for (int index = 0; index < _skillActionReferences.Length; index += 1)
            {
                // 현재 슬롯 Action 확인
                InputAction skillAction = GetAction(_skillActionReferences[index]);
                // 현재 슬롯 callback 확인
                Action<InputAction.CallbackContext> callback = index < _skillCallbacks.Length
                    ? _skillCallbacks[index]
                    : null;
                if (skillAction == null)
                {
                    // 다음 스킬 슬롯 해제 확인
                    continue;
                }

                if (callback != null)
                {
                    skillAction.performed -= callback;
                }

                skillAction.Disable();
            }

            // 설치 확정 callback과 Action 해제
            InputAction confirmAction = GetAction(_placementConfirmActionReference);
            if (confirmAction != null)
            {
                confirmAction.performed -= OnPlacementConfirmPerformed;
                confirmAction.Disable();
            }

            // 설치 취소 callback과 Action 해제
            InputAction cancelAction = GetAction(_placementCancelActionReference);
            if (cancelAction != null)
            {
                cancelAction.performed -= OnPlacementCancelPerformed;
                cancelAction.Disable();
            }

            // 해제된 callback 배열 비우기
            _skillCallbacks = Array.Empty<Action<InputAction.CallbackContext>>();
        }

        // 전투 Phase의 공격과 스킬 입력 허용 상태 변경
        public void SetCombatInputEnabled(bool enabled)
        {
            // 최신 전투 입력 허용 상태 저장
            _combatInputEnabled = enabled;

            // 활성 Component에서 즉시 Action 상태 반영
            if (isActiveAndEnabled)
            {
                ApplyCombatInputAvailability();
            }
        }

        // 정비 Phase의 설치 입력 허용 상태 변경
        public void SetPlacementInputEnabled(bool enabled)
        {
            // 최신 설치 입력 허용 상태 저장
            _placementInputEnabled = enabled;

            // 활성 Component에서 즉시 Action 상태 반영
            if (isActiveAndEnabled)
            {
                ApplyPlacementInputAvailability();
            }
        }

        // 현재 권한에 맞춰 모든 Action 상태 반영
        private void ApplyInputAvailability()
        {
            // 전투 입력 상태 반영
            ApplyCombatInputAvailability();
            // 설치 입력 상태 반영
            ApplyPlacementInputAvailability();
        }

        // 공격과 스킬 Action 상태 반영
        private void ApplyCombatInputAvailability()
        {
            // 발사 Action 상태 변경
            SetActionEnabled(GetAction(_attackActionReference), _combatInputEnabled);

            // 모든 스킬 Action 상태 변경
            foreach (InputActionReference reference in _skillActionReferences)
            {
                SetActionEnabled(GetAction(reference), _combatInputEnabled);
            }
        }

        // 설치 확정과 취소 Action 상태 반영
        private void ApplyPlacementInputAvailability()
        {
            // 설치 확정 Action 상태 변경
            SetActionEnabled(GetAction(_placementConfirmActionReference), _placementInputEnabled);
            // 설치 취소 Action 상태 변경
            SetActionEnabled(GetAction(_placementCancelActionReference), _placementInputEnabled);
        }

        // 발사 입력을 의미 있는 요청으로 전달
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            // 연결된 Controller에 발사 요청 통지
            AttackRequested?.Invoke();
        }

        // 스킬 입력을 슬롯 요청으로 전달
        private void OnSkillPerformed(InputAction.CallbackContext context, int slot)
        {
            // 연결된 Controller에 스킬 요청 통지
            SkillRequested?.Invoke(slot);
        }

        // 설치 확정 입력을 요청으로 전달
        private void OnPlacementConfirmPerformed(InputAction.CallbackContext context)
        {
            // 연결된 Controller에 설치 확정 요청 통지
            PlacementConfirmRequested?.Invoke();
        }

        // 설치 취소 입력을 요청으로 전달
        private void OnPlacementCancelPerformed(InputAction.CallbackContext context)
        {
            // 연결된 Controller에 설치 취소 요청 통지
            PlacementCancelRequested?.Invoke();
        }

        // Input Action 참조에서 실제 Action 확인
        private static InputAction GetAction(InputActionReference reference)
        {
            // 누락된 참조는 빈 Action 반환
            return reference != null ? reference.action : null;
        }

        // Input Action의 활성화 또는 비활성화 적용
        private static void SetActionEnabled(InputAction action, bool enabled)
        {
            // 누락된 Action은 변경 없이 종료
            if (action == null)
            {
                // 변경할 Action 없이 종료
                return;
            }

            // 요청된 상태에 맞춰 Action 변경
            if (enabled)
            {
                action.Enable();
            }
            else
            {
                action.Disable();
            }
        }
    }
}
