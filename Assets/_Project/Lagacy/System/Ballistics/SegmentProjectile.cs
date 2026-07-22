using UnityEngine;
using EnemySystem;

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
        private Element element;

        // 투사체 생성 직후 데이터를 주입받는 초기화 함수
        public void Init(Vector3 dir, float spd, float dmg, Element element)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;
            isInitialized = true;
            this.element = element;
            
            // 메모리 누수 방지
            Destroy(gameObject, lifeTime);

            // 생성 직후, 이미 적의 몸통(Collider) 내부에 파고들어 스폰되었는지 1차 검사
            CheckPointBlankHit();
        }

        // [추가됨] 데미지와 속성을 안전하게 전달하는 공통 함수
        private void ApplyDamageToTarget(Collider targetCol)
        {
            // 자식 콜라이더(팔, 다리)에 맞았을 경우를 대비해 최상위 부모의 Enemy 컴포넌트를 찾음
            Enemy targetEnemy = targetCol.GetComponentInParent<Enemy>();
            
            if (targetEnemy != null)
            {
                if(HitFeedbackUI.Instance != null) HitFeedbackUI.Instance.ShowHitmarker();
                // 타워와 동일하게 기존 시스템 전용 피해 정보에 데미지와 속성을 담아 포장
                LegacyDamageInfo info = LegacyDamageInfo.Default(damage, 0f, element);
                info.Instigator = this.gameObject;
                
                // DamageSystem을 통해 전달 (알아서 ElementReceiver로 넘어감)
                DamageSystem.ApplyDamage(targetEnemy.gameObject, info);
                Debug.Log($"[Segment] 적중! 대상: {targetEnemy.name}, 속성 적용됨");
            }
        }

        private void CheckPointBlankHit()
        {
            // 총알 위치에 아주 작은 구체를 만들어 즉각적인 겹침 판정 수행 (트리거 무시)
            Collider[] overlaps = Physics.OverlapSphere(transform.position, 0.1f, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider col in overlaps)
            {
                if (!col.CompareTag("Player"))
                {
                    ApplyDamageToTarget(col); // [수정됨] SendMessage 대체
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
                    ApplyDamageToTarget(hit.collider); // [수정됨] SendMessage 대체
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
