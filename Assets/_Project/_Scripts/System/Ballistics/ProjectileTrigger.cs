using UnityEngine;

namespace Ballistics
{
    // 물리 투사체의 껍데기에 부착되어 충돌(Trigger) 판정만 전담하는 리포터 클래스
    public class ProjectileTrigger : MonoBehaviour
    {
        [HideInInspector] public float damage; // ProjectileBehaviour가 생성 직후 주입해 줄 데미지

        private void OnTriggerEnter(Collider other)
        {
            // 발사하자마자 플레이어 자신의 콜라이더에 맞는 것을 방지
            if (other.CompareTag("Player")) return;

            // 맞은 객체에게 데미지 전달 (Hitscan과 동일한 규격)
            other.SendMessage("TakeDamage", (int)damage, SendMessageOptions.DontRequireReceiver);
            
            Debug.Log($"[Projectile] 적중! 대상: {other.name}, 데미지: {(int)damage}");

            // 타격 후 투사체 파괴
            Destroy(gameObject);
        }
    }
}