using UnityEngine;

namespace Defense
{
    // DefenseBuilding을 상속받아 포탑의 공통 기능을 정의합니다.
    public abstract class Turret : DefenseBuilding
    {
        [Header("Turret Base Settings")]
        public float attackRange = 5f;
        public float fireRate = 1f;
        public int attackDamage = 10;
        public LayerMask enemyLayer;

        [Header("Debug / Test")]
        public bool isBattlePhase = true;

        // 자식(ArcherTower)에서도 이 변수들을 써야 하므로 protected로 선언합니다.
        protected Transform currentTarget;
        protected float lastFireTime = 0f;
        protected bool hasTarget = false;

        protected virtual void Update()
        {
            if (isBattlePhase)
            {
                FindClosestEnemy();

                if (currentTarget != null)
                {

                    // 쿨타임 계산 공통 로직
                    if (Time.time >= lastFireTime + fireRate)
                    {
                        Shoot(); 
                        lastFireTime = Time.time;
                    }
                }
            }
        }

        // 타겟 탐지 공통 로직 (ArcherTower에 있던 것을 부모로 끌어올림)
        protected void FindClosestEnemy()
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

            if (hasTarget && !foundNew)
            {
                Debug.Log("타겟 처치 완료! 새로운 적이 나타날 때까지 탐색을 대기합니다.");
            }
            else if (!hasTarget && foundNew)
            {
                Debug.Log($"새로운 타겟 포착: '{nearestEnemy.name}'");
            }
            else if (hasTarget && foundNew && currentTarget != nearestEnemy)
            {
                Debug.Log($"타겟 파괴(또는 변경)됨! 다음 타겟: '{nearestEnemy.name}'");
            }

            hasTarget = foundNew;
            currentTarget = nearestEnemy;
        }

        // ✨ 핵심: 발사하는 방식은 자식 타워마다 다르기 때문에, 구현은 자식에게 맡깁니다.
        protected abstract void Shoot();

        // 에디터용 기즈모 공통 로직
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
    }

