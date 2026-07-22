using UJam.Runtime.Enemy;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class IdleState
    {
        // Spawn 행동을 구현하는 Enemy
        private readonly EnemyBase _enemy;

        // Spawn 완료 전환을 관리하는 FSM
        private readonly EnemyFSM _fsm;

        // Spawn 중복 실행 차단 상태
        private bool _started;

        // Enemy와 FSM 연결
        public IdleState(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        // 최초 Spawn 행동 실행
        public void Spawn()
        {
            // 이미 시작한 Spawn 중복 차단
            if (_started)
            {
                // 중복 실행 종료
                return;
            }

            _started = true;

            // Enemy Spawn 행동 호출
            _enemy.Spawn();
        }

        // Coroutine 또는 외부 신호 대기 경계
        public void Wait()
        {
            // 시간 대기 또는 외부 신호 연결 로직이 구현되어야 함
        }

        // Spawn 대기 완료와 Move 전환
        public bool Done()
        {
            // FSM Spawn 완료 결과 반환
            return _fsm.FinishSpawn();
        }
    }
}
