using UnityEngine;
using EnemySystem; // Enemy가 있는 네임스페이스
using System.Collections;

public class ThunderCloudSkill : MonoBehaviour
{
    [Header("폭발 세팅")]
    public float explosionDelay = 3f;      // 지연 시간 (n초 뒤 폭발)
    public float explosionRadius = 4f;     // 폭발 데미지가 들어가는 실제 반경
    public float explosionDamage = 150f;   // 폭발 데미지

    [Header("시각적 장판 세팅")]
    // 🌟 상점 HoverIndicator의 그래픽(Mesh)을 담당하는 부분을 여기에 넣습니다.
    public Transform indicatorVisual;      

    void Start()
    {
        // 1. 강제 공중 부양: 고렘 머리 위(3미터 허공)에 무조건 띄워버림!
        transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);

        if (indicatorVisual != null)
        {
            // Y축(두께)은 그대로 두고, X축과 Z축(바닥 넓이)을 지름만큼 키웁니다.
            indicatorVisual.localScale = new Vector3(explosionRadius * 2, indicatorVisual.localScale.y, explosionRadius * 2);
        }
        else
        {
            transform.localScale = new Vector3(explosionRadius * 2, 1f, explosionRadius * 2);
        }

        // 3. 타이머 시작
        StartCoroutine(ExplodeRoutine());
    }

    IEnumerator ExplodeRoutine()
    {
        // n초 동안 대기 (장판이 바닥에 표시된 상태 유지)
        yield return new WaitForSeconds(explosionDelay);

        // 3. 폭발! 실제 데미지를 주는 논리적 범위 계산
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 💡 DamageInfo를 쓰기로 하셨으니 거기에 맞춰서 데미지를 줍니다!
                enemy.TakeDamage(new DamageInfo { Amount = explosionDamage, Element = ElementType.Lightning });
            }
        }

        Debug.Log($"<color=yellow>[낙뢰 폭발!]</color> 반경 {explosionRadius}에 데미지 발생!");
        
        // 폭발 후 장판(프리팹) 파괴
        Destroy(gameObject);
    }

    // 4. 유니티 에디터에서 범위를 미리 볼 수 있게 그려주는 보너스 코드
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // 반투명 노란색
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}