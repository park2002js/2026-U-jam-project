using UnityEngine;
using UnityEngine.InputSystem;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Enemy
{
    public sealed class EnemyStateTester : MonoBehaviour
    {
        // 상태를 테스트할 EnemyBase 객체
        [SerializeField] private EnemyBase _enemy;

        // 현재 테스트에서 유지할 Enemy 상태
        private EnemyStateType _heldState = EnemyStateType.Idle;

        // 지정한 상태를 계속 유지할지 여부
        private bool _isHoldingState;

        // Trigger에 의한 상태 전이를 기다리고 있는지 여부
        private bool _isWaitingForTriggeredState;

        // Trigger 테스트에서 도착할 목표 상태
        private EnemyStateType _triggeredState = EnemyStateType.None;

        // 테스트 시작 시 Enemy와 최초 FSM 상태 확인
        private void Start()
        {
            // Inspector에 Enemy가 연결되지 않은 경우 테스트 중단
            if (_enemy == null)
            {
                Debug.LogError(
                    "[EnemyStateTester] Enemy가 연결되지 않았습니다.",
                    this);

                return;
            }

            // 현재 FSM 상태를 최초 테스트 상태로 저장
            _heldState = _enemy.FSM.state;
            _isHoldingState = true;

            Debug.Log(
                $"[EnemyStateTester] 시작 상태: {_heldState}",
                this);
        }

        // 키 입력을 확인하고 지정한 Enemy 상태 유지
        private void Update()
        {
            if (_enemy == null || _enemy.FSM == null)
            {
                return;
            }

            // 숫자 키를 통한 상태 전환 확인
            HandleStateInput();

            // Trigger에 의한 상태 전이 완료 여부 확인
            if (_isWaitingForTriggeredState)
            {
                if (_enemy.FSM.state == _triggeredState)
                {
                    // 실제 상태 전이가 완료되면 해당 상태를 유지
                    _heldState = _triggeredState;
                    _isHoldingState = true;
                    _isWaitingForTriggeredState = false;

                    Debug.Log(
                        $"[EnemyStateTester] Trigger 전이 완료: {_triggeredState} 상태 유지 시작",
                        this);
                }

                return;
            }

            // 상태 유지가 필요하지 않으면 종료
            if (!_isHoldingState)
            {
                return;
            }

            // Dead는 기존 EnemyBase의 사망 처리에 따라 GameObject가 삭제되므로 반복 전환하지 않음
            if (_heldState == EnemyStateType.Dead)
            {
                return;
            }

            // FSM 내부에서 자동 전이가 발생한 경우 테스트 상태로 복귀
            if (_enemy.FSM.state != _heldState)
            {
                ForceState(_heldState);
            }
        }

        // 숫자 키를 각각 Enemy 상태 테스트와 연결
        private void HandleStateInput()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[Key.Digit1].wasPressedThisFrame)
            {
                TriggerState(EnemyStateType.Idle);
            }
            else if (Keyboard.current[Key.Digit2].wasPressedThisFrame)
            {
                TriggerState(EnemyStateType.Move);
            }
            else if (Keyboard.current[Key.Digit3].wasPressedThisFrame)
            {
                TriggerState(EnemyStateType.Attack);
            }
            else if (Keyboard.current[Key.Digit4].wasPressedThisFrame)
            {
                TriggerState(EnemyStateType.Dead);
            }
            else if (Keyboard.current[Key.Digit5].wasPressedThisFrame)
            {
                TriggerAttackByMovement();
            }
        }

        // 전달받은 상태를 새로운 테스트 상태로 지정
        public void TriggerState(EnemyStateType state)
        {
            if (_enemy == null || _enemy.FSM == null)
            {
                Debug.LogError(
                    "[EnemyStateTester] EnemyFSM을 사용할 수 없습니다.",
                    this);

                return;
            }

            // Trigger 테스트가 진행 중이라면 종료
            _isWaitingForTriggeredState = false;
            _triggeredState = EnemyStateType.None;

            // 앞으로 유지할 상태 저장
            _heldState = state;
            _isHoldingState = true;

            // 이미 해당 상태라면 다시 전환하지 않음
            if (_enemy.FSM.state == state)
            {
                Debug.Log(
                    $"[EnemyStateTester] 이미 {state} 상태입니다.",
                    this);

                return;
            }

            // 상태별 테스트 위치를 준비한 뒤 FSM 상태 변경
            PreparePosition(state);
            ForceState(state);

            Debug.Log(
                $"[EnemyStateTester] {state} 상태 테스트 시작",
                this);
        }

        // Move 완료를 실제 Trigger로 사용하여 Attack 상태 전이 테스트
        private void TriggerAttackByMovement()
        {
            if (_enemy == null || _enemy.FSM == null)
            {
                return;
            }

            // Trigger 테스트는 Idle 상태에서 시작
            if (_enemy.FSM.state != EnemyStateType.Idle)
            {
                Debug.LogWarning(
                    "[EnemyStateTester] Trigger 테스트는 Idle 상태에서 시작해야 합니다.",
                    this);

                return;
            }

            // BaseCore 사거리 밖으로 이동시켜 실제 Move 조건 준비
            PreparePosition(EnemyStateType.Move);

            // 자동 상태 전이 중에는 강제 상태 유지 중단
            _isHoldingState = false;
            _isWaitingForTriggeredState = true;
            _triggeredState = EnemyStateType.Attack;

            // Move까지만 직접 시작하고 Attack 진입은 기존 이동 로직에 맡김
            _enemy.FSM.SetState(EnemyStateType.Move);

            Debug.Log(
                "[EnemyStateTester] Move 완료 Trigger에 의한 Attack 상태 전이 대기",
                this);
        }

        // EnemyFSM을 지정한 상태로 강제 전환
        private void ForceState(EnemyStateType state)
        {
            if (_enemy == null || _enemy.FSM == null)
            {
                return;
            }

            // 이미 목표 상태이면 추가 전환하지 않음
            if (_enemy.FSM.state == state)
            {
                return;
            }

            // 이전 상태에서 실행 중이던 Coroutine이 새 테스트에 남지 않도록 정리
            _enemy.StopAllCoroutines();

            // 자동 전이 이후 Move 상태를 다시 테스트할 경우 시작 위치 복구
            if (state == EnemyStateType.Move)
            {
                PreparePosition(state);
            }

            // 자동 전이 이후 Attack 상태를 다시 테스트할 경우 공격 위치 복구
            if (state == EnemyStateType.Attack)
            {
                PreparePosition(state);
            }

            // EnemyFSM의 기존 상태 전환 함수 사용
            bool changed = _enemy.FSM.SetState(state);

            if (!changed)
            {
                Debug.LogWarning(
                    $"[EnemyStateTester] {state} 상태 전환에 실패했습니다.",
                    this);
            }
        }

        // Move와 Attack 상태를 확인할 수 있도록 Enemy 위치 조정
        private void PreparePosition(EnemyStateType state)
        {
            GridSystem grid = GridSystem.Instance;

            // Grid가 준비되지 않았으면 위치 조정 중단
            if (!grid.IsInitialized)
            {
                Debug.LogWarning(
                    "[EnemyStateTester] GridSystem이 초기화되지 않았습니다.",
                    this);

                return;
            }

            // Grid 정보에서 BaseCore가 위치한 Z 좌표 계산
            float baseCoreZ =
                grid.Origin.z
                + grid.BaseCoreRow * grid.CellHeight;

            // 기존 X, Y 좌표를 유지하기 위해 현재 위치 복사
            Vector3 position = _enemy.transform.position;

            // Move 상태에서는 BaseCore로부터 충분히 떨어진 위치에서 시작
            if (state == EnemyStateType.Move)
            {
                position.z =
                    baseCoreZ
                    + grid.CellHeight * 10f;
            }

            // Attack 상태에서는 BaseCore의 사거리 안쪽으로 이동
            if (state == EnemyStateType.Attack)
            {
                position.z = baseCoreZ;
            }

            // 계산한 테스트 위치 적용
            _enemy.transform.position = position;
        }
    }
}