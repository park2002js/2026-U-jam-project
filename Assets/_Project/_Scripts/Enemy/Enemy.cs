using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utility;

namespace EnemySystem
{
    public abstract class Enemy : MonoBehaviour, IDamageable
    {
        [Header("기본 능력치 (자식 클래스에서 설정됨)")]
        public float HP;
        public float moveSpeed;
        public int AD;
        public float AS;

        [Header("감지 사거리")]

        public float chaseRange = 10f;
        public float attackRange = 5f;
        public List<ElementType> activeElements = new List<ElementType>();

        protected Transform target;
        protected Transform defaultTarget;
        protected List<string> priorityTags = new List<string> { "Player", "Decoy" };
        protected HashSet<Transform> priorityInChaseRange = new HashSet<Transform>();

        protected bool isDead = false;
        protected bool isAttacking = false;
        protected Rigidbody rb;

        // 자식 클래스에서 반드시 구현해야 하는 능력치 설정 함수
        protected abstract void InitStatus();

        public virtual void Start()
        {
            rb = GetComponent<Rigidbody>();

            // 1. 자식 클래스의 능력치 설정 호출
            InitStatus();

            // 2. 기본 타겟(성벽) 설정
            GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
            if (baseObj != null) defaultTarget = baseObj.transform;
            target = defaultTarget;

            // 3. 센서 구체 생성
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

            // 실제 3D 거리 계산 (Y축 포함)
            float distanceToTarget = Vector3.Distance(myPos, destination);

            if (distanceToTarget > attackRange * 0.9f)
            {
                MoveToTarget();
            }
            else
            {
                if (rb != null)
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                    rb.angularVelocity = Vector3.zero;
                }
                LookAtTarget();
            }
        }

        private void LookAtTarget()
        {
            if (target == null) return;

            Vector3 myPos = transform.position;
            Vector3 lookDest = target.position;

            Collider col = target.GetComponent<Collider>();
            if (col != null) lookDest = col.ClosestPoint(myPos);

            Vector3 direction = (lookDest - myPos).normalized;
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

        protected virtual void MoveToTarget()
        {
            if (target == null) return;

            Vector3 destination = target.position;
            if (target.CompareTag("Base"))
            {
                Collider col = target.GetComponent<Collider>();
                if (col != null) destination = col.ClosestPoint(transform.position);
            }

            Vector3 direction = (destination - transform.position).normalized;
            direction.y = 0;

            // 리지드바디가 있다면 X, Z 속도만 초기화 (중력 Y는 유지)
            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

            // X, Z 축 이동 (Y값은 보존하여 중력 작동 허용)
            Vector3 nextPos = transform.position + (direction * moveSpeed * Time.deltaTime);
            transform.position = nextPos;

            if (direction != Vector3.zero)
                transform.forward = direction;
        }

        IEnumerator AttackRoutine()
        {
            isAttacking = true;
            Debug.Log($"{gameObject.name}: 공격 시작");

            while (target != null && !isDead)
            {
                Vector3 myPos = transform.position;
                Vector3 targetPos = target.position;
                Collider col = target.GetComponent<Collider>();
                if (col != null) targetPos = col.ClosestPoint(myPos);

                float distance = Vector3.Distance(myPos, targetPos);

                if (distance > attackRange + 1.2f) break;

                // 실제 데미지 로직이 들어갈 자
                col.SendMessage("TakeDamage", AD, SendMessageOptions.DontRequireReceiver);
                float speed = AS > 0 ? AS : 1f;
                yield return new WaitForSeconds(1f / speed);
            }

            isAttacking = false;
            Debug.Log($"{gameObject.name}: 공격 종료");
            UpdateTarget();
        }


        public void TakeDamage(DamageInfo info)
        {
            if (isDead) return;

            // 1. 실제 데미지 적용
            HP -= info.Amount;
            Debug.Log($"[피격] {gameObject.name}이 {info.Amount}의 데미지를 받음. 남은 체력: {HP}");

            // 2. 속성 부여 로직 실행
            if (info.Element != ElementType.None)
            {
                ApplyElement(info.Element);
            }

            if (HP <= 0) Die();
        }

        public void TakeDamage(int damage)
        {
            TakeDamage(DamageInfo.Default((float)damage));
        }

        private void ApplyElement(ElementType incomingElement)
        {
            // [규칙 7] 리스트 꽉 참 검사
            if (activeElements.Count >= 2)
            {
                Debug.LogWarning($"<color=red>[속성 거부]</color> {gameObject.name}: 이미 2개의 속성이 존재합니다! ({incomingElement} 무시됨)");
                return; 
            }

            // [규칙 5] 속성 추가 및 부여 로그
            activeElements.Add(incomingElement);
            
            // 🌟 [확실한 확인] 현재 적이 가진 모든 속성을 리스트 형태로 출력
            // 예: "적 속성 상태: [ Poison, Wind ]"
            string elementListStr = string.Join(", ", activeElements); 
            Debug.Log($"<color=cyan>[속성 업데이트]</color> {gameObject.name}의 현재 속성: <b>[ {elementListStr} ]</b>");

            // [규칙 6] 연계 확인
            if (activeElements.Count == 2)
            {
                Debug.Log($"<color=yellow>[🔥 연계 발동!]</color> {activeElements[0]} + {activeElements[1]} 조합 성공!");
            }
        }

        private void TriggerElementCombo()
        {
            ElementType first = activeElements[0];
            ElementType second = activeElements[1];

            // [규칙 6] 연계 디버그 메시지
            Debug.Log($"<color=yellow>[🔥 속성 연계 발동!]</color> {first} + {second} 조합 폭발!!");

            // TODO: 여기서 실제로 연계 데미지를 주거나 이펙트를 생성합니다.
            // 예: if(first == ElementType.Poison && second == ElementType.Wind) { /* 확산 효과 */ }

            // 연계 후 리스트를 비워줄지 말지는 기획에 따라 결정합니다. 
            // 일단은 비워주는 처리를 넣어두었습니다.
            // activeElements.Clear(); 
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}