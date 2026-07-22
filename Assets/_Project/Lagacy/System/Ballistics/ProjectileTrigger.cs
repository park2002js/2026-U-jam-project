using UnityEngine;
using EnemySystem;

namespace Ballistics
{
    // 물리 투사체의 껍데기에 부착되어 충돌(Trigger) 판정만 전담하는 리포터 클래스
    public class ProjectileTrigger : MonoBehaviour
    {
        [HideInInspector] public float damage; // ProjectileBehaviour가 생성 직후 주입해 줄 데미지
        public Element element;
        [Header("MVP AoE 폭발 반경")]
        public float explosionRadius = 5.0f; // 폭발 반경 (에디터나 코드로 조절 가능)
        
        private bool isExploded = false;

        private void OnTriggerEnter(Collider other)
        {
            if (isExploded) return;
            
            // 발사하자마자 플레이어 자신이나 총구 등에 맞는 것을 방지
            if (other.CompareTag("Player") || other.CompareTag("Weapon")) return;

            // 바닥, 벽, 적 등 '아무거나' 닿는 순간 폭발!
            isExploded = true;

            // 1. 탄착군 주변 폭발 반경 내의 모든 'Enemy' 콜라이더 탐색
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Enemy"));
            
            foreach (Collider hit in hits)
            {
                // 적의 팔/다리 같은 자식 콜라이더에 맞았을 수도 있으므로 최상위 부모의 Enemy를 찾음
                Enemy targetEnemy = hit.GetComponentInParent<Enemy>();
                if (targetEnemy != null)
                {
                    // 2. 데미지와 '속성(element)' 데이터를 온전히 포장
                    // 기존 투사체 시스템에 전달할 피해 정보
                    LegacyDamageInfo info = LegacyDamageInfo.Default(damage, 0f, element);
                    info.Instigator = gameObject;
                    
                    // 3. 중앙 통제 시스템으로 광역 데미지+속성 전달
                    DamageSystem.ApplyDamage(targetEnemy.gameObject, info);
                }
            }
            
            Debug.Log($"[AoE Projectile] 쾅! {hits.Length}명의 적에게 범위 피해+속성 부여 완료.");
            
            // 폭발 후 투사체 파괴 (이펙트가 있다면 이 줄 바로 위에 추가하세요)
            Destroy(gameObject);
        }
    }
}
