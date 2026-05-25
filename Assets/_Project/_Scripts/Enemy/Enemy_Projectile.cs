using UnityEngine;

public class Enemy_Projectile : MonoBehaviour
{
    public float speed;
    private Transform target;
    private int damage;
    private bool isHit = false;

    public void Launch(Transform target, int damage)
    {
        this.target = target;
        this.damage = damage;

        if (target != null)
        {
            transform.forward = (target.position - transform.position).normalized;
        }

        // 5초 뒤 자동 소멸
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        if (isHit) return;

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.forward = dir;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHit) return;

        // 1. 부딪힌 물체의 레이어 이름을 가져옵니다.
        string layerName = LayerMask.LayerToName(other.gameObject.layer);

        // 2. [핵심] Enemy 레이어라면 무시하고 그냥 통과합니다 (팀킬 방지).
        if (layerName == "Enemy")
        {
            return;
        }

        // 3. Enemy를 제외한 '그 어떤 것'과 부딪혀도 무조건 충돌 처리 및 파괴
        isHit = true;

        // 데미지를 줄 수 있는 대상(Player, Base 등)에게만 데미지 전달
        // (바닥이나 벽은 이 함수가 없어도 에러가 나지 않도록 DontRequireReceiver 옵션 유지)
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        // 디버깅을 위해 누구와 부딪혀서 소멸했는지 로그 출력
        Debug.Log($"<color=orange>[Projectile]</color> Enemy가 아닌 다른 물체({other.name} / Layer: {layerName})와 충돌하여 소멸!");

        // 투사체 삭제
        Destroy(gameObject);
    }
}