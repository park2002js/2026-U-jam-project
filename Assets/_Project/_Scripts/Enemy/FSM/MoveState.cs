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

        // 이동 시작
        public void Enter()
        {
            // 우선 공격 대상이 사거리 안에 존재하지 않으면 (Check의 반환이 False) Move 함수 발동
            if(!_fsm.TGV.Check()) _enemy.Move();
            // 사거리 내에 존재하면 바로 Attack 상태로 전환한다.
            else _fsm.SetState(EnemyStateType.Attack);
        }
    }
}
