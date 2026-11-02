namespace UJam.Runtime.Enemy.FSM
{
    public sealed class SpawnState
    {
        private readonly EnemyBase _enemy;
        public SpawnState(EnemyBase enemy, EnemyFSM fsm) => _enemy = enemy;
        public void Enter() => _enemy.Spawn();
        public void Exit() { }
    }
}
