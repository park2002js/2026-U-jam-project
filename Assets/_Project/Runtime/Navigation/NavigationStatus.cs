namespace UJam.Runtime.Navigation
{
    public enum NavigationStatus
    {
        // 이동 요청을 처리하는 중인 상태
        Moving,

        // 목적지에 도착한 상태
        Arrived,

        // 공통 장애물로 진행이 막힌 상태
        Blocked,

        // 승인된 실패 사유로 이동을 완료하지 못한 상태
        Failed,

        // 현재 경로를 다시 계산해야 하는 상태
        NeedsRepath
    }
}
