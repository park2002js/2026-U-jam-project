using UnityEngine;
using UJam.Runtime.Combat;
using UJam.Runtime.Grid;
using UJam.Runtime.Phase;

namespace UJam.Runtime.Player
{
    public sealed class PlayerRuntimeBinder : MonoBehaviour
    {
        // 같은 GameObject에서 사용할 Player 입력 Adapter
        [SerializeField] private PlayerInputAdapter _inputAdapter;

        // 같은 GameObject에서 사용할 공격 Executor
        [SerializeField] private PlayerAttackExecutor _attackExecutor;

        // 같은 GameObject에서 사용할 Player Health
        [SerializeField] private Health _health;

        // 명시적으로 주입받는 PhaseSystem
        private PhaseSystem _phaseSystem;

        // 명시적으로 주입받는 Grid 조회 포트
        private IGridAreaQuery _gridAreaQuery;

        // 명시적으로 주입받는 공격 대상 Provider
        private IPlayerAttackTargetProvider _targetProvider;

        // 현재 의존성으로 계산한 읽기 전용 Player 상태
        public PlayerRuntimeState State
        {
            get
            {
                // 현재 의존성으로 최신 상태를 계산
                return new PlayerRuntimeState(
                    _phaseSystem,
                    _gridAreaQuery,
                    _targetProvider,
                    _health);
            }
        }

        // PhaseSystem을 명시적으로 주입
        public void ConfigurePhaseSystem(PhaseSystem phaseSystem)
        {
            // Player가 사용할 PhaseSystem을 저장
            _phaseSystem = phaseSystem;
        }

        // Grid 조회 포트를 명시적으로 주입
        public void ConfigureGridAreaQuery(IGridAreaQuery gridAreaQuery)
        {
            // Player가 사용할 Grid 포트를 저장
            _gridAreaQuery = gridAreaQuery;
        }

        // 공격 대상 Provider를 명시적으로 주입
        public void ConfigureAttackTargetProvider(IPlayerAttackTargetProvider targetProvider)
        {
            // Player가 사용할 Target Provider를 저장
            _targetProvider = targetProvider;
        }

        // 초기화 시 같은 GameObject Component만 fallback으로 확인
        private void Awake()
        {
            // Inspector 누락 시 같은 GameObject의 Input Adapter를 사용
            if (_inputAdapter == null)
            {
                _inputAdapter = GetComponent<PlayerInputAdapter>();
            }

            // Inspector 누락 시 같은 GameObject의 Executor를 사용
            if (_attackExecutor == null)
            {
                _attackExecutor = GetComponent<PlayerAttackExecutor>();
            }

            // Inspector 누락 시 같은 GameObject의 Health를 사용
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }
        }

        // Binder 활성화 시 입력 이벤트를 연결
        private void OnEnable()
        {
            // 입력 Adapter가 있을 때만 이벤트를 연결
            if (_inputAdapter != null)
            {
                _inputAdapter.AttackRequested -= OnAttackRequested;
                _inputAdapter.AttackRequested += OnAttackRequested;
            }
        }

        // Binder 비활성화 시 입력 이벤트를 해제
        private void OnDisable()
        {
            // 입력 Adapter가 있을 때만 이벤트를 해제
            if (_inputAdapter != null)
            {
                _inputAdapter.AttackRequested -= OnAttackRequested;
            }
        }

        // 유효한 Combat 요청만 Executor로 전달
        private void OnAttackRequested()
        {
            // 현재 상태를 다시 계산해 요청 시점 조건을 확인
            PlayerRuntimeState state = State;

            // 모든 의존성과 Combat 생존 조건이 없으면 요청을 버림
            if (!state.CanAttack || _attackExecutor == null)
            {
                // 대상 조회와 피해 전달 없이 종료
                return;
            }

            // 유효한 요청을 Executor에 한 번 전달
            _attackExecutor.TryExecuteAttack(_targetProvider, _gridAreaQuery);
        }
    }
}
