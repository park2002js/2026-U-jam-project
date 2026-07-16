using UJam.Runtime.Grid;

namespace UJam.Runtime.Navigation
{
    public readonly struct NavigationRequest
    {
        // 목적지와 통과 조건을 저장하는 이동 요청 생성자
        public NavigationRequest(
            GridCell destination,
            TraversalProfile traversalProfile,
            GridDistance requiredAttackDistance)
        {
            Destination = destination;
            TraversalProfile = traversalProfile;
            RequiredAttackDistance = requiredAttackDistance;
        }

        // Grid 기준 목적지 Cell
        public GridCell Destination { get; }

        // 이동에 필요한 통과 능력
        public TraversalProfile TraversalProfile { get; }

        // 목적지에 도착하기 위해 필요한 공격 거리
        public GridDistance RequiredAttackDistance { get; }
    }
}
