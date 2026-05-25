using UnityEngine;
using EnemySystem;

public class ChainLightning : MonoBehaviour
{
    public float damage = 20f;         // 번개 데미지
    public float bounceRadius = 5f;    // 튕기는 범위
    public int maxBounces = 3;         // 최대 연쇄 횟수
    
    [Header("불+라이트닝 전용 세팅")]
    public bool leaveDoT = false;      // 도트딜을 남길 것인가?
    public float dotDamage = 5f;       // 초당 도트딜 수치

    void Start()
    {
        // 1. 내 주변(bounceRadius)의 적들을 찾습니다.
        Collider[] hits = Physics.OverlapSphere(transform.position, bounceRadius);
        int bounceCount = 0;

        foreach (var hit in hits)
        {
            if (bounceCount >= maxBounces) break;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 2. 번개 데미지 쾅!
                enemy.TakeDamage(damage);

                // 3. (불+라이트닝 전용) 도트딜 디버프 남기기
                if (leaveDoT)
                {
                    enemy.AddEffect(new UniversalStatus("감전 화상", 3f, StatType.MoveSpeed, 0, dotDamage));
                }
                bounceCount++;
            }
        }
        
        // 연쇄가 끝났으면 자신(프리팹)은 파괴됩니다.
        Destroy(gameObject, 0.1f);
    }
}