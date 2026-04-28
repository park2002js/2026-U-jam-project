using UnityEngine;

namespace Ballistics
{
    // 분할 레이캐스트 방식으로 매 프레임 이동과 충돌을 직접 계산하는 투사체
    public class SegmentProjectile : MonoBehaviour
    {
        private Vector3 direction;
        private float speed;
        private float damage;
        private float lifeTime = 5f; // 허공으로 날아갔을 때 파괴될 시간

        // 투사체 생성 직후 데이터를 주입받는 초기화 함수
        public void Init(Vector3 dir, float spd, float dmg)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;
            
            // 메모리 누수 방지
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            // 이번 프레임에 날아갈 거리 (속력 * 시간)
            float distanceThisFrame = speed * Time.deltaTime;

            // 현재 위치에서 다음 위치로 이동하기 전에, 그 사이 궤적에 장애물이 있는지 Raycast로 확인
            // QueryTriggerInteraction.Ignore를 사용하여 적의 감지 구체(Sensor)를 유령처럼 통과하도록 처리
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distanceThisFrame, ~0, QueryTriggerInteraction.Ignore))
            {
                // 플레이어 본인을 맞춘 게 아니라면 타격 처리
                if (!hit.collider.CompareTag("Player"))
                {
                    // 대상에게 데미지 전달
                    hit.collider.SendMessage("TakeDamage", (int)damage, SendMessageOptions.DontRequireReceiver);
                    Debug.Log($"[Segment] 적중! 대상: {hit.collider.name}, 데미지: {(int)damage}");
                    
                    // 타격 완료 후 투사체 파괴
                    Destroy(gameObject);
                    return; // 파괴되었으므로 더 이상 이동하지 않음
                }
            }

            // 아무것도 맞지 않았거나 무시할 대상(플레이어)이었다면 실제 위치를 전진시킴
            transform.position += direction * distanceThisFrame;
        }
    }
}