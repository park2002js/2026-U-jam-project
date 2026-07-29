using UJam.Runtime.Combat;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public class RangedEnemy : EnemyBase
    {
        // 원거리 Enemy가 발사할 투사체 Prefab
        [SerializeField] private EnemyProjectile _projectilePrefab;

        // 투사체를 생성할 위치
        [SerializeField] private Transform _firePoint;

        // 투사체가 목표까지 이동할 속도
        [SerializeField, Min(0.01f)] private float _projectileSpeed = 10f;

        // 마지막으로 투사체를 발사한 시각
        private float _lastShot = float.NegativeInfinity;

        // 원거리 Enemy 기본 스탯 생성
        protected override EnemyStatus MakeStatus()
        {
            // 발표용 원거리 Enemy 스탯 생성
            EnemyStatus status = new EnemyStatus(
                80f,
                3f,
                8f,
                1f,
                10f,
                false,
                false,
                false,
                "Enemy",
                "Ranged",
                "Physical");

            // 원거리 Enemy 초기 스탯 반환
            return status;
        }

        // 별도 Spawn 연출 전 즉시 Move 진입
        protected override void OnSpawn()
        {
            // Spawn 완료와 Move 전환
            SpawnDone();
        }

        // 현재 Target을 향한 투사체 발사
        protected override void OnAttack(GameObject target, Vector3 attackPoint)
        {
            // Target과 Cooldown 확인
            if (target == null || Time.time < _lastShot + Status.Cooldown)
            {
                // 발사할 수 없는 공격 종료
                return;
            }

            // 투사체 Prefab 누락을 Console에 표시
            if (_projectilePrefab == null)
            {
                Debug.LogWarning(
                    $"[RangedEnemy] {gameObject.name}의 Projectile Prefab 연결 필요",
                    gameObject);

                // 투사체 없는 공격 종료
                return;
            }

            // FirePoint가 없으면 Enemy 루트를 임시 발사 위치로 사용
            Transform firePoint = _firePoint != null ? _firePoint : transform;

            // 공격 Animation이 준비되면 투사체 생성 전에 재생할 자리

            // 발사 위치에 원거리 투사체 생성
            EnemyProjectile projectile = Instantiate(
                _projectilePrefab,
                firePoint.position,
                firePoint.rotation);

            // 투사체 생성 실패 차단
            if (projectile == null)
            {
                // 실패한 발사 종료
                return;
            }

            // Enemy 공격 정보를 투사체에 전달
            DamageInfo damageInfo = new DamageInfo(
                Status.Damage,
                gameObject.name,
                DamageSourceKind.Enemy);
            projectile.Initialize(target, attackPoint, damageInfo, _projectileSpeed);
            _lastShot = Time.time;

            // 발표용 공격 발사 성공 확인
            Debug.Log(
                $"[RangedEnemy] {gameObject.name} 투사체 발사 → {attackPoint}",
                gameObject);

            // 투사체 발사 뒤 Status.Cooldown 동안 Attack 상태를 유지하는 연출을 넣을 자리
        }
    }
}
