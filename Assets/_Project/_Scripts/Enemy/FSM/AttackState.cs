using UJam.Runtime.Enemy;
using UnityEngine;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class AttackState
    {
        // 공격 행동을 구현하는 Enemy
        private readonly EnemyBase _enemy;

        // 현재 타겟과 Move 전환을 관리하는 FSM
        private readonly EnemyFSM _fsm;

        // Enemy와 FSM 연결
        public AttackState(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        // Attack 상태 진입 준비
        public void Ready()
        {
            // 공격 Animation 선딜레이 또는 준비 로직이 구현되어야 함
        }

        // 현재 타겟 공격 실행
        public void Hit()
        {
            // 실제 Attack 상태에서만 공격 허용
            if (_fsm.State != EnemyStateKind.Attack)
            {
                // 다른 상태의 공격 요청 종료
                return;
            }

            // 공격 전 사거리 재검사 없이 현재 타겟 사용
            _enemy.Attack(_fsm.Target);
        }

        // 외부 효과로 타겟 교체와 Move 전환
        public void ChangeTarget(Object target)
        {
            // 중앙 FSM에 새 타겟 전달
            _fsm.SetTarget(target);
        }
    }
}
