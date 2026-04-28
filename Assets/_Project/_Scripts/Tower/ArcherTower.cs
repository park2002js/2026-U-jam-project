using System.Collections;
using UnityEngine;

public class ArcherTower : DefenseBuilding
{
    [Header("Archer Tower Settings")]
    public float attackRange = 5f;
    public float fireRate = 1f;
    public float attackDamage = 10f;
    public float damageDelay = 0.5f; 
    
    public GameObject projectilePrefab;
    public Transform firePoint;
    public LayerMask enemyLayer;

    [Header("Debug / Test")]
    public bool isBattlePhase = true;

    private float lastFireTime = 0f;
    private Transform currentTarget;

    void Update()
    {
        if (isBattlePhase)
        {
            FindClosestEnemy();

            if (currentTarget != null)
            {
                transform.LookAt(currentTarget);

                if (Time.time >= lastFireTime + fireRate)
                {
                    Shoot();
                }
            }
        }
    }

    private void FindClosestEnemy()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (Collider enemy in enemiesInRange)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy.transform;
            }
        }

        if (currentTarget != nearestEnemy)
        {
            // 1. 타겟이 없었는데 새로 발견했을 때
            if (currentTarget == null && nearestEnemy != null)
            {
                Debug.Log($" 새로운 타겟 포착: '{nearestEnemy.name}'");
            }
            // 2. 타겟이 있었는데 사라졌을 때 (죽었거나 사거리 밖으로 도망침)
            else if (currentTarget != null && nearestEnemy == null)
            {
                Debug.Log($"타겟 상실 (처치됨 또는 사거리 이탈)");
            }
            // 3. 둘 다 있는데 더 가까운 다른 타겟으로 변경되었을 때
            else if (currentTarget != null && nearestEnemy != null)
            {
                Debug.Log($"타겟 변경: '{currentTarget.name}' -> '{nearestEnemy.name}'");
            }
        }
        currentTarget = nearestEnemy;
    }

    private void Shoot()
    {
        if (projectilePrefab != null && firePoint != null && currentTarget != null)
        {
            Collider targetCollider = currentTarget.GetComponent<Collider>();
            Vector3 targetCenter = targetCollider != null ? targetCollider.bounds.center : currentTarget.position;
            Vector3 direction = (targetCenter - firePoint.position).normalized;
            
            // ✨ 1. 타워와 적 사이의 거리를 계산합니다.
            float distance = Vector3.Distance(firePoint.position, targetCenter);
            Debug.DrawRay(firePoint.position, direction * attackRange, Color.red, 2f);

            if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, attackRange, enemyLayer))
            {
                Enemy targetEnemy = hit.collider.GetComponent<Enemy>();
                
                if (targetEnemy != null)
                {
                    Debug.Log($"'{targetEnemy.name}' 공격, ({damageDelay}초 후 명중 예정)");
                    GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                    MeshProjectile projectileScript = projectileObj.GetComponent<MeshProjectile>();

                    if (projectileScript != null)
                    {
                        projectileScript.SetTarget(currentTarget);
                        
                        // ✨ 2. 속력 = 거리 / 시간 공식을 적용하여 투사체의 속도를 자동 조절합니다.
                        // (만약 damageDelay가 0이라면 에러가 나므로, 0보다 클 때만 적용하도록 안전장치 추가)
                        if (damageDelay > 0)
                        {
                            projectileScript.speed = distance / damageDelay;
                        }
                    }

                    StartCoroutine(ApplyDamageAfterDelay(targetEnemy, damageDelay));
                }
            }
            
            lastFireTime = Time.time;
        }
    }

    private IEnumerator ApplyDamageAfterDelay(Enemy enemy, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (enemy != null)
        {
            enemy.takeDamage(attackDamage);
            Debug.Log($"{delay}초 경과 데미지 적용 완료.");
        }
    }

    public override string GetUpgradeDescription()
    {
        return "사거리 및 공격 속도 증가";
    }

    public override int GetUpgradeCost()
    {
        return 50 + (currentLevel * 20);
    }

    public override void ApplyUpgrade()
    {
        attackRange += 1f;
        fireRate = Mathf.Max(0.1f, fireRate - 0.1f);
        currentLevel++;
    }
}