using UnityEngine;

namespace Ballistics
{
    // 스나이퍼, 레이저 무기 등 발사 즉시 타격이 들어가는 로직
    public class HitscanBehaviour : IBallisticsBehaviour
    {
        // 탄환 최대 거리
        private float maxRange = 1000f;

        public void Execute(Transform firePoint, Vector3 direction, float damage, float projectileSpeed, GameObject projectilePrefab)
        {
            // 목적지 지점 저장
            Vector3 endPoint;

            // 1. 발사 즉시 Raycast를 쏘아 적중 여부 판별
            if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, maxRange))
            {
                endPoint = hit.point; // 맞은 지점

                // 맞은 객체에게 데미지 전달 (요청하신 대로 TakeDamage(int) 통일 규격 사용)
                // MVP 단계에서 가장 범용적으로 쓸 수 있는 SendMessage를 활용해 인터페이스 없이도 함수를 강제 호출합니다.
                hit.collider.SendMessage("TakeDamage", (int)damage, SendMessageOptions.DontRequireReceiver);
                
                Debug.Log($"[Hitscan] 적중! 대상: {hit.collider.name}, 데미지: {(int)damage}");
            }
            else
            {
                // 아무것도 맞지 않았다면 최대 사거리 허공을 끝점으로 설정
                endPoint = firePoint.position + direction * maxRange;
            }

            // 2. 투사체 시각화 (Projectile)
            if (projectilePrefab != null)
            {
                VisualizeHitscan(firePoint.position, endPoint, projectilePrefab);
            }
        }

        // TODO : 이 방식은 그냥 땜빵에 불과하므로, 나중에 탄도 궤적으로 쓸 Prefab을 할당하는 것으로 해결한다.
        // 레이저나 탄도 궤적처럼 보이게 껍데기 프리팹을 조작하는 함수
        private void VisualizeHitscan(Vector3 start, Vector3 end, GameObject prefab)
        {
            // 총구 위치에 프리팹 생성
            GameObject visual = Object.Instantiate(prefab, start, Quaternion.identity);
            
            // 프리팹을 시작점과 끝점의 정중앙에 위치시킴
            visual.transform.position = (start + end) / 2f;
            
            // 프리팹이 끝점을 바라보도록 회전
            visual.transform.rotation = Quaternion.LookRotation(end - start);
            
            // 거리를 계산하여 Z축(길이) 스케일을 쭈욱 늘림 (레이저 형태)
            float distance = Vector3.Distance(start, end);
            visual.transform.localScale = new Vector3(0.1f, 0.1f, distance); // X, Y는 얇게 유지

            // 즉발형이므로 잔상처럼 아주 잠깐(0.05초) 보였다가 스스로 삭제되도록 처리
            Object.Destroy(visual, 0.05f);
        }
    }
}