using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding; // A* Pathfinding 사용
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
        [SerializeField] protected Transform defaultTarget; // ✨ 인스펙터에서 직접 tem_base 할당 가능
        protected Transform target;

        // ✨ [추가] BarricadeBreaker가 설정하는 강제 목표 (있으면 최우선)
        [HideInInspector] public Transform forcedTarget;
        protected Transform ActiveTarget => forcedTarget != null ? forcedTarget : target;

        protected List<string> priorityTags = new List<string> { "Player", "Decoy" };
        protected HashSet<Transform> priorityInChaseRange = new HashSet<Transform>();

        protected bool isDead = false;
        protected bool isAttacking = false;
        protected Rigidbody rb;

        protected AIPath aiPath; // ✨ A* 컴포넌트
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

            // ✨ 인스펙터에 타겟이 안 비어있으면 우선 사용, 비어있으면 태그로 찾기
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

        // ✨ [수정] ActiveTarget(강제 목표 우선)을 사용하도록 변경
        protected virtual void Update()
        {
            if (isDead) return;
            Transform t = ActiveTarget;
            if (t == null) return;

            // 복잡한 표면 계산 로직 끄기! 무조건 타겟의 정중앙 좌표 사용
            float distanceToTarget = Vector3.Distance(transform.position, t.position);

            if (distanceToTarget > attackRange)
            {
                MoveToTarget(t.position);
            }
            else
            {
                StopAndLookAt(t.position);
            }
        }

        protected virtual void MoveToTarget(Vector3 destination)
        {
            if (aiPath != null)
            {
                aiPath.canMove = true;

                // 목적지가 바뀌었을 때만 갱신
                if (Vector3.Distance(aiPath.destination, destination) > 0.1f)
                {
                    aiPath.destination = destination;
                }

                // 이동 방향을 향해 부드럽게 시선 회전
                if (aiPath.velocity.sqrMagnitude > 0.01f)
                {
                    Vector3 moveDir = aiPath.velocity.normalized;
                    moveDir.y = 0;
                    transform.forward = Vector3.Lerp(transform.forward, moveDir, Time.deltaTime * 10f);
                }
            }
            else
            {
                // AIPath가 없을 때의 예외 처리 (고전적 이동)
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
            {
                transform.forward = Vector3.Lerp(transform.forward, direction, Time.deltaTime * 10f);
            }
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
                // ✨ [수정] 바리케이드 공략 중(forcedTarget 있음)엔 베이스 공격 트리거 차단
                if (forcedTarget == null && (other == target || other.CompareTag("Base")))
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
            // ✨ [추가] 바리케이드 공략 중엔 우선순위 타겟 시스템 무시
            if (forcedTarget != null) return;

            if (priorityInChaseRange.Count > 0)
            {
                Transform bestTarget = null;
                float closestDist = Mathf.Infinity;
                foreach (Transform p in priorityInChaseRange)
                {
                    if (p == null) continue;
                    float dist = Vector3.Distance(transform.position, p.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestTarget = p;
                    }
                }
                target = bestTarget;
            }
            else
            {
                target = defaultTarget;
            }
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
            Debug.Log($"<color=cyan>[Attack]</color> {gameObject.name} 공격 실행");

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

        public void SetSpawner(EnemySpawner spawner)
        {
            enemySpawner = spawner;
        }

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