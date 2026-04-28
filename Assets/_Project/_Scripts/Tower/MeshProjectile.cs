using UnityEngine;

public class MeshProjectile : MonoBehaviour
{
    private Transform target;
    private DefenseBuilding.ElementType element; 
    private float damage; // 타워로부터 전달받을 데미지
    
    public float speed = 10f;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetElement(DefenseBuilding.ElementType newElement) 
    {
        element = newElement;
    }

    // ✨ 타워에서 데미지를 전달받는 함수
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        // 투사체가 적에게 도착한 순간
        if (direction.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(direction.normalized * distanceThisFrame, Space.World);
        transform.LookAt(target); 
    }

    private void HitTarget()
    {
        Enemy enemyScript = target.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            // ✨ 적의 takeDamage 함수 호출 및 데미지 적용
            enemyScript.takeDamage(damage); 
            Debug.Log($"[투사체] 명중! {target.name}에게 {damage}의 데미지를 입혔습니다.");
        }

        // 데미지를 입힌 후 투사체는 파괴됨
        Destroy(gameObject);
    }
}