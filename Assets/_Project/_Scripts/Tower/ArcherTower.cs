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

                // 타겟이 있고 쿨타임이 지났을 때 사격 (타워 회전 로직 제거)
                if (currentTarget != null && Time.time >= lastFireTime + fireRate)
                {
                    Shoot();
                }
            }
        }

        private void FindClosestEnemy()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
            float shortestDistance = Mathf.Infinity;
            Transform nearestEnemy = null;

            foreach (var hitCollider in hitColliders)
            {
                Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    float distanceToEnemy = Vector3.Distance(transform.position, hitCollider.transform.position);
                    
                    // 3. 가장 가까운 적을 타겟으로 갱신
                    if (distanceToEnemy < shortestDistance)
                    {
                        shortestDistance = distanceToEnemy;
                        nearestEnemy = enemy.transform;
                    }
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
            if (projectilePrefab == null || firePoint == null || currentTarget == null) return;


            // 1. 타겟의 정확한 중심점(Collider Center) 계산
            Collider targetCollider = currentTarget.GetComponent<Collider>();
            Vector3 targetCenter = targetCollider != null ? targetCollider.bounds.center : currentTarget.position;
            
            // 2. 사격 방향 및 거리 계산
            Vector3 direction = (targetCenter - firePoint.position).normalized;
            float distance = Vector3.Distance(firePoint.position, targetCenter);
            Debug.DrawRay(firePoint.position, direction * attackRange, Color.red, 2f);

            // 3. 투사체 생성 (타겟 방향을 바라보도록 회전 설정)
            GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
            MeshProjectile projectileScript = projectileObj.GetComponent<MeshProjectile>();

            // 4. 투사체 스크립트 설정 (명중 시간 예측 속도 적용)
            if (projectileScript != null)
            {
                projectileScript.SetTarget(currentTarget);
                if (damageDelay > 0)
                {
                    projectileScript.speed = distance / damageDelay;
                }
            }

            // 5. [핵심] 타워에서 투사체의 수명 주기(데미지 + 파괴)를 직접 관리
            StartCoroutine(ApplyDamageAndCleanup(currentTarget.GetComponent<Enemy>(), projectileObj, damageDelay));
            
            lastFireTime = Time.time;

        }

        private IEnumerator ApplyDamageAndCleanup(Enemy enemy, GameObject projectile, float delay)
        {
            // 투사체가 날아가는 시간만큼 대기
            yield return new WaitForSeconds(delay);

            // 적이 살아있다면 데미지 적용
            if (enemy != null)
            {
                enemy.TakeDamage((int)attackDamage);
                Debug.Log($"[ArcherTower] {enemy.name}에게 {attackDamage} 데미지 적용.");
            }

            // [핵심] 타워가 생성한 투사체를 직접 파괴하여 정리
            if (projectile != null)
            {
                Destroy(projectile);
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