using UJam.Runtime.Enemy;
using UnityEngine;

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

            // Enemy Navigation 이동 호출
            _enemy.Move();

            // 이미 전달된 사거리 정보가 유효하면 Attack 전환
            if (_fsm.Target != null && _fsm.InRange)
            {
                _fsm.SetState(EnemyStateKind.Attack);
            }
        }

        // 외부 범위 판정 결과 전달
        public void SetRange(Object target, bool inside)
        {
            // 중앙 FSM에 사거리 정보 전달
            _fsm.SetRange(target, inside);
        }
    }
}
