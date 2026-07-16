using UJam.Runtime.Combat;
using UJam.Runtime.Grid;
using UJam.Runtime.Navigation;
using UnityEngine;

namespace UJam.Runtime.Enemy.Composition
{
    public sealed class MeleeEnemyRuntimeBinder : MonoBehaviour
    {
        // 기존 이동 Driver Component 연결 슬롯
        [SerializeField] private NavigationDriver _navigationDriver;
        // 근접 표적 Sensor Component 연결 슬롯
        [SerializeField] private MeleeEnemyTargetSensor _targetSensor;
        // 근접 공격 Executor Component 연결 슬롯
        [SerializeField] private MeleeEnemyAttackExecutor _attackExecutor;
        // 죽음 수명주기 Component 연결 슬롯
        [SerializeField] private MeleeEnemyDeathLifecycle _deathLifecycle;
        // 체력 Component 연결 슬롯
        [SerializeField] private Health _health;

        // 조립된 일반 C# Enemy FSM
        private EnemyStateMachine _stateMachine;
        // 사망 이벤트 구독 여부
        private bool _deathSubscribed;

        // Unity 활성화 시 조립 Component와 FSM을 연결
        private void Awake()
        {
            // 누락된 Inspector 참조를 같은 GameObject에서 보완
            if (_navigationDriver == null) _navigationDriver = GetComponent<NavigationDriver>();
            // 누락된 Sensor 참조를 같은 GameObject에서 보완
            if (_targetSensor == null) _targetSensor = GetComponentInChildren<MeleeEnemyTargetSensor>();
            // 누락된 Executor 참조를 같은 GameObject에서 보완
            if (_attackExecutor == null) _attackExecutor = GetComponent<MeleeEnemyAttackExecutor>();
            // 누락된 Lifecycle 참조를 같은 GameObject에서 보완
            if (_deathLifecycle == null) _deathLifecycle = GetComponent<MeleeEnemyDeathLifecycle>();
            // 누락된 Health 참조를 같은 GameObject에서 보완
            if (_health == null) _health = GetComponent<Health>();

            // 기존 Enemy Port를 조립할 Context 생성
            EnemyContext context = new EnemyContext(_navigationDriver, _targetSensor, _attackExecutor, _deathLifecycle);
            // 새 FSM 인스턴스 생성
            _stateMachine = new EnemyStateMachine();
            // FSM을 Lifecycle에 전달해 Animation Event 완료 경계를 연결
            if (_deathLifecycle != null) _deathLifecycle.BindStateMachine(_stateMachine);
            // FSM 초기화와 Move 진입
            _stateMachine.Initialize(context);
            // Health 사망 이벤트를 FSM Dead 요청에 연결
            if (_health != null)
            {
                _health.Died += RequestDead;
                _deathSubscribed = true;
            }
        }

        // Unity 프레임마다 기존 FSM Tick을 한 번 호출
        private void Update()
        {
            // 조립되지 않은 FSM은 안전하게 대기
            if (_stateMachine == null)
            {
                // 초기화 전 Update 종료
                return;
            }

            // 기존 Enemy FSM에 프레임 실행 위임
            _stateMachine.Tick();
        }

        // 외부 Grid 계약을 기존 Navigation과 Sensor에 전달
        public void ConfigureNavigation(IGridMetrics gridMetrics, IGridNavigation gridNavigation)
        {
            // Navigation Driver가 있을 때만 Grid 계약 전달
            if (_navigationDriver != null)
            {
                // 기존 Driver의 공식 초기화 경계 호출
                _navigationDriver.Initialize(gridMetrics, gridNavigation);
            }
            // Sensor가 있을 때만 Grid 계약 전달
            if (_targetSensor != null)
            {
                // Sensor의 명시적 Navigation 구성 경계 호출
                _targetSensor.ConfigureNavigation(gridMetrics, gridNavigation);
            }
        }

        // Health 사망 이벤트에서 FSM Dead 전이를 요청
        private void RequestDead()
        {
            // FSM이 조립된 경우에만 사망 전이 요청
            if (_stateMachine == null)
            {
                // 초기화 전 사망 이벤트 무동작
                return;
            }

            // 기존 FSM의 Dead 전이 경계 호출
            _stateMachine.RequestDead();
        }

        // Unity 제거 전 Health 이벤트 구독 해제
        private void OnDestroy()
        {
            // 구독된 Health가 있을 때만 이벤트 해제
            if (_health != null && _deathSubscribed)
            {
                // 사망 이벤트 구독 해제
                _health.Died -= RequestDead;
                // 해제 완료 상태 기록
                _deathSubscribed = false;
            }
        }
    }
}
