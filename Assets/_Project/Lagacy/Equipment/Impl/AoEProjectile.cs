using UnityEngine;
using EnemySystem;

public class AoEProjectile : MonoBehaviour
{
    [HideInInspector] public float damage = 30f; // 무기에서 전달받을 데미지
    
    public float explosionRadius = 1f; // 요구사항: 반경 1

    private bool isExploded = false;

    // 트리거(isTrigger = true)로 설정되어 있을 경우
    private void OnTriggerEnter(Collider other)
    {
        if (isExploded) return;

        // 부딪힌 대상의 레이어가 "Ground"인지 확인
        if (LayerMask.LayerToName(other.gameObject.layer) == "Ground")
        {
            Debug.Log("바닥에 닿음");
            Explode();
        }
    }

    // 일반 물리 충돌(isTrigger = false)로 설정되어 있을 경우
    private void OnCollisionEnter(Collision collision)
    {
        if (isExploded) return;

        // 부딪힌 대상의 레이어가 "Ground"인지 확인
        if (LayerMask.LayerToName(collision.gameObject.layer) == "Ground")
        {
            Explode();
        }
    }

    private void Explode()
    {
        isExploded = true;

        // 1. 시각적 원(반경 1) 바닥에 그리기
        DrawExplosionCircle();

        // 2. 폭발 반경(explosionRadius) 내의 모든 콜라이더를 가져옴
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        
        foreach (Collider hit in hits)
        {
            // 3. 요구사항: "Enemy" 태그가 붙어있는지 확인
            if (hit.CompareTag("Enemy"))
            {
                // 적의 본체(Enemy 스크립트)를 찾아서 데미지 전달
                Enemy targetEnemy = hit.GetComponentInParent<Enemy>();
                if (targetEnemy != null)
                {
                    // 기존 범위 공격 시스템에 전달할 피해 정보
                    LegacyDamageInfo info = LegacyDamageInfo.Default(damage);
                    DamageSystem.ApplyDamage(targetEnemy.gameObject, info);
                    Debug.Log($"[AoE 폭발] {targetEnemy.name}에게 {damage} 범위 피해 적중!");
                }
            }
        }

        // 폭발 처리가 끝났으므로 투사체 자신은 즉시 파괴
        Destroy(gameObject);
    }

    // 코드로 즉석에서 빨간색 테두리 원(시각적 효과)을 그리는 함수
    private void DrawExplosionCircle()
    {
        GameObject circleObj = new GameObject("ExplosionVisual");
        circleObj.transform.position = transform.position;
        
        LineRenderer line = circleObj.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        
        // MVP 시연용 심플한 빨간색 메테리얼 설정
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.blue;
        line.endColor = Color.yellow;
        
        int segments = 36;
        line.positionCount = segments + 1;
        
        float angle = 0f;
        for (int i = 0; i < (segments + 1); i++)
        {
            // 반경(explosionRadius) 1에 맞게 x, z 좌표 계산
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * explosionRadius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * explosionRadius;
            
            // 바닥(Ground)에 파묻히지 않도록 y축을 살짝 띄움(0.05f)
            line.SetPosition(i, new Vector3(x, 0.1f, z)); 
            angle += (360f / segments);
        }

        // 0.5초 뒤에 시각적으로 그려진 원(폭발 자국) 자동 삭제
        Destroy(circleObj, 0.5f);
    }
}
