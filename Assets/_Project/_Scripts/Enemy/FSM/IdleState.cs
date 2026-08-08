using UJam.Runtime.Enemy;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class IdleState
    {
        // Idle 행동을 구현하는 Enemy
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

        // Idle 상태에 진입할 때 마다 호출되는 함수
        public void Enter()
        {
            _enemy.Idle();  
        }
    }
}
