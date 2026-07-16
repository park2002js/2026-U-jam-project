using UJam.Runtime.Navigation;

namespace UJam.Runtime.Enemy
{
    public sealed class EnemyContext
    {
        // 이동 요청을 전달하는 기존 Navigation Port
        private readonly EnemyNavigationPort _navigationPort;

        // 표적 조건과 이동 요청 생성을 제공하는 확장 Port
        private readonly IEnemyTargetConditionProvider _targetConditionProvider;

        // 공격 실행을 요청하는 확장 Port
        private readonly IEnemyAttackExecutor _attackExecutor;

        // 죽음 표현 수명주기를 알리는 확장 Port
        private readonly IEnemyDeathLifecyclePort _deathLifecyclePort;

        // FSM이 사용할 외부 Port를 명시적으로 보관하는 Context 생성자
        public EnemyContext(
            EnemyNavigationPort navigationPort,
            IEnemyTargetConditionProvider targetConditionProvider,
            IEnemyAttackExecutor attackExecutor,
            IEnemyDeathLifecyclePort deathLifecyclePort)
        {
            _navigationPort = navigationPort;
            _targetConditionProvider = targetConditionProvider;
            _attackExecutor = attackExecutor;
            _deathLifecyclePort = deathLifecyclePort;
        }

        // 표적 Provider에서 현재 조건을 읽고 연결이 없으면 기본 조건 반환
        public EnemyTargetCondition GetTargetCondition()
        {
            // 표적 조건 Provider 연결 여부 확인
            if (_targetConditionProvider == null)
            {
                // 연결되지 않은 Provider의 안전한 기본 조건 반환
                return default;
            }

            // Provider가 계산한 표적 조건 반환
            return _targetConditionProvider.GetCurrentCondition();
        }

        // 표적 Provider의 이동 요청 생성 가능 여부와 요청 값 전달
        public bool TryCreateNavigationRequest(out NavigationRequest navigationRequest)
        {
            // 출력 요청의 기본값 초기화
            navigationRequest = default;

            // 표적 조건 Provider 연결 여부 확인
            if (_targetConditionProvider == null)
            {
                // 이동 요청을 만들 수 없음을 반환
                return false;
            }

            // Provider의 이동 요청 생성 결과 반환
            return _targetConditionProvider.TryCreateNavigationRequest(out navigationRequest);
        }

        // Navigation Port가 연결된 경우 이동 요청 전달
        public void RequestNavigation(NavigationRequest navigationRequest)
        {
            // Navigation Port 연결 여부 확인
            if (_navigationPort == null)
            {
                // 연결되지 않은 Navigation Port는 무동작 처리
                return;
            }

            // 기존 Navigation 계약으로 이동 요청 전달
            _navigationPort.RequestNavigation(navigationRequest);
        }

        // 공격 Executor가 연결된 경우 공격 실행 요청
        public void ExecuteAttack()
        {
            // 공격 Executor 연결 여부 확인
            if (_attackExecutor == null)
            {
                // 연결되지 않은 공격 Port는 무동작 처리
                return;
            }

            // concrete 공격 실행을 외부 Port에 위임
            _attackExecutor.ExecuteAttack();
        }

        // Death Lifecycle Port가 연결된 경우 죽음 표현 시작 통지
        public void BeginDeathPresentation()
        {
            // Death Lifecycle Port 연결 여부 확인
            if (_deathLifecyclePort == null)
            {
                // 연결되지 않은 Death Port는 무동작 처리
                return;
            }

            // 죽음 표현 시작을 외부 Port에 통지
            _deathLifecyclePort.BeginDeathPresentation();
        }

        // Death Lifecycle Port가 연결된 경우 죽음 표현 완료 통지
        public void CompleteDeathPresentation()
        {
            // Death Lifecycle Port 연결 여부 확인
            if (_deathLifecyclePort == null)
            {
                // 연결되지 않은 Death Port는 무동작 처리
                return;
            }

            // 죽음 표현 완료를 외부 Port에 통지
            _deathLifecyclePort.CompleteDeathPresentation();
        }
    }
}
