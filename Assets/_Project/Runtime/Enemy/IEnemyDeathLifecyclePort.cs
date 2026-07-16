namespace UJam.Runtime.Enemy
{
    public interface IEnemyDeathLifecyclePort
    {
        // 죽음 표현 시작을 알리는 경계
        void BeginDeathPresentation();

        // 죽음 표현 완료를 알리는 경계
        void CompleteDeathPresentation();
    }
}
