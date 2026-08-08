using UJam.Runtime.Combat;
using UJam.Runtime.Enemy.Movement;
using UJam.Runtime.Phase;
using UJam.Runtime.Shop;
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


        #endregion

        #region Unity Life Cycle

        // Status와 Component를 준비하고 FSM 생성
        protected virtual void Awake()
        {
            // 속성 초기화 위치에서는 EnemyBase 자신의 객체를 전달할 수 없기 때문에 Awake에 임시로 정의
            _fsm = new EnemyFSM(this);
        }

        // 발표용 거리 판정과 이동과 공격 상태 갱신
        private void Update()
        {
        }

        // Enemy 제거 전 Health 이벤트 해제
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
            _movement.Enter();
        }

        // Attack의 기본 공통 행동 정의
        public virtual void Attack()
        {
        }

        // Dead의 기본 공통 행동 정의
        public virtual void Dead()
        {

        }

        #endregion

        
        #region Public Commands

        public float TakeDamage(DamageInfo info)
        {
            return 0f;
        }

        #endregion
    }
}
