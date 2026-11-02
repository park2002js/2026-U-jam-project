using System.Collections;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public class RollbugEnemy : EnemyBase
    {
        [SerializeField] private GameObject _detonationEffect;
        [SerializeField] private GameObject _deathEffect;
        private bool _resolved;
        public override bool ImmediateAttack => true;

        public override void Attack()
        {
            if (_resolved || Status.HP <= 0 || Status.IsStunned || !FSM.TGV.Check() ||
                !TryGetAttackTarget(out _, out var damageable)) return;
            _resolved = true;
            if (_detonationEffect != null) Instantiate(_detonationEffect, transform.position, Quaternion.identity);
            // shortcut: explosion radius is unspecified; damage the current target until an area is approved.
            try
            {
                damageable.TakeDamage(new UJam.Runtime.Combat.DamageInfo(Status.OutgoingDamage, name, UJam.Runtime.Combat.DamageSourceKind.Enemy));
            }
            finally
            {
                try { Status.ApplyDamage(float.MaxValue, true); }
                finally { FSM.SetState(EnemyStateType.Dead); }
            }
        }

        public override void Dead()
        {
            if (!_resolved)
            {
                _resolved = true;
                if (_deathEffect != null) Instantiate(_deathEffect, transform.position, Quaternion.identity);
            }
            base.Dead();
        }

        public override IEnumerator DeadAnim()
        {
            Destroy(gameObject);
            yield break;
        }
    }
}
