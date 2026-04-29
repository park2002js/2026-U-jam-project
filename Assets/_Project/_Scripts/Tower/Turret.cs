using UnityEngine;

namespace Building
{
    // 1. MonoBehaviour 대신 DefenseBuilding을 상속받도록 변경합니다.
    public class Turret : DefenseBuilding
    {
        [Header("Turret Settings")]
        [Tooltip("포탑의 공격 사거리")]
        public float attackRange = 10f;
        
        [Tooltip("공격 딜레이 (초). 낮을수록 공격이 빠름")]
        public float fireRate = 1f; 
        public int damage = 10;
        
        [Tooltip("적을 감지할 레이어")]
        public LayerMask enemyLayer;

        private Transform target;
        private float fireCountdown = 0f;

        void Start()
        {
            // 0.2초 간격으로 사거리 내 가장 가까운 적을 탐색합니다.
            InvokeRepeating("UpdateTarget", 0f, 0.2f);
        }

        void Update()
        {
            // 타겟이 없으면 공격 로직을 실행하지 않음
            if (target == null) return;

            // 공격 쿨타임 계산 및 공격 실행
            if (fireCountdown <= 0f)
            {
                Attack();
                fireCountdown = fireRate; 
            }

            fireCountdown -= Time.deltaTime;
        }

        // 사거리 내 가장 가까운 적을 찾는 함수
        void UpdateTarget()
        {
            Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
            float shortestDistance = Mathf.Infinity;
            GameObject nearestEnemy = null;

            foreach (Collider enemyCollider in enemiesInRange)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, enemyCollider.transform.position);
                if (distanceToEnemy < shortestDistance)
                {
                    shortestDistance = distanceToEnemy;
                    nearestEnemy = enemyCollider.gameObject;
                }
            }

            if (nearestEnemy != null && shortestDistance <= attackRange)
            {
                target = nearestEnemy.transform;
            }
            else
            {
                target = null; // 적이 사거리를 벗어나면 타겟팅 해제
            }
        }

        void Attack()
        {
            // 추후 여기에 발사체(Projectile)를 생성하는 코드가 들어갑니다.
            Debug.Log($"[Attack] {target.name} 공격! (속성: {myElement}, 데미지: {damage})");
        }

        // =========================================================
        // --- 부모 클래스(DefenseBuilding)의 추상 함수 구현(Override) ---
        // =========================================================

        public override string GetUpgradeDescription()
        {
            return "사거리 증가, 공격 속도 증가";
        }

        public override int GetUpgradeCost()
        {
            return baseCost * currentLevel;
        }

        public override void ApplyUpgrade()
        {
            if (CanUpgrade())
            {
                attackRange += 2f;
                fireRate *= 0.8f; 
                currentLevel++; 
                
                Debug.Log($"[{buildingName}] 강화 완료! 현재 레벨: {currentLevel} / 사거리: {attackRange} / 공격 딜레이: {fireRate:F2}초");
            }
            else
            {
                Debug.Log("이미 최대 레벨입니다.");
            }
        }

        // 유니티 에디터에서 사거리를 빨간색 원으로 시각화
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}