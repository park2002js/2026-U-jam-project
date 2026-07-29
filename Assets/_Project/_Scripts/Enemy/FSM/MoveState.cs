using UJam.Runtime.Enemy;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class MoveState
    {
        // 이동 행동을 구현하는 Enemy
        private readonly EnemyBase _enemy;

        // Move와 Attack 전환을 관리하는 FSM
        private readonly EnemyFSM _fsm;

        // Enemy와 FSM 연결
        public MoveState(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        // 현재 목적지 이동 요청
        public void Go()
        {
            // 실제 Move 상태에서만 이동 요청 허용
            if (_fsm.State != EnemyStateKind.Move)
            {
                // 다른 상태의 이동 요청 종료
                return;
            }

            // 발표용 임시 직선 이동 허용
            _enemy.StartMovement();
        }
    }
}
