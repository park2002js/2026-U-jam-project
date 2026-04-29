using System.Collections;
using UnityEngine;

namespace Defense
{
    public class ArcherTower : Turret
    {
        [Header("Archer Tower Specific")]
        public float damageDelay = 0.5f; 
        public GameObject projectilePrefab;
        public Transform firePoint;

        // 부모 클래스의 추상 함수인 Shoot을 이 타워만의 방식으로 구체화합니다.
        protected override void Shoot()
        {
            if (projectilePrefab != null && firePoint != null && currentTarget != null)
            {
                Collider targetCollider = currentTarget.GetComponent<Collider>();
                Vector3 targetCenter = targetCollider != null ? targetCollider.bounds.center : currentTarget.position;
                Vector3 direction = (targetCenter - firePoint.position).normalized;
                
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
                            
                            if (damageDelay > 0)
                            {
                                projectileScript.speed = distance / damageDelay;
                            }
                        }

                        // attackDamage는 부모(Turret)에서 상속받은 변수를 그대로 사용합니다.
                        StartCoroutine(ApplyDamageAfterDelay(targetEnemy, damageDelay));
                    }
                }
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

        // --- 할아버지(DefenseBuilding)에게서 물려받은 업그레이드 로직 ---
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