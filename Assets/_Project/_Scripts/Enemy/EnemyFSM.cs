using System;
using System.Collections.Generic;
using UJam.Runtime.Enemy.FSM;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public enum EnemyStateType
    {
        None = 0,
        Idle = 1,
        Move = 2,
        Attack = 3,
        Dead = 4,
        Init = 5,
        Spawn = 6,
        Summon = 7
    }

    public class EnemyFSM
    {
        private readonly EnemyBase _enemy;
        private EnemyStateType _pendingState = EnemyStateType.None;
        public EnemyStateType state { get; private set; }
        public EnemyStateType PendingState => _pendingState;
        public List<GameObject> Targets { get; } = new List<GameObject>();
        public TargetValidator TGV { get; }
        public InitState Init { get; }
        public IdleState Idle { get; }
        public MoveState Move { get; }
        public AttackState Attack { get; }
        public DeadState Dead { get; }
        public SpawnState Spawn { get; }
        public SummonState Summon { get; }

        public EnemyFSM(EnemyBase enemy)
        {
            _enemy = enemy;
            Init = new InitState(enemy, this);
            Idle = new IdleState(enemy, this);
            Move = new MoveState(enemy, this);
            Attack = new AttackState(enemy, this);
            Dead = new DeadState(enemy, this);
            Spawn = new SpawnState(enemy, this);
            Summon = new SummonState(enemy, this);
            TGV = new TargetValidator(enemy, this);
        }

        public bool SetState(EnemyStateType next)
        {
            if (!Enum.IsDefined(typeof(EnemyStateType), next) || next == EnemyStateType.None ||
                next == state || state == EnemyStateType.Dead ||
                next == EnemyStateType.Init && state != EnemyStateType.None)
                return false;

            if (_enemy.IsAttackPlaying && !_enemy.ImmediateAttack && next != EnemyStateType.Dead)
            {
                _pendingState = next;
                return true;
            }
            _pendingState = EnemyStateType.None;

            switch (state)
            {
                case EnemyStateType.Move: Move.Exit(); break;
                case EnemyStateType.Attack: Attack.Exit(); break;
                case EnemyStateType.Spawn: Spawn.Exit(); break;
                case EnemyStateType.Summon: Summon.Exit(); break;
            }

            state = next;
            switch (next)
            {
                case EnemyStateType.Init: Init.Enter(); break;
                case EnemyStateType.Idle: Idle.Enter(); break;
                case EnemyStateType.Move: Move.Enter(); break;
                case EnemyStateType.Attack: Attack.Enter(); break;
                case EnemyStateType.Dead: Dead.Enter(); break;
                case EnemyStateType.Spawn: Spawn.Enter(); break;
                case EnemyStateType.Summon: Summon.Enter(); break;
            }
            return true;
        }

        public void SetDead() => SetState(EnemyStateType.Dead);

        public void CompleteAttack()
        {
            if (state != EnemyStateType.Attack || _enemy.IsAttackPlaying) return;
            EnemyStateType pending = _pendingState;
            _pendingState = EnemyStateType.None;
            if (pending != EnemyStateType.None) SetState(pending);
            else if (!TGV.Check()) SetState(EnemyStateType.Move);
        }

        public void ReTargeting()
        {
            if (state == EnemyStateType.Attack && _enemy.IsAttackPlaying) return;
            if (state == EnemyStateType.Move)
            {
                if (!TGV.TryGetTarget(out _))
                {
                    _enemy.Movement?.Exit();
                    _enemy.Animation?.Play("Idle");
                }
                else if (TGV.Check()) SetState(EnemyStateType.Attack);
                else _enemy.Movement.ReTargeting();
            }
            else if (state == EnemyStateType.Attack && !TGV.Check())
            {
                SetState(EnemyStateType.Move);
            }
        }
    }
}
