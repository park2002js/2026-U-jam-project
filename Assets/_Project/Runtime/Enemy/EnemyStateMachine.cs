using UJam.Runtime.Enemy.FSM;

namespace UJam.Runtime.Enemy
{
    public sealed class EnemyStateMachine
    {
        // 현재 FSM이 사용하는 외부 계약 Context
        private EnemyContext _context;

        // 현재 실행 중인 일반 C# 상태 객체
        private IEnemyState _currentState;

        // Context 초기화 완료 여부
        private bool _isInitialized;

        // 죽음 표현 완료 통지의 중복 호출 차단 상태
        private bool _deathPresentationCompleted;

        // 외부 생성자가 제공한 Context로 FSM을 시작하고 Move까지 전이
        public void Initialize(EnemyContext context)
        {
            // terminal Dead 상태의 재초기화 여부 확인
            if (_currentState != null && _currentState.Kind == EnemyStateKind.Dead)
            {
                // Dead 상태는 Initialize 재호출로 되돌리지 않음
                return;
            }

            // 이미 초기화된 FSM의 상태 재설정 차단
            if (_isInitialized)
            {
                // 기존 상태와 Context를 유지
                return;
            }

            // 필수 Context 연결 여부 확인
            if (context == null)
            {
                // Context 없는 초기화 요청 차단
                return;
            }

            // 유효한 Context 보관
            _context = context;

            // FSM 초기화 완료 기록
            _isInitialized = true;

            // 죽음 표현 완료 플래그 초기화
            _deathPresentationCompleted = false;

            // 외부 생성과 초기화 완료를 나타내는 Spawn 상태 생성
            _currentState = new SpawnState();

            // Spawn 진입 경계 호출
            _currentState.Enter(_context);

            // 초기화 흐름 안에서 Move 상태로 전이
            TransitionTo(EnemyStateKind.Move);
        }

        // 현재 상태를 한 번 실행하고 상태 전이를 적용
        public void Tick()
        {
            // 초기화되지 않은 FSM의 Tick 차단
            if (!_isInitialized || _currentState == null)
            {
                // 실행할 상태가 없으므로 종료
                return;
            }

            // terminal Dead 상태의 일반 Tick 차단
            if (_currentState.Kind == EnemyStateKind.Dead)
            {
                // Dead 상태를 계속 유지
                return;
            }

            // 현재 상태가 계산한 다음 상태 저장
            EnemyStateKind nextStateKind = _currentState.Tick(_context);

            // 같은 상태 요청은 재진입 없이 유지
            if (nextStateKind == _currentState.Kind)
            {
                // 현재 상태 유지
                return;
            }

            // 계산된 상태로 단일 전이 적용
            TransitionTo(nextStateKind);
        }

        // 현재 상태에서 Dead로 한 번만 강제 전이
        public void RequestDead()
        {
            // 초기화되지 않은 FSM의 사망 요청 차단
            if (!_isInitialized || _currentState == null)
            {
                // 실행할 상태가 없으므로 종료
                return;
            }

            // 이미 Dead인 FSM의 중복 전이 차단
            if (_currentState.Kind == EnemyStateKind.Dead)
            {
                // terminal 상태 유지
                return;
            }

            // 어떤 활성 상태에서도 Dead로 강제 전이
            TransitionTo(EnemyStateKind.Dead);
        }

        // 외부 죽음 표현 완료를 Dead Lifecycle Port에 한 번 통지
        public void CompleteDeathPresentation()
        {
            // 초기화되지 않은 FSM의 완료 통지 차단
            if (!_isInitialized || _currentState == null)
            {
                // 실행할 상태가 없으므로 종료
                return;
            }

            // Dead가 아닌 상태의 완료 통지 차단
            if (_currentState.Kind != EnemyStateKind.Dead)
            {
                // 죽음 표현이 시작되지 않았으므로 종료
                return;
            }

            // 완료 통지가 이미 전달됐는지 확인
            if (_deathPresentationCompleted)
            {
                // 완료 통지 중복 호출 차단
                return;
            }

            // 완료 통지 중복 방지 상태 기록
            _deathPresentationCompleted = true;

            // concrete 표현 완료를 외부 Port에 위임
            _context.CompleteDeathPresentation();
        }

        // 현재 상태를 종료하고 지정된 일반 C# 상태 객체로 전환
        private void TransitionTo(EnemyStateKind nextStateKind)
        {
            // 초기화되지 않은 FSM의 전이 차단
            if (!_isInitialized || _context == null)
            {
                // 전이 조건이 없으므로 종료
                return;
            }

            // terminal Dead 상태에서 모든 이탈 전이 차단
            if (_currentState != null && _currentState.Kind == EnemyStateKind.Dead)
            {
                // Dead 상태 유지
                return;
            }

            // 동일 상태 재진입 차단
            if (_currentState != null && _currentState.Kind == nextStateKind)
            {
                // 현재 상태 유지
                return;
            }

            // 기존 상태가 있으면 이탈 경계 호출
            if (_currentState != null)
            {
                // 기존 상태 종료 처리
                _currentState.Exit(_context);
            }

            // 다음 상태 객체 생성
            IEnemyState nextState = CreateState(nextStateKind);

            // 새 상태 객체 보관
            _currentState = nextState;

            // 새 상태 진입 경계 호출
            _currentState.Enter(_context);
        }

        // 네 가지 상태 종류에 대응하는 일반 C# 상태 객체 생성
        private IEnemyState CreateState(EnemyStateKind stateKind)
        {
            // 상태 종류별 생성 분기
            switch (stateKind)
            {
                // Spawn 상태 객체 생성
                case EnemyStateKind.Spawn:
                    // Spawn 상태 반환
                    return new SpawnState();
                // Move 상태 객체 생성
                case EnemyStateKind.Move:
                    // Move 상태 반환
                    return new MoveState();
                // Attack 상태 객체 생성
                case EnemyStateKind.Attack:
                    // Attack 상태 반환
                    return new AttackState();
                // Dead 상태 객체 생성
                case EnemyStateKind.Dead:
                    // Dead 상태 반환
                    return new DeadState();
                // 정의되지 않은 값은 terminal 상태로 제한
                default:
                    // 예측할 수 없는 상태를 Dead로 제한한 결과 반환
                    return new DeadState();
            }
        }
    }
}
