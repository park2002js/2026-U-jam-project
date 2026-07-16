namespace UJam.Runtime.Navigation
{
    // 외부 A* 구현은 향후 UJam.Integration.AStar Adapter가 이 계약 뒤에서 연결할 예정
    // Runtime Navigation은 외부 A* 타입을 직접 참조하지 않음
    // 현재 WO에서는 기능이 연결되지 않았고 후속 승인 Work Order 필요
    public interface INavigationPathfinder
    {
        // 경로 요청을 외부 탐색 구현에 전달
        NavigationPathResult FindPath(NavigationPathRequest request);
    }
}
