using System.Collections;
using UnityEngine;
using EnemySystem;

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

        private IEnumerator ApplyDamageAfterDelay(IDamageable enemy, float delay)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log($"{delay}초 경과 데미지 적용 완료.");

            if (enemy != null)
            {
                // 🌟 핵심: 공격력과 내 속성(myElement)을 묶어서 info 보따리로 만듭니다.
                DamageInfo info = DamageInfo.Default((float)attackDamage, 0f, myElement);
                
                // 🌟 보따리를 통째로 던집니다! (기존의 (int)attackDamage 이런 거 절대 쓰면 안 됩니다!)
                enemy.TakeDamage(info); 
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