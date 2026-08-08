using UJam.Runtime.Enemy;
using UnityEngine;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class AttackState
    {
        // 공격 행동을 구현하는 Enemy
        private EnemyBase _enemy;

        private EnemyFSM _fsm;

        // Enemy와 FSM 연결
        public AttackState(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        // Attack 상태 진입 준비
        public void Enter()
        {
            // TargetValidator가 True를 내보내면 계속 공격을 진행
            while(_fsm.TGV.Check())
            {
                _enemy.Attack();                
            }
            // 반복문이 종료되면 False를 내보낸 것이므로 Move 상태로 변경
            _fsm.SetState(EnemyStateType.Move);
        }
    }
}
