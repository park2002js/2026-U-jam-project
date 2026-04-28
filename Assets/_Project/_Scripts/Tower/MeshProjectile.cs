using UnityEngine;

public class MeshProjectile : MonoBehaviour
{
    private Transform target;
    public float speed = 10f;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void Update()
    {
        // 타겟이 죽어서 사라졌다면 투사체도 같이 파괴
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        // 타겟에 도달했을 때 (데미지는 타워가 주니까 여기선 파괴만 함)
        if (direction.magnitude <= distanceThisFrame)
        {
            Destroy(gameObject);
            return;
        }

        // 타겟 방향으로 부드럽게 이동
        transform.Translate(direction.normalized * distanceThisFrame, Space.World);
        transform.LookAt(target); 
    }
}