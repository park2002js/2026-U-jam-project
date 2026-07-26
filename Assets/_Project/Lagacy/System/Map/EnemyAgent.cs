using UnityEngine;
using Pathfinding;

namespace EnemySystem
{
    [RequireComponent(typeof(AIPath))]
    public class EnemyAgent : MonoBehaviour
    {
        [Tooltip("EnemyPathfinding의 경로 설정과 매칭할 ID")]
        public string enemyId = "goblin";
        [Tooltip("경로 배정 실패 시 재시도 간격")]
        public float retryInterval = 0.5f;

        private Enemy enemy;
        private AIPath aiPath;
        private Transform goal;

        private Barricade targetBarricade;
        private Vector3 attackPoint;
        private float attackTimer;
        private bool hasRoute;
        private float retryTimer;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            aiPath = GetComponent<AIPath>();
        }

        void OnEnable()
        {
            hasRoute = false;
            retryTimer = 0f;
            if (EnemyPathfinding.Instance != null)
                EnemyPathfinding.Instance.RegisterAgent(this);
        }

        void OnDisable()
        {
            if (EnemyPathfinding.Instance != null)
                EnemyPathfinding.Instance.UnregisterAgent(this);   // 예약 청소 포함
        }

        void Update()
        {
            var mgr = EnemyPathfinding.Instance;
            if (mgr == null || !mgr.Ready) return;

            if (!hasRoute)
            {
                retryTimer -= Time.deltaTime;
                if (retryTimer <= 0f) RequestRoute();
                return;
            }

            // 바리케이드 공략 중
            if (targetBarricade != null)
            {
                if (targetBarricade.Equals(null)) { RequestRoute(); return; }

                float d = Vector3.Distance(transform.position, attackPoint);
                if (d <= enemy.attackRange + 0.5f)
                {
                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        targetBarricade.TakeDamage(enemy.AD);
                        attackTimer = 1f / Mathf.Max(0.01f, enemy.AS);
                    }
                }
                return;
            }

            // 베이스 도착 → Enemy 기본 로직(베이스 공격)으로 인계
            if (aiPath.reachedEndOfPath && enemy.usingAssignedPath)
            {
                enemy.usingAssignedPath = false;
                aiPath.canSearch = true;
            }
        }

        public void RequestRoute()
        {
            var mgr = EnemyPathfinding.Instance;
            if (mgr == null || !mgr.Ready) return;
            if (goal == null) goal = mgr.GetGoal(enemyId);
            if (goal == null) return;

            var r = mgr.BuildRoute(this, transform.position, goal.position);
            if (!r.ok)
            {
                hasRoute = false;
                retryTimer = retryInterval;
                return;
            }

            hasRoute = true;
            targetBarricade = r.barricade;
            attackPoint = r.attackPoint;

            enemy.usingAssignedPath = true;
            aiPath.canSearch = false;

            if (r.path != null)
            {
                aiPath.canMove = true;
                aiPath.endReachedDistance = 0.3f;   // 배정 경로 끝까지 감
                aiPath.SetPath(r.path);
                Debug.Log($"[Agent] {name} 경로 {r.path.path.Count}칸" +
                          (targetBarricade != null ? $" → {targetBarricade.name} 공략" : " → 베이스"));
            }
            else
            {
                aiPath.canMove = false;   // 바리케이드 코앞, 제자리 공격
            }
        }
    }
}