using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UJam.Runtime.Player
{
    public sealed class PlayerInputAdapter : MonoBehaviour
    {
        // 미래 Player Prefab에서 연결할 Attack Input Action 참조
        [SerializeField] private InputActionReference _attackActionReference;

        // 런타임에 주입된 Attack Input Action
        private InputAction _configuredAttackAction;

        // Attack performed를 Player 요청으로 전달
        public event Action AttackRequested;

        // 현재 Adapter가 사용 중인 Attack Action을 주입
        public void ConfigureAttackAction(InputAction attackAction)
        {
            // 활성 상태에서 기존 callback을 먼저 해제
            UnsubscribeFromAttackAction();

            // 새 Action을 저장
            _configuredAttackAction = attackAction;

            // 활성화된 Adapter만 새 callback을 연결
            if (isActiveAndEnabled)
            {
                SubscribeToAttackAction();
            }
        }

        // Component 활성화 시 Attack callback을 연결
        private void OnEnable()
        {
            // 주입 Action이 없으면 Inspector 참조를 사용
            if (_configuredAttackAction == null && _attackActionReference != null)
            {
                _configuredAttackAction = _attackActionReference.action;
            }

            // 유효한 Action에만 callback을 연결
            SubscribeToAttackAction();
        }

        // Component 비활성화 시 Attack callback을 해제
        private void OnDisable()
        {
            // 중복 요청을 막기 위해 현재 callback을 해제
            UnsubscribeFromAttackAction();
        }

        // 현재 Action의 performed callback을 한 번 연결
        private void SubscribeToAttackAction()
        {
            // Input Action이 없으면 아무 작업도 하지 않음
            if (_configuredAttackAction == null)
            {
                return;
            }

            // 동일 callback의 중복 연결을 막기 위해 먼저 해제 후 연결
            _configuredAttackAction.performed -= OnAttackPerformed;
            _configuredAttackAction.performed += OnAttackPerformed;
        }

        // 현재 Action의 performed callback을 해제
        private void UnsubscribeFromAttackAction()
        {
            // Input Action이 없으면 해제할 callback도 없음
            if (_configuredAttackAction == null)
            {
                return;
            }

            // 현재 Adapter callback만 해제
            _configuredAttackAction.performed -= OnAttackPerformed;
        }

        // performed 입력을 Attack 요청 이벤트로 전달
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            // 구독자에게만 공격 요청을 전달
            AttackRequested?.Invoke();
        }
    }
}
