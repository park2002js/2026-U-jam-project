using UJam.Runtime.Navigation;

namespace UJam.Runtime.Enemy
{
    public interface IEnemyTargetConditionProvider
    {
        // 현재 표적과 공격 사거리 조건 조회
        EnemyTargetCondition GetCurrentCondition();

        // 현재 조건에 맞는 이동 요청 생성 가능 여부 조회
        bool TryCreateNavigationRequest(out NavigationRequest navigationRequest);
    }
}
