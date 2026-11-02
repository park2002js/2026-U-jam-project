namespace UJam.Runtime.Enemy.FSM
{
    public sealed class SummonState
    {
        private readonly EnemyBase _enemy;
        public SummonState(EnemyBase enemy, EnemyFSM fsm) => _enemy = enemy;
        public void Enter() => _enemy.Summon();
        public void Exit() { }
    }
}
