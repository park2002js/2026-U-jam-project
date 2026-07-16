using UJam.Runtime.Enemy;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class SpawnState : IEnemyState
    {
        // Spawn 상태 식별자 반환
        public EnemyStateKind Kind
        {
            get
            {
                // Spawn 상태 종류 반환
                return EnemyStateKind.Spawn;
            }
        }

        // 외부 생성과 Initialize 완료를 나타내는 Spawn 진입 경계
        public void Enter(EnemyContext context)
        {
        }

        // 초기화 완료 뒤 Move로 넘길 다음 상태 반환
        public EnemyStateKind Tick(EnemyContext context)
        {
            // 생성 완료 후 이동 상태 반환
            return EnemyStateKind.Move;
        }

        // Spawn 이탈 시 별도 정리가 없는 경계
        public void Exit(EnemyContext context)
        {
        }
    }
}
