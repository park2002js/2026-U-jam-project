namespace UJam.Runtime.Enemy.FSM
{
    public sealed class InitState
    {
        private readonly EnemyBase _enemy;
        private readonly EnemyFSM _fsm;
        public InitState(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        public void Enter()
        {
            _enemy.Init();
            _fsm.SetState(EnemyStateType.Idle);
        }
    }
}
