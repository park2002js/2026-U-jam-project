using UnityEngine;

namespace Ballistics
{
    // 물리 엔진(Rigidbody)을 활용하여 실제 맵을 날아가는 투사체 로직
    public class ProjectileBehaviour : IBallisticsBehaviour
    {
        private float lifeTime = 5f; // 허공으로 날아갔을 때 메모리 누수를 막기 위한 최대 생존 시간

        public void Execute(Transform firePoint, Vector3 direction, float damage, float projectileSpeed, GameObject projectilePrefab, Element element = null)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[ProjectileBehaviour] 투사체 프리팹이 할당되지 않았습니다!");
                return;
            }

            // 1. 총구 위치와 조준 방향에 맞춰 투사체 생성
            GameObject projectile = Object.Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

            // 2. 콜라이더 트리거 세팅 (플레이어 밀림 현상 방지)
            Collider col = projectile.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true; 
            }

            // 3. 물리 엔진(Rigidbody) 세팅
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = projectile.AddComponent<Rigidbody>();
            }

            
            
            rb.useGravity = false; // 기획서: "낙차 없이 날아감"
            rb.isKinematic = false; // 물리 연산을 통해 날아가야 하므로 false
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 총알이 너무 빨라 벽을 뚫고 지나가는 버그 방지

            // 4. 충돌 감지 리포터 부착 및 데미지 주입
            ProjectileTrigger reporter = projectile.GetComponent<ProjectileTrigger>();
            if (reporter == null)
            {
                reporter = projectile.AddComponent<ProjectileTrigger>();
            }
            reporter.damage = damage; // 무기의 공격력을 투사체에 전달
            reporter.element = element;

            // 5. 물리적인 힘(속도)을 가하여 발사 (유니티 최신 linearVelocity 사용)
            rb.linearVelocity = direction * projectileSpeed;

            // 6. 아무것도 맞추지 못했을 경우 일정 시간 뒤 자동 파괴
            Object.Destroy(projectile, lifeTime);
        }
    }
}