using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utility;
using Defense;

namespace EnemySystem
{
    public abstract class Enemy : MonoBehaviour, IDamageable, IStatReceiver
    {
        [Header("기본 능력치 (자식 클래스에서 설정됨)")]
        public float HP;
        public float moveSpeed;
        public int AD;
        public float AS;

        

        [Header("감지 사거리")]

        public float chaseRange = 10f;
        public float attackRange = 5f;

        [Header("속성 시스템 관리")]
        public float elementDuration = 5f; // 속성 유지 시간
        public List<Element> allElementDatas;
        public List<ElementType> activeElements = new List<ElementType>();
        private List<IStatusEffect<IStatReceiver>> activeEffects = new List<IStatusEffect<IStatReceiver>>();
        public string currentElement = "None";
        private Dictionary<ElementType, Coroutine> elementTimers = new Dictionary<ElementType, Coroutine>();

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
            
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                activeEffects[i].OnTick(this, Time.deltaTime);
                
                // Status가 "나 시간 다 됐어!" 라고 하면 지워줍니다.
                if (activeEffects[i].IsFinished)
                {
                    activeEffects[i].OnRemove(this);
                    activeEffects.RemoveAt(i);
                }
            }

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


        private void ApplyElement(ElementType incomingElement)
        {
            // 🌟 1. 이미 속성이 하나 걸려 있다면 연계 체크!
            if (activeElements.Count > 0)
            {
                ElementType currentType = activeElements[0]; // 현재 걸려있는 속성

                // 현재 속성의 SO 데이터를 찾습니다. (예: Fire SO)
                Element currentElementData = allElementDatas.Find(x => x.elementType == currentType);

                if (currentElementData != null)
                {
                    // SO에게 "방금 들어온 속성이랑 연계되는 거 있어?" 라고 물어봅니다.
                    ComboData? combo = currentElementData.CheckCombo(incomingElement);

                    if (combo.HasValue) // 기획자님이 SO에 연계를 만들어 뒀다면!
                    {
                        // 핵심 변경점: 옛날 변수들 대신 SO에 적어둔 comboEffectData를 통째로 가져옵니다!
                        StatusData data = combo.Value.comboEffectData;
                        
                        if (data.skillPrefab != null)
                        {
                            Instantiate(data.skillPrefab, transform.position, Quaternion.identity);
                        }
                        // 가져온 데이터를 만능 Status에 넣어서 내 몸에 적용!
                        AddEffect(new UniversalStatus(data.effectName, data.duration, data.targetStat, data.changeAmount, data.dotDamage));
                        Debug.Log($"<color=yellow>[🔥 연계 발동!]</color> {data.effectName} 발생!");

                        // 연계가 터졌으니 기존 속성과 타이머는 초기화
                        activeElements.Clear();
                        if (elementTimers.ContainsKey(currentType) && elementTimers[currentType] != null)
                        {
                            StopCoroutine(elementTimers[currentType]);
                            elementTimers.Remove(currentType);
                        }
                        return; // 연계가 발동했으니 여기서 함수 종료
                    }
                }
            }

            // 🌟 2. 연계가 없거나 최초 부여일 때
            activeElements.Add(incomingElement);

            // SO에서 '최초 부여 시의 기본 효과 데이터'를 가져와서 씌웁니다.
            Element newElementData = allElementDatas.Find(x => x.elementType == incomingElement);
            if (newElementData != null)
            {
                StatusData baseData = newElementData.baseEffectData;
                
                // 도트딜이나 디버프가 세팅되어 있다면 만능 Status 발동!
                AddEffect(new UniversalStatus(baseData.effectName, baseData.duration, baseData.targetStat, baseData.changeAmount, baseData.dotDamage));
            }

            // 5초 타이머 시작 (기존 타이머가 있다면 끄고 새로 시작)
            if (elementTimers.ContainsKey(incomingElement) && elementTimers[incomingElement] != null)
            {
                StopCoroutine(elementTimers[incomingElement]);
            }
            elementTimers[incomingElement] = StartCoroutine(RemoveElementAfterDelay(incomingElement, elementDuration));
            Debug.Log($"<color=cyan>[속성 부여]</color> {incomingElement} 획득! 5초 타이머 시작.");
        }

        // 5초 뒤에 자동으로 호출되어 속성을 지우는 예약 함수
        private IEnumerator RemoveElementAfterDelay(ElementType element, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (activeElements.Contains(element))
            {
                activeElements.Remove(element);
                elementTimers.Remove(element); // 수첩에서도 지워줍니다.
                Debug.Log($"<color=gray>[속성 소멸]</color> {element} 시간이 다 되어 사라졌습니다.");
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
        public void ModifyStat(StatType type, float amount)
        {
            if (type == StatType.MoveSpeed) moveSpeed += amount;
            else if (type == StatType.AttackCooldown) AS += amount;
            else if (type == StatType.AttackPower) AD += (int)amount; // AD는 int니까 변환
        }

        // 🌟 [통로 2] Status가 "너 도트 데미지 달아!" 라고 던질 때 받는 곳 (기존 DamageInfo 함수와 별개로 추가)
        public void TakeDamage(float amount)
        {
            if (isDead) return;
            HP -= amount;
            Debug.Log($"<color=red>[도트딜]</color> {gameObject.name}이 {amount} 피해를 입음! (남은 체력: {HP})");
            
            if (HP <= 0) Die();
        }

        // 🌟 [통로 3] 새로운 상태이상(Status)을 내 몸에 달아주는 함수
        public void AddEffect(IStatusEffect<IStatReceiver> newEffect)
        {
            activeEffects.Add(newEffect);
            newEffect.OnApply(this); // 달아주자마자 "적용해!" 라고 명령
        }
    }
}