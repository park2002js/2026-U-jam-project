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
            // 발표용 임시 이동을 끊고 공격 준비
            _enemy.StopMovement();

            // 발표용 Attack 상태 진입 확인
            Debug.Log($"[EnemyFSM] {_enemy.gameObject.name} Attack 상태 진입", _enemy.gameObject);
        }

        // 현재 타겟 공격 실행
        public void Hit()
        {
            // 실제 Attack 상태와 최신 발표용 Grid 사거리 확인
            if (_fsm.State != EnemyStateKind.Attack || !_fsm.CanAttackTarget())
            {
                // 다른 상태 또는 사거리 밖 공격 요청 종료
                return;
            }

            // 검증된 현재 Target과 같은 col의 Grid 지점 공격
            _enemy.Attack(_fsm.Target, _fsm.AttackPoint);
        }
    }
}
