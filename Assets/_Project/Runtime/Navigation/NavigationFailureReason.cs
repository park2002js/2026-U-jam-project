namespace UJam.Runtime.Navigation
{
    public enum NavigationFailureReason
    {
        // 실패 사유가 없는 상태
        None,

        // 이동 가능한 경로가 없는 실패
        NoPath,

        // Grid가 초기화되지 않은 실패
        GridNotInitialized,

        // 이동 Motor가 없는 실패
        MotorMissing,

        // 외부 경로 Provider에서 발생한 실패
        ProviderError
    }
}
