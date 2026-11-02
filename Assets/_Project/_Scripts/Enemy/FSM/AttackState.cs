using System.Collections;
using UnityEngine;

namespace UJam.Runtime.Enemy.FSM
{
    public sealed class AttackState
    {
        private readonly EnemyBase _enemy;
        private readonly EnemyFSM _fsm;
        private Coroutine _coroutine;

        public AttackState(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        public void Enter()
        {
            if (_enemy.ImmediateAttack && !_enemy.Status.IsStunned) _enemy.BeginAttack();
            else _coroutine = _enemy.StartCoroutine(AttackLoop());
        }

        public void Exit()
        {
            _enemy.CancelAttack();
            if (_coroutine != null) _enemy.StopCoroutine(_coroutine);
            _coroutine = null;
        }

        private IEnumerator AttackLoop()
        {
            float nextAttackAt = Time.time;
            while (_fsm.state == EnemyStateType.Attack)
            {
                if (_enemy.IsAttackPlaying)
                {
                    yield return null;
                    continue;
                }
                if (!_fsm.TGV.Check())
                {
                    _fsm.SetState(EnemyStateType.Move);
                    yield break;
                }

                if (!_enemy.IsAttackPlaying && _enemy.TrySummon()) yield break;

                if (_enemy.ImmediateAttack)
                {
                    if (!_enemy.Status.IsStunned)
                    {
                        _enemy.BeginAttack();
                        yield break;
                    }
                    yield return null;
                    continue;
                }

                float attackSpeed = _enemy.Status.AttackSpeed;
                if (!_enemy.Status.IsStunned && float.IsFinite(attackSpeed) && attackSpeed > 0f &&
                    !_enemy.IsAttackPlaying && Time.time >= nextAttackAt)
                {
                    nextAttackAt = Time.time + 1f / attackSpeed;
                    _enemy.BeginAttack();
                }
                yield return null;
            }
        }
    }
}
