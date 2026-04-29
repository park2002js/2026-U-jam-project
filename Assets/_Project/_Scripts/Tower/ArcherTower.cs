using System.Collections;
using UnityEngine;
using EnemySystem;

namespace Building
{
    
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
        private bool hasTarget = false;

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

            bool foundNew = nearestEnemy != null;

            // ✨ 탐지 상태 변경 로직 수정 (적 파괴를 완벽하게 감지합니다)
            if (hasTarget && !foundNew)
            {
                // 방금 전까지 적이 있었는데, 파괴되어서 주변에 아무도 안 남았을 때
                Debug.Log("타겟 처치 완료! 새로운 적이 나타날 때까지 탐색을 대기합니다.");
            }
            else if (!hasTarget && foundNew)
            {
                // 아무도 없다가 새로운 적이 나타났을 때
                Debug.Log($" 새로운 타겟 포착: '{nearestEnemy.name}'");
            }
            else if (hasTarget && foundNew && currentTarget != nearestEnemy)
            {
                // 기존 적이 파괴되자마자 다른 적을 찾았거나, 더 가까운 적이 나타났을 때
                Debug.Log($"타겟 파괴(또는 변경)됨! 다음 타겟: '{nearestEnemy.name}'");
            }

            hasTarget = foundNew;
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
                enemy.TakeDamage((int)attackDamage);
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
}