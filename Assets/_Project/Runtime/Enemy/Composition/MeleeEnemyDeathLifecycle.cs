using UJam.Runtime.Enemy;
using UJam.Runtime.Navigation;
using UnityEngine;

namespace UJam.Runtime.Enemy.Composition
{
    public sealed class MeleeEnemyDeathLifecycle : MonoBehaviour, IEnemyDeathLifecyclePort
    {
        // 정지할 표적 Sensor 연결 슬롯
        [SerializeField] private MeleeEnemyTargetSensor _targetSensor;
        // 정지할 공격 Executor 연결 슬롯
        [SerializeField] private MeleeEnemyAttackExecutor _attackExecutor;
        // 정지할 Navigation Driver 연결 슬롯
        [SerializeField] private NavigationDriver _navigationDriver;
        // 선택적 Animator 연결 슬롯
        [SerializeField] private Animator _animator;
        // FSM 완료 통지 대상
        private EnemyStateMachine _stateMachine;
        // GameObject 제거 완료 중복 차단
        private bool _destroyRequested;
        // FSM 완료 통지 수신 여부
        private bool _completionAcknowledged;

        // Unity 활성화 시 누락된 런타임 참조 보완
        private void Awake()
        {
            // 같은 GameObject에서 Sensor 보완
            if (_targetSensor == null) _targetSensor = GetComponentInChildren<MeleeEnemyTargetSensor>();
            // 같은 GameObject에서 Executor 보완
            if (_attackExecutor == null) _attackExecutor = GetComponent<MeleeEnemyAttackExecutor>();
            // 같은 GameObject에서 Navigation 보완
            if (_navigationDriver == null) _navigationDriver = GetComponent<NavigationDriver>();
            // 같은 GameObject에서 Animator 보완
            if (_animator == null) _animator = GetComponent<Animator>();
        }

        // Binder가 조립한 FSM을 완료 통지 대상으로 연결
        public void BindStateMachine(EnemyStateMachine stateMachine)
        {
            // 외부 FSM 참조 보관
            _stateMachine = stateMachine;
        }

        // Dead 진입 시 Runtime 동작을 멈추고 시체 Layer 적용
        public void BeginDeathPresentation()
        {
            // 이미 제거를 요청한 객체는 추가 동작 차단
            if (_destroyRequested)
            {
                // 종료 중복 호출 무동작
                return;
            }
            // Sensor Runtime 정지
            if (_targetSensor != null)
            {
                // 표적 감지 정지
                _targetSensor.StopRuntime();
            }
            // Attack Runtime 정지
            if (_attackExecutor != null)
            {
                // 공격 실행 정지
                _attackExecutor.StopRuntime();
            }
            // Navigation Component 정지
            if (_navigationDriver != null)
            {
                // 이동 Update와 요청 처리 정지
                _navigationDriver.enabled = false;
            }
            // 존재하는 EnemyCorpse Layer만 적용
            int corpseLayer = LayerMask.NameToLayer("EnemyCorpse");
            // Layer가 프로젝트에 있을 때만 GameObject에 적용
            if (corpseLayer >= 0)
            {
                // 시체 Layer 적용
                gameObject.layer = corpseLayer;
            }
            // Animator가 있으면 현재 재생을 유지하고 Event 완료를 기다림
            if (_animator != null)
            {
                // Animator 참조 존재만 확인하고 Controller는 만들지 않음
                _animator.enabled = true;
            }
        }

        // FSM이 내부적으로 호출하는 완료 Port는 재귀 없이 대기
        public void CompleteDeathPresentation()
        {
            // FSM의 완료 통지를 Lifecycle 상태에 기록
            _completionAcknowledged = true;
        }

        // Animator Event가 호출하는 사망 Animation 완료 함수
        public void NotifyDeathAnimationFinished()
        {
            // 이미 제거를 요청했으면 중복 호출 차단
            if (_destroyRequested)
            {
                // 중복 Animation Event 무동작
                return;
            }
            // 제거 요청 상태를 먼저 기록
            _destroyRequested = true;
            // 이전 완료 통지 상태 초기화
            _completionAcknowledged = false;
            // FSM이 Dead 완료를 한 번 통지하도록 요청
            if (_stateMachine != null)
            {
                // 기존 FSM 완료 경계 호출
                _stateMachine.CompleteDeathPresentation();
            }
            // 현재 Enemy GameObject 제거 예약
            Destroy(gameObject);
        }
    }
}
