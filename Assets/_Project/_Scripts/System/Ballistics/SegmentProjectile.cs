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
        private bool isInitialized = false;

        // 투사체 생성 직후 데이터를 주입받는 초기화 함수
        public void Init(Vector3 dir, float spd, float dmg)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;
            isInitialized = true;
            
            // 메모리 누수 방지
            Destroy(gameObject, lifeTime);

            // 생성 직후, 이미 적의 몸통(Collider) 내부에 파고들어 스폰되었는지 1차 검사
            CheckPointBlankHit();
        }

        private void CheckPointBlankHit()
        {
            // 총알 위치에 아주 작은 구체를 만들어 즉각적인 겹침 판정 수행 (트리거 무시)
            Collider[] overlaps = Physics.OverlapSphere(transform.position, 0.1f, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider col in overlaps)
            {
                if (!col.CompareTag("Player"))
                {
                    // SendMessageUpwards로 변경하여 적의 자식 객체에 맞아도 본체로 데미지가 올라가도록 처리
                    col.SendMessageUpwards("TakeDamage", (int)damage, SendMessageOptions.DontRequireReceiver);
                    Debug.Log($"[Segment 초근접] 적중! 대상: {col.name}, 데미지: {(int)damage}");
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private void Update()
        {
            if (!isInitialized) return;
            // 이번 프레임에 날아갈 거리 (속력 * 시간)
            float distanceThisFrame = speed * Time.deltaTime;

            // 2차 검사: 날아가면서 Raycast로 궤적 검사
            // 현재 위치에서 다음 위치로 이동하기 전에, 그 사이 궤적에 장애물이 있는지 Raycast로 확인
            // QueryTriggerInteraction.Ignore를 사용하여 적의 감지 구체(Sensor)를 유령처럼 통과하도록 처리
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distanceThisFrame, ~0, QueryTriggerInteraction.Ignore))
            {
                // 플레이어 본인을 맞춘 게 아니라면 타격 처리
                if (!hit.collider.CompareTag("Player"))
                {
                    // 대상에게 데미지 전달
                    hit.collider.SendMessageUpwards("TakeDamage", (int)damage, SendMessageOptions.DontRequireReceiver);
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