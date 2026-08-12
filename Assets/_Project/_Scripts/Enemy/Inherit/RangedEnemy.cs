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

        public override void Attack()
        {
            if (_projectilePrefab == null || _projectileMovement == null || _firePoint == null)
            {
                Debug.LogError($"{name}의 원거리 공격 설정이 완료되지 않았습니다.", this);
                return;
            }

            if (FSM.Targets.Count == 0)
            {
                Debug.LogError($"{name}의 공격 대상이 없습니다.", this);
                return;
            }
            
            // 공격 대상 정보를 가져온 뒤, 
            // 우선 공격 대상을 담는 Stack이 "GameObject"타입을 담는 것으로 정의되어 있기 때문에, TakeDamage를 호출하기 위해서 IDamageable로 형변환을 시도
            GameObject target = FSM.Targets[FSM.Targets.Count - 1];
            IDamageable damageable = target != null
                ? target.GetComponent<IDamageable>()
                : null;

            if (damageable == null)
            {
                Debug.LogError($"{name}의 공격 대상이 유효하지 않습니다.", this);
                return;
            }

            Vector3 destination = target.transform.position;
            destination.y = 1f;

            // 공격 대상이 거점이면, 투사체가 도달할 x축 좌표를 Enemy가 위치한 좌표로 고정하고, Z축 좌표는 거점이 위치한 Row 줄로 설정한다.
            if (target.CompareTag("BaseCore"))
            {
                GridSystem grid = GridSystem.Instance;
                destination.x = transform.position.x;
                destination.z = grid.Origin.z + grid.BaseCoreRow * grid.CellHeight;
            }

            Vector3 direction = destination - _firePoint.position;
            Quaternion rotation = direction.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direction)
                : _firePoint.rotation;

            // Prefab에 기반하여, 투사체 인스턴스를 발사 시작 위치에 생성
            Projectile projectile = Instantiate(_projectilePrefab, _firePoint.position, rotation);

            // 투사체 발사
            projectile.Launch(this, damageable, destination, _projectileSpeed, _projectileMovement);
        }
    }
}
