using UJam.Runtime.Enemy;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class AttackState : IEnemyState
    {
        // Attack 상태 식별자 반환
        public EnemyStateKind Kind
        {
            get
            {
                // Attack 상태 종류 반환
                return EnemyStateKind.Attack;
            }
        }

        // 공격 상태 진입 시 외부 concrete 동작을 만들지 않는 경계
        public void Enter(EnemyContext context)
        {
        }

        // Tick 직전 조건 재검사와 선택적 공격 실행 뒤 다음 상태 반환
        public EnemyStateKind Tick(EnemyContext context)
        {
            // 공격 직전 현재 표적 조건 재조회
            EnemyTargetCondition condition = context.GetTargetCondition();

            // 표적이 없거나 사거리를 벗어나면 이동 상태 반환
            if (!condition.HasTarget || !condition.IsWithinAttackRange)
            {
                // 이동 상태 전이 반환
                return EnemyStateKind.Move;
            }

            // 조건을 통과한 공격 실행을 외부 Port에 위임
            context.ExecuteAttack();

            // 공격 조건이 유지되므로 공격 상태 유지 반환
            return EnemyStateKind.Attack;
        }

        // Attack 이탈 시 별도 concrete 동작을 만들지 않는 경계
        public void Exit(EnemyContext context)
        {
        }
    }
}
