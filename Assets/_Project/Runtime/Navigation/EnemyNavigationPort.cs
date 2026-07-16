namespace UJam.Runtime.Navigation
{
    public interface EnemyNavigationPort
    {
        // 목적지 이동 요청 제출 경계
        void RequestNavigation(NavigationRequest request);

        // 현재 이동 결과 조회 경계
        NavigationResult GetCurrentResult();
    }
}
