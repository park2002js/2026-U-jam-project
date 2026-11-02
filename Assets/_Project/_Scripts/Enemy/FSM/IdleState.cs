namespace UJam.Runtime.Enemy.FSM
{
    public sealed class IdleState
    {
        private readonly EnemyBase _enemy;
        public IdleState(EnemyBase enemy, EnemyFSM fsm) => _enemy = enemy;

        public void Enter()
        {
            _enemy.Idle();
            _enemy.Animation?.Play("Idle");
        }
    }
}
