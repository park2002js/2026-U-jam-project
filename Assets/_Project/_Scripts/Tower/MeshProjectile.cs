using UnityEngine;

namespace Defense
{
    public class MeshProjectile : MonoBehaviour
    {
        // 타워 스크립트에서 거속시 공식을 통해 이 속도를 강제로 덮어씌웁니다.
        [HideInInspector] 
        public float speed = 10f; 

        private Transform target;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            
            // 안전장치: 혹시라도 적이 죽어서 투사체가 허공에 영원히 날아가는 것을 방지 (5초 뒤 무조건 삭제)
            Destroy(gameObject, 5f); 
        }

        void Update()
        {
            // 날아가는 도중 타겟이 이미 죽어서 사라졌다면 투사체도 즉시 삭제
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            // 타겟의 몸통 정중앙(Bounds.center) 좌표 가져오기
            Collider targetCollider = target.GetComponent<Collider>();
            Vector3 targetCenter = targetCollider != null ? targetCollider.bounds.center : target.position;

            // 타겟을 향해 이동
            Vector3 direction = (targetCenter - transform.position).normalized;
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
            
            // 화살촉이 날아가는 방향을 바라보도록 회전
            transform.LookAt(targetCenter);

            // 타겟의 중심에 거의 도달하면 (시각적으로 맞은 것처럼 보이면) 투사체 삭제
            if (Vector3.Distance(transform.position, targetCenter) < 0.2f)
            {
                Destroy(gameObject);
            }
        }
    }
}