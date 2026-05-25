using UnityEngine;

public class TurretTargeting : MonoBehaviour
{
    [Header("포탑 설정")]
    [Tooltip("포탑의 공격 사거리")]
    public float attackRange = 5f;
    
    [Tooltip("1초당 공격 횟수 (예: 2 = 1초에 2번 공격)")]
    public float attackRate = 1f;

    [Tooltip("적을 식별할 레이어")]
    public LayerMask enemyLayer;

    private Transform currentTarget; // 현재 타겟팅된 적
    private float nextAttackTime = 0f;

    void Start()
    {
        // 매 프레임 탐색은 성능 저하를 유발하므로 0.2초마다 탐색하도록 설정
        InvokeRepeating("UpdateTarget", 0f, 0.2f);
    }

    void Update()
    {
        // 타겟이 없으면 대기
        if (currentTarget == null) return;

        // 공격 쿨타임 확인
        if (Time.time >= nextAttackTime)
        {
            Attack();
            // 다음 공격 가능 시간 계산
            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    // 사거리 내 가장 가까운 적을 찾는 핵심 로직
    void UpdateTarget()
    {
        // 1. 사거리 반경 내에 있는 'enemyLayer'에 해당하는 모든 오브젝트 감지
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        // 2. 감지된 적들을 하나씩 확인하며 거리를 비교
        foreach (Collider enemy in enemiesInRange)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);

            // 3. 기존에 찾은 적보다 더 가까우면 타겟 갱신
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy.transform;
            }
        }

        // 4. 가장 가까운 적이 존재하고, 사거리 내에 있다면 현재 타겟으로 지정
        if (nearestEnemy != null && shortestDistance <= attackRange)
        {
            currentTarget = nearestEnemy;
        }
        else
        {
            // 사거리를 벗어났거나 적이 없으면 타겟팅 해제
            currentTarget = null;
        }
    }

    void Attack()
    {
        // 실시간 공격 확인용 Debug.Log
        float currentDistance = Vector3.Distance(transform.position, currentTarget.position);
        Debug.Log($"[공격!] {gameObject.name}이(가) {currentTarget.name}을(를) 공격 중! (거리: {currentDistance:F2})");
    }

    // Unity 에디터에서 사거리를 시각적으로 보기 위한 기즈모 (빨간색 원)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}