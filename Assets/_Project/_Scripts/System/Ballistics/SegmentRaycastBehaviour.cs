using UnityEngine;

namespace Ballistics
{
    // 중앙 통제형 일반 투사체 발사 로직
    public class SegmentRaycastBehaviour : IBallisticsBehaviour
    {
        public void Execute(Transform firePoint, Vector3 direction, float damage, float projectileSpeed, GameObject projectilePrefab, Element element = null)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[SegmentRaycast] 투사체 프리팹이 할당되지 않았습니다!");
                return;
            }

            // 1. 총구 위치와 조준 방향에 맞춰 투사체 생성
            GameObject projectile = Object.Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

            // 2. 투사체 이동 및 충돌을 계산할 SegmentProjectile 스크립트 확인 및 부착
            SegmentProjectile segment = projectile.GetComponent<SegmentProjectile>();
            if (segment == null)
            {
                segment = projectile.AddComponent<SegmentProjectile>();
            }

            // 3. 방향, 속도, 데미지 데이터를 넘겨주어 발사 시작
            segment.Init(direction, projectileSpeed, damage, element);
        }
    }
}