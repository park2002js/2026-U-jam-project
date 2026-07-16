namespace UJam.Runtime.Enemy
{
    public interface IEnemyAttackExecutor
    {
        // 공격 상태가 조건을 통과한 뒤 실행을 요청하는 경계
        void ExecuteAttack();
    }
}
