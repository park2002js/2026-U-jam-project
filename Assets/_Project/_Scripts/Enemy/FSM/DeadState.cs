using UJam.Runtime.Enemy;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class DeadState
    {
        // Dead 행동을 구현하는 Enemy
        private EnemyBase _enemy;

        private EnemyFSM _fsm;

        // Enemy와 FSM 연결
        public DeadState(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        public void Enter()
        {
            _enemy.Dead();
        }
    }
}
