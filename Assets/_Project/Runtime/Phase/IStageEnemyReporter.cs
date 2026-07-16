namespace UJam.Runtime.Phase
{
    public interface IStageEnemyReporter
    {
        // 적 생성 등록과 발행 토큰 반환을 요청
        bool TryRegisterEnemySpawned(out EnemyStageToken enemyToken);

        // 발행된 적 토큰의 처치를 보고
        bool TryReportEnemyDefeated(EnemyStageToken enemyToken);

        // 웨이브 적 생성 종료를 보고
        bool TryCompleteWaveSpawning();
    }
}
