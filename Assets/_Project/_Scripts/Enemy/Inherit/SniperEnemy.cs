using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public sealed class SniperEnemy : EnemyBase
    {
        [SerializeField] private Transform _firePoint;
        [SerializeField] private GameObject _shotEffect;

        public override void Attack()
        {
            base.Attack();
            if (_shotEffect != null)
            {
                Transform source = _firePoint != null ? _firePoint : transform;
                Vector3 direction = AttackTargetPosition - source.position;
                Instantiate(_shotEffect, source.position,
                    direction.sqrMagnitude > 0f ? Quaternion.LookRotation(direction) : source.rotation);
            }
        }
    }
}
