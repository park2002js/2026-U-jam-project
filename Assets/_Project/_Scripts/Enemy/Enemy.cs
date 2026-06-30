using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Utility;

namespace EnemySystem
{
    public abstract class Enemy : MonoBehaviour
    {
        [Header("기본 능력치")]
        public float HP;
        public float moveSpeed;
        public int AD;
        public float AS;

        [Header("감지 사거리")]
        public float chaseRange = 10f;
        public float attackRange = 5f;

        [Header("원거리 공격 설정")]
        public GameObject projectilePrefab;
        public Transform throwPoint;

        [Header("타겟 설정")]
        [SerializeField] protected Transform defaultTarget;
        protected Transform target;

        [HideInInspector] public Transform forcedTarget;
        protected Transform ActiveTarget => forcedTarget != null ? forcedTarget : target;

        // 바리케이드 공략: 이동 목표(벽 앞 칸) + 공격 거리 기준(벽 칸)
        [HideInInspector] public Vector3 forcedPoint;        // 이동 목표 (서는 곳)
        [HideInInspector] public Vector3 forcedAttackPoint;  // 공격 거리 기준 (벽 칸)
        [HideInInspector] public bool hasForcedPoint = false;
        public void SetForcedPoint(Vector3 stand, Vector3 wall)
        {
            forcedPoint = stand;
            forcedAttackPoint = wall;
            hasForcedPoint = true;
        }
        public void ClearForcedPoint() { hasForcedPoint = false; }

        protected List<string> priorityTags = new List<string> { "Player", "Decoy" };
        protected HashSet<Transform> priorityInChaseRange = new HashSet<Transform>();

        protected bool isDead = false;
        protected bool isAttacking = false;
        protected Rigidbody rb;

        protected AIPath aiPath;
        private EnemySpawner enemySpawner;

        protected abstract void InitStatus();

        public virtual void Start()
        {
            rb = GetComponent<Rigidbody>();
            InitStatus();
            aiPath = GetComponent<AIPath>();

            if (aiPath != null)
            {
                aiPath.maxSpeed = moveSpeed;
                aiPath.endReachedDistance = attackRange * 0.8f;
                aiPath.canMove = true;
            }

            if (defaultTarget == null)
            {
                GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
                if (baseObj != null) defaultTarget = baseObj.transform;
            }
            target = defaultTarget;

            CreateDetectionSphere(chaseRange, DetectionSphere.RangeType.Chase);
            CreateDetectionSphere(attackRange, DetectionSphere.RangeType.Attack);
        }

        private void CreateDetectionSphere(float radius, DetectionSphere.RangeType type)
        {
            GameObject go = new GameObject(type.ToString() + "Range");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.layer = gameObject.layer;

            var ds = go.AddComponent<DetectionSphere>();
            ds.type = type;
            ds.Init(this, radius);
            ds.OnTargetEnter = HandleTargetEnter;
            ds.OnTargetExit = HandleTargetExit;
        }

        protected virtual void Update()
        {
            if (isDead) return;

            // 바리케이드 공략 중: 이동은 벽 앞 칸으로, 멈춤 판정은 벽 칸 기준 attackRange
            if (hasForcedPoint)
            {
                float distToWall = Vector3.Distance(transform.position, forcedAttackPoint);
                if (distToWall > attackRange) MoveToTarget(forcedPoint);
                else StopAndLookAt(forcedPoint);
                return;
            }

            Transform t = ActiveTarget;
            if (t == null) return;

            float distanceToTarget = Vector3.Distance(transform.position, t.position);
            if (distanceToTarget > attackRange) MoveToTarget(t.position);
            else StopAndLookAt(t.position);
        }

        protected virtual void MoveToTarget(Vector3 destination)
        {
            if (aiPath != null)
            {
                aiPath.canMove = true;
                if (Vector3.Distance(aiPath.destination, destination) > 0.1f)
                    aiPath.destination = destination;

                if (aiPath.velocity.sqrMagnitude > 0.01f)
                {
                    Vector3 moveDir = aiPath.velocity.normalized;
                    moveDir.y = 0;
                    transform.forward = Vector3.Lerp(transform.forward, moveDir, Time.deltaTime * 10f);
                }
            }
            else
            {
                Vector3 direction = (destination - transform.position).normalized;
                direction.y = 0;
                if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                transform.position += direction * moveSpeed * Time.deltaTime;
                if (direction != Vector3.zero) transform.forward = direction;
            }
        }

        private void StopAndLookAt(Vector3 destination)
        {
            if (aiPath != null) aiPath.canMove = false;
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                rb.angularVelocity = Vector3.zero;
            }
            Vector3 direction = (destination - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
                transform.forward = Vector3.Lerp(transform.forward, direction, Time.deltaTime * 10f);
        }

        private void HandleTargetEnter(Transform other, DetectionSphere.RangeType type)
        {
            if (priorityTags.Contains(other.tag))
            {
                if (type == DetectionSphere.RangeType.Chase)
                {
                    priorityInChaseRange.Add(other);
                    UpdateTarget();
                }
            }
            if (type == DetectionSphere.RangeType.Attack)
            {
                if (forcedTarget == null && !hasForcedPoint &&
                    (other == target || other.CompareTag("Base")))
                {
                    if (!isAttacking) StartCoroutine(AttackRoutine());
                }
            }
        }

        private void HandleTargetExit(Transform other, DetectionSphere.RangeType type)
        {
            if (priorityInChaseRange.Contains(other))
            {
                priorityInChaseRange.Remove(other);
                if (type == DetectionSphere.RangeType.Chase) UpdateTarget();
            }
        }

        private void UpdateTarget()
        {
            if (isAttacking) return;
            if (forcedTarget != null || hasForcedPoint) return;

            if (priorityInChaseRange.Count > 0)
            {
                Transform bestTarget = null;
                float closestDist = Mathf.Infinity;
                foreach (Transform p in priorityInChaseRange)
                {
                    if (p == null) continue;
                    float dist = Vector3.Distance(transform.position, p.position);
                    if (dist < closestDist) { closestDist = dist; bestTarget = p; }
                }
                target = bestTarget;
            }
            else target = defaultTarget;
        }

        IEnumerator AttackRoutine()
        {
            isAttacking = true;
            if (aiPath != null) aiPath.canMove = false;

            while (target != null && !isDead)
            {
                Vector3 myPos = transform.position;
                Vector3 targetPos = target.position;
                Collider col = target.GetComponent<Collider>();
                if (col != null) targetPos = col.ClosestPoint(myPos);

                float distance = Vector3.Distance(myPos, targetPos);
                if (distance > attackRange + 1.5f) break;

                PerformAttack();
                float speed = AS > 0 ? AS : 1f;
                yield return new WaitForSeconds(1f / speed);
            }

            isAttacking = false;
            UpdateTarget();
        }

        protected virtual void PerformAttack()
        {
            if (attackRange > 5f) ThrowProjectile();
            else if (target != null) target.SendMessage("TakeDamage", (float)AD, SendMessageOptions.DontRequireReceiver);
        }

        private void ThrowProjectile()
        {
            if (projectilePrefab != null && throwPoint != null)
            {
                GameObject go = Instantiate(projectilePrefab, throwPoint.position, Quaternion.identity);
                Enemy_Projectile p = go.GetComponent<Enemy_Projectile>();
                if (p != null) p.Launch(target, AD);
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;
            HP -= damage;
            if (HP <= 0) Die();
        }

        public void SetSpawner(EnemySpawner spawner) { enemySpawner = spawner; }

        public virtual void Die()
        {
            if (isDead) return;
            isDead = true;
            StopAllCoroutines();
            if (aiPath != null) aiPath.canMove = false;
            if (rb != null) rb.isKinematic = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            if (enemySpawner != null) enemySpawner.OnEnemyDestroyed();
            StartCoroutine(DeathAnimation());
        }

        IEnumerator DeathAnimation()
        {
            float timer = 0;
            while (timer < 2f)
            {
                transform.Translate(Vector3.down * 2.5f * Time.deltaTime);
                timer += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}