namespace UJam.Runtime.Navigation
{
    public interface IMovementMotor
    {
        // 이동체가 현재 점유한 Cell 제공
        UJam.Runtime.Grid.GridCell CurrentCell { get; }

        // 승인된 Grid 경로의 실제 이동 시작
        void BeginPath(NavigationPath path);

        // 실제 이동 결과 한 Tick 처리
        NavigationMotorResult Tick(float deltaTime);

        // 현재 실제 이동 중단
        void Stop();
    }
}
