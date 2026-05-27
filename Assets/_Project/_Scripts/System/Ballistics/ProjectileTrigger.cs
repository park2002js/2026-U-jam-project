using UnityEngine;
using EnemySystem;

namespace Ballistics
{
    // 물리 투사체의 껍데기에 부착되어 충돌(Trigger) 판정만 전담하는 리포터 클래스
    public class ProjectileTrigger : MonoBehaviour
    {
        [HideInInspector] public float damage; // ProjectileBehaviour가 생성 직후 주입해 줄 데미지
        public Element element;
        private void OnTriggerEnter(Collider other)
        {
            // 발사하자마자 플레이어 자신의 콜라이더에 맞는 것을 방지
            if (other.CompareTag("Player")) return;

            Enemy targetEnemy = other.GetComponentInParent<Enemy>();
            if (targetEnemy != null)
            {
                DamageInfo info = DamageInfo.Default(damage, 0f, element);
                info.Instigator = gameObject;
                DamageSystem.ApplyDamage(targetEnemy.gameObject, info);
            }
            
            Debug.Log($"[Projectile] 적중! 대상: {other.name}, 데미지: {(int)damage}");

            // 타격 후 투사체 파괴
            Destroy(gameObject);
        }
    }
}