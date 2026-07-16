using UJam.Runtime.Enemy;
using UJam.Runtime.Navigation;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class MoveState : IEnemyState
    {
        // Move 상태 식별자 반환
        public EnemyStateKind Kind
        {
            get
            {
                // Move 상태 종류 반환
                return EnemyStateKind.Move;
            }
        }

        // 이동 상태 진입 시 별도 concrete 동작을 만들지 않는 경계
        public void Enter(EnemyContext context)
        {
        }

        // 공격 조건 또는 선택적 Navigation 요청에 따른 다음 상태 반환
        public EnemyStateKind Tick(EnemyContext context)
        {
            // 현재 표적 조건 조회
            EnemyTargetCondition condition = context.GetTargetCondition();

            // 표적이 있고 공격 사거리에 들어온 경우 공격 상태 반환
            if (condition.HasTarget && condition.IsWithinAttackRange)
            {
                // 공격 상태 전이 반환
                return EnemyStateKind.Attack;
            }

            // Provider가 이동 요청을 만들 수 있는지 확인
            if (context.TryCreateNavigationRequest(out NavigationRequest navigationRequest))
            {
                // 유효한 Provider 요청만 Navigation Port에 전달
                context.RequestNavigation(navigationRequest);
            }

            // 공격 조건이 아니면 이동 상태 유지 반환
            return EnemyStateKind.Move;
        }

        // Move 이탈 시 별도 concrete 동작을 만들지 않는 경계
        public void Exit(EnemyContext context)
        {
        }
    }
}
