using UJam.Runtime.Combat;
using UJam.Runtime.Enemy.Projectiles;
using UJam.Runtime.Grid;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    /// <summary>
    /// 원거리 잡몹의 기본 틀을 정의한 Class
    /// EnemyBase의 Attack을 상속받아서,
    /// Projectile을 투사체로 활용하여 ProjectileMovement에 정의된 대로 날려보내는 Attack 방식으로 새로 재구성한다.
    /// </summary>
    public class RangedEnemy : EnemyBase
    {
        // Projectile.cs를 컴포넌트로 보유한, Prefab화 된 날려보낼 투사체
        [SerializeField] private Projectile _projectilePrefab;

        [SerializeField] private Transform _firePoint;
        [SerializeField] private ProjectileMovement _projectileMovement;
        [SerializeField, Min(0.01f)] private float _projectileSpeed = 5f;
        private Vector3 _attackDestination;
        public Vector3 AttackDestination => _attackDestination;

        protected override void CaptureAttackDestination()
        {
            _attackDestination = AttackTargetPosition;
            _attackDestination.y += 1f;
            if (AttackTargetWasBaseCore)
            {
                GridSystem grid = GridSystem.Instance;
                _attackDestination.x = _firePoint != null ? _firePoint.position.x : transform.position.x;
                _attackDestination.y = _firePoint != null ? _firePoint.position.y : transform.position.y;
                _attackDestination.z = grid.Origin.z + grid.BaseCoreRow * grid.CellHeight;
            }
        }

        public override void Attack()
        {
            if (_projectilePrefab == null || _projectileMovement == null || _firePoint == null)
            {
                Debug.LogError($"{name}의 원거리 공격 설정이 완료되지 않았습니다.", this);
                return;
            }

            TryGetAttackTarget(out _, out IDamageable damageable);
            Vector3 destination = _attackDestination;

            Vector3 direction = destination - _firePoint.position;
            Quaternion rotation = direction.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direction)
                : _firePoint.rotation;

            // Prefab에 기반하여, 투사체 인스턴스를 발사 시작 위치에 생성
            Projectile projectile = Instantiate(_projectilePrefab, _firePoint.position, rotation);

            // 투사체 발사
            projectile.Launch(this, damageable, destination, _projectileSpeed, _projectileMovement,
                AttackTargetPosition, AttackTargetWasBaseCore);
        }
    }
}
