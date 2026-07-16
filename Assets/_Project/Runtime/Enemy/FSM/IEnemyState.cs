using UJam.Runtime.Enemy;

namespace UJam.Runtime.Enemy.FSM
{
    public interface IEnemyState
    {
        // 상태 객체가 대표하는 네 가지 상태 중 하나 반환
        EnemyStateKind Kind { get; }

        // 상태 진입 시 Context 기반 초기화 경계
        void Enter(EnemyContext context);

        // 한 번의 FSM Tick 결과로 다음 상태 후보 반환
        EnemyStateKind Tick(EnemyContext context);

        // 상태 이탈 시 정리 경계
        void Exit(EnemyContext context);
    }
}
