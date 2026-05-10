using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        protected Transform target;
        protected Transform defaultTarget;

        protected List<string> priorityTags = new List<string> { "Player", "Decoy" };
        protected HashSet<Transform> priorityInChaseRange = new HashSet<Transform>();

        protected bool isDead = false;
        protected bool isAttacking = false;
        protected Rigidbody rb;

        protected abstract void InitStatus();

        public virtual void Start()
        {
            rb = GetComponent<Rigidbody>();
            InitStatus();

            GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
            if (baseObj != null) defaultTarget = baseObj.transform;
            target = defaultTarget;

            CreateDetectionSphere(chaseRange, DetectionSphere.RangeType.Chase);
            CreateDetectionSphere(attackRange, DetectionSphere.RangeType.Attack);
        }

        private void CreateDetectionSphere(float radius, DetectionSphere.RangeType type)
        {
            GameObject go = new GameObject(type.ToString() + "Range");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            var ds = go.AddComponent<DetectionSphere>();
            ds.type = type;
            ds.Init(this, radius);

            ds.OnTargetEnter = HandleTargetEnter;
            ds.OnTargetExit = HandleTargetExit;
        }

        protected virtual void Update()
        {
            if (isDead || target == null) return;

            Vector3 myPos = transform.position;
            Vector3 destination = target.position;

            Collider targetCol = target.GetComponent<Collider>();
            if (targetCol != null) destination = targetCol.ClosestPoint(myPos);

            float distanceToTarget = Vector3.Distance(myPos, destination);

            if (distanceToTarget > attackRange * 0.9f)
            {
                MoveToTarget(destination);
            }
            else
            {
                StopAndLookAt(destination);
            }
        }

        protected virtual void MoveToTarget(Vector3 destination)
        {
            Vector3 direction = (destination - transform.position).normalized;
            direction.y = 0;

            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

            transform.position += direction * moveSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
                transform.forward = direction;
        }

        private void StopAndLookAt(Vector3 destination)
        {
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
                if (other == target || other.CompareTag("Base"))
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

            if (attackRange > 5f)
            {
                ThrowProjectile();
            }
            else
            {
                if (target != null)
                    target.SendMessage("takeDamage", AD, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void ThrowProjectile()
        {
            if (projectilePrefab != null && throwPoint != null)
            {
                // 원본 projectilePrefab에 대입하지 않고 go라는 지역변수 사용
                GameObject go = Instantiate(projectilePrefab, throwPoint.position, Quaternion.identity);

                Projectile p = go.GetComponent<Projectile>();
                if (p != null) p.Launch(target, AD);
            }
            else
            {
                Debug.LogError($"{gameObject.name}: 프리팹 또는 발사위치가 비어있음!");
            }
        }

        public void takeDamage(float damage)
        {
            if (isDead) return;
            HP -= damage;
            if (HP <= 0) Die();
        }

        public virtual void Die()
        {
            if (isDead) return;
            isDead = true;
            StopAllCoroutines();
            if (rb != null) rb.isKinematic = true;
            GetComponent<Collider>().enabled = false;
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