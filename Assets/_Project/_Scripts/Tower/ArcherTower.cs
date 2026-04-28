using UnityEngine;

public class ArcherTower : DefenseBuilding
{
    [Header("Archer Tower Settings")]
    public float attackRange = 5f;
    public float fireRate = 1f;
    public float attackDamage = 10f; // ✨ 추가: 타워의 기본 공격력 설정
    public GameObject projectilePrefab;
    public Transform firePoint;
    public LayerMask enemyLayer;

    [Header("Debug / Test")]
    public bool isBattlePhase = true; // PhaseManager 구현 전까지 사용할 임시 변수

    private float lastFireTime = 0f;
    private Transform currentTarget;

    void Update()
    {
        if (isBattlePhase)
        {
            FindClosestEnemy();

            if (currentTarget != null && Time.time >= lastFireTime + fireRate)
            {
                Shoot();
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

        currentTarget = nearestEnemy;
    }

    private void Shoot()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            
            MeshProjectile projectileScript = projectileObj.GetComponent<MeshProjectile>();
            if (projectileScript != null)
            {
                projectileScript.SetElement(myElement);
                projectileScript.SetTarget(currentTarget);
                projectileScript.SetDamage(attackDamage); // ✨ 추가: 생성된 투사체에 공격력 전달
            }
            Debug.Log($"[아처 타워] '{currentTarget.name}'을(를) 향해 투사체 발사 완료!");
            lastFireTime = Time.time;
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