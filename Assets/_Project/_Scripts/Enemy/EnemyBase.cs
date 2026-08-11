using System.Collections;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy.Movement;
using UJam.Runtime.Phase;
using UJam.Runtime.Shop;
using Unity.VisualScripting;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    [RequireComponent(typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        #region Inspector 창에 띄울 것들

        // 사용할 Status 객체 [Inspector에서 할당]
        [SerializeField] private EnemyStatus _status;

        #endregion

        #region Properties

        /// <summary>
        /// Enemy가 보유한 FSM 시스템 객체
        /// EnemyFSM객체를 통해 상태를 제어한다.
        /// </summary>
        private EnemyFSM _fsm;

        /// <summary>
        /// Enemy가 보유한 이동 알고리즘.
        /// EnemyStatus에서 가져온 객체를 사용한다.
        /// </summary>
        private EnemyMovement _movement;

        // 현재 Enemy 스탯
        public EnemyStatus Status { get { return _status; } } // 현재 스탯 반환

        // 현재 Enemy 상태 머신
        public EnemyFSM FSM { get { return _fsm; }} // 현재 FSM 반환

        // 현재 Enemy 움직임 로직
        public EnemyMovement Movement { get { return _movement; }} // 현재 movement 반환


        #endregion

        #region Unity Life Cycle

        // Status와 Component를 준비하고 FSM 생성
        protected virtual void Awake()
        {
            // 속성 초기화 위치에서는 EnemyBase 자신의 객체를 전달할 수 없기 때문에 Awake에 임시로 정의
            _fsm = new EnemyFSM(this);
        }

        private void Update()
        {
        }

        protected virtual void OnDestroy()
        {

        }

        #endregion

        #region FSM Commands

        // Idle의 기본 공통 행동 정의
        public virtual void Idle()
        {
            // 1. Status 객체의 초기화
            _status.init();            

            // 2. 이동 알고리즘 Status에서 가져와서 할당
            _movement = _status.Movement;
            // 없으면 기본 이동 방식 (열 이동 방식)을 사용하도록 하여 오류 방어
            if(_movement == null)
            {
                Debug.LogError($"{name}의 EnemyStatus에 EnemyMovement가 할당되지 않았습니다.", this);
                _movement = GetComponent<GridColMovement>();
                if (_movement == null) _movement = gameObject.AddComponent<GridColMovement>();
            }
            // EnemyBase 객체를 전달하여 초기화
            _movement.init(this);

            // 3. 이벤트 구독 연결
            _status.Died += _fsm.SetDead; // 체력이 0이되었을 때 발생하는 이벤트에, Dead 상태로 전환하는 함수 구독

            // 4. 우선 공격 대상 Stack에 거점 할당
            _fsm.Targets.Clear(); // Stack 내부의 요소들을 명시적으로 초기화 함
            _fsm.Targets.Add(GameObject.FindGameObjectWithTag("BaseCore")); // 초기화된 스택에 요소 추가 = 0번째 요소를 거점으로 설정
        }

        // Move의 기본 공통 행동 정의
        public virtual void Move()
        {
            // 필요하다면 animation 단계를 여기에 추가
            _movement.Enter();
        }

        // Attack의 기본 공통 행동 정의
        public virtual void Attack()
        {
            // 우선 공격 대상을 담는 Stack이 "GameObject"타입을 담는 것으로 정의되어 있기 때문에, TakeDamage를 호출하기 위해서 IDamageable로 형변환을 시도
            GameObject target = _fsm.Targets[_fsm.Targets.Count - 1];
            IDamageable damageable = target.GetComponent<IDamageable>();

            // 형변환 실패시 IDamageable을 상속받은 대상을 공격하고 있지 않다는 뜻이므로 Debug.Log를 호출하도록 하는 것으로 
            if (damageable == null)
            {
                Debug.LogError($"공격 대상 {target.name}의 TakeDamage를 호출할 수 없습니다.", target);
                return;
            }

            damageable.TakeDamage(new DamageInfo(_status.AttackDamage, name, DamageSourceKind.Enemy));
        }

        // Dead의 기본 공통 행동 정의
        public virtual void Dead()
        {
            // 1. 모든 코루틴 종료
            _fsm.Move.Exit();
            _fsm.Attack.Exit();
            StopAllCoroutines();

            // 2. 모든 물리 상호작용 제거
            foreach (Collider targetCollider in GetComponentsInChildren<Collider>())
            {
                targetCollider.enabled = false;
            }

            // 3. Wallet에 돈 추가
            if (Wallet.Instance != null)
            {
                Wallet.Instance.AddCurrency(_status.Credits);
            }

            // 4. WaveController에 사망 정보 보내기
            if (WaveController.Instance != null)
            {
                WaveController.Instance.ReportEnemyDead(gameObject);
            }

            // 5. 기본 Animation을 시작하고 삭제시킨다.
            StartCoroutine(DeadAnim());
        }

        /// <summary>
        /// 기본 Animation으로, Y축 아래로 하강한 뒤 Destroy를 통해 인스턴스를 게임내에서 지운다.
        /// 필요시 해당 animation을 구체화할 수 있다.
        /// </summary>
        public virtual IEnumerator DeadAnim()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.down * 1.5f;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, elapsed / duration);
                yield return null;
            }

            Destroy(gameObject);
        }

        #endregion

        
        #region Public Commands

        public float TakeDamage(DamageInfo info)
        {
            Debug.Log("[EnemyBase] : TakeDamage 호출됨");
            _status.ApplyDamage(info.Damage);
            return 0f;
        }

        /// <summary>
        /// FSM 시스템의 ReTargeting을 호출하는 중간자 역할
        /// </summary>
        public void ReTargeting()
        {
            _fsm.ReTargeting();
        }

        #endregion
    }
}
