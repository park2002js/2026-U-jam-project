using UJam.Runtime.Enemy;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class DeadState : IEnemyState
    {
        // Dead 상태 식별자 반환
        public EnemyStateKind Kind
        {
            get
            {
                // Dead 상태 종류 반환
                return EnemyStateKind.Dead;
            }
        }

        // 죽음 표현 시작만 외부 Lifecycle Port에 통지
        public void Enter(EnemyContext context)
        {
            // 물리·Animation·제거 없이 죽음 표현 시작 통지
            context.BeginDeathPresentation();
        }

        // Dead 상태를 유지하는 terminal 상태 반환
        public EnemyStateKind Tick(EnemyContext context)
        {
            // terminal Dead 상태 유지 반환
            return EnemyStateKind.Dead;
        }

        // terminal 상태는 이탈하지 않으므로 별도 정리 없음
        public void Exit(EnemyContext context)
        {
        }
    }
}
