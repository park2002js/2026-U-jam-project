using UJam.Runtime.Enemy;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class DeadState
    {
        // 사망 행동을 구현하는 Enemy
        private readonly EnemyBase _enemy;

        // terminal 상태와 공유 타겟을 관리하는 FSM
        private readonly EnemyFSM _fsm;

        // Enemy와 FSM 연결
        public DeadState(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        // 공유 타겟 제거와 모든 사망 행동 실행
        public void Die()
        {
            // 실제 Dead 상태에서만 사망 행동 허용
            if (_fsm.State != EnemyStateKind.Dead)
            {
                // 다른 상태의 사망 행동 종료
                return;
            }

            // 사망한 Enemy의 공유 타겟 제거
            _fsm.ClearTarget();

            // Enemy 사망 행동 호출
            _enemy.Dead();
        }
    }
}
