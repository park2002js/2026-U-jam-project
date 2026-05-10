using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
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

        // 레이어 이름 확인
        string layerName = LayerMask.LayerToName(other.gameObject.layer);

        // Enemy 레이어는 무시하고 통과
        if (layerName == "Enemy") return;

        // 공격 대상 레이어 확인
        if (layerName == "Player" || layerName == "Base" || other.CompareTag("Decoy"))
        {
            isHit = true;

            // 데미지 전달
            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            Debug.Log($"<color=orange>[Projectile]</color> {other.name} 적중!");
            Destroy(gameObject);
        }
    }
}