using UJam.Runtime.Combat;
using UJam.Runtime.Grid;
using UJam.Runtime.Navigation;
using UJam.Runtime.Phase;
using UJam.Runtime.Shop;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    [RequireComponent(typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        #region Inspector 창에 띄울 것들

        // Enemy 체력을 관리할 Component
        [SerializeField] private Health _health;

        // 상태별 Animation을 재생할 Component
        [SerializeField] private Animator _anim;

        // 목적지 이동을 처리할 Component
        [SerializeField] private NavigationDriver _nav;

        // 사망 시 Wallet에 지급할 재화
        [SerializeField, Min(0)] private long _currencyReward = 1L;

        #endregion

        #region Runtime Data

        // 현재 Enemy 스탯
        private EnemyStatus _status;

        // 현재 Enemy 중앙 상태 머신
        private EnemyFSM _fsm;

        // 현재 공격 사거리를 Cell 개수로 올림 변환한 값
        private int _attackRangeCellCount;

        // Health 사망 이벤트 연결 여부
        private bool _healthLinked;

        // 중복 사망 보상 지급을 막을 상태
        private bool _rewardGranted;

        #endregion

        #region Properties

        // 현재 Enemy 스탯
        public EnemyStatus Status { get { return _status; } } // 현재 스탯 반환

        // 현재 Enemy 상태 머신
        public EnemyFSM FSM { get { return _fsm; }} // 현재 FSM 반환

        // 현재 Health Component
        public Health Health { get { return _health; } } // 현재 Health 반환

        // 외부 사거리 판정자가 사용할 공격 가능 Cell 개수
        public int AttackRangeCellCount
        {
            get
            {
                // 최신 Singleton Grid 크기로 공격 사거리 Cell 개수 갱신
                UpdateAttackRangeCellCount();

                // Grid 크기로 변환된 현재 공격 사거리 반환
                return _attackRangeCellCount;
            }
        }

        #endregion

        #region Unity Life Cycle

        // Status와 Component를 준비하고 FSM 생성
        protected virtual void Awake()
        {
            // 파생 Enemy가 정의한 초기 스탯
            EnemyStatus startStatus = MakeStatus();

            // 스탯이 없으면 안전한 기본값 사용
            if (startStatus == null)
            {
                startStatus = new EnemyStatus();
            }

            // 인스턴스 전용 스탯 복사
            _status = startStatus.Copy();

            // 같은 GameObject에서 Health 보완
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }

            // 같은 GameObject에서 Animator 보완
            if (_anim == null)
            {
                _anim = GetComponent<Animator>();
            }

            // 같은 GameObject에서 Navigation 보완
            if (_nav == null)
            {
                _nav = GetComponent<NavigationDriver>();
            }

            // Enemy와 Animator를 연결한 FSM 생성
            _fsm = new EnemyFSM(this, _anim);

            // Health와 최초 Idle 상태 초기화
            _fsm.Init();
        }

        // Enemy 제거 전 Health 이벤트 해제
        protected virtual void OnDestroy()
        {
            // 실제로 연결된 이벤트만 해제
            if (_health != null && _healthLinked)
            {
                _health.Died -= OnHealthDead;
                _healthLinked = false;
            }
        }

        #endregion

        #region Public Commands

        // 외부 피해 요청을 Health에 전달하고 실제 피해량 반환
        public float TakeDamage(DamageInfo info)
        {
            // 피해를 적용할 Health 연결 확인
            if (_health == null)
            {
                // Health가 없는 실제 피해 0 반환
                return 0f;
            }

            // Health가 실제로 감소시킨 피해량 반환
            return _health.ApplyDamage(info.Damage);
        }

        // 전투 전 스탯 변경 적용
        public bool ApplyBeforeFight(IStatChange change)
        {
            // Idle 상태와 유효한 변경만 허용
            if (change == null || _status == null || _fsm == null || _fsm.State != EnemyStateKind.Idle)
            {
                // 적용 실패 반환
                return false;
            }

            // 외부 버프 또는 디버프 적용
            change.Apply(_status);

            // 변경된 스탯 검증
            _status.Sanitize();

            // 변경된 공격 사거리를 현재 Grid Cell 개수로 다시 계산
            UpdateAttackRangeCellCount();

            // 전투 시작 체력 다시 설정
            if (_health != null)
            {
                _health.SetStartHealth(_status.MaxHealth);
            }

            // 파생 Enemy 후처리 호출
            OnStatsBefore();

            // 적용 성공 반환
            return true;
        }

        // 전투 중 스탯 변경 적용
        public bool ApplyDuringFight(IStatChange change)
        {
            // 생존 상태와 유효한 변경만 허용
            if (change == null || _status == null || _fsm == null || _fsm.State == EnemyStateKind.Dead)
            {
                // 적용 실패 반환
                return false;
            }

            // 외부 버프 또는 디버프 적용
            change.Apply(_status);

            // 변경된 스탯 검증
            _status.Sanitize();

            // 변경된 공격 사거리를 현재 Grid Cell 개수로 다시 계산
            UpdateAttackRangeCellCount();

            // 파생 Enemy 후처리 호출
            OnStatsNow();

            // 적용 성공 반환
            return true;
        }

        // 외부 사거리 판정 전달
        public void SetRange(Object target, bool inside)
        {
            // 준비된 FSM에 타겟과 사거리 전달
            if (_fsm != null)
            {
                _fsm.SetRange(target, inside);
            }
        }

        // 외부 효과로 공격 타겟 변경
        public void ChangeTarget(Object target)
        {
            // Attack 상태의 타겟 변경 경계 호출
            if (_fsm != null)
            {
                _fsm.Attack.ChangeTarget(target);
            }
        }

        // Spawn 대기 완료 알림
        public void SpawnDone()
        {
            // Idle 상태 완료 경계 호출
            if (_fsm != null)
            {
                _fsm.Idle.Done();
            }
        }

        // 현재 타겟 공격 실행
        public void Hit()
        {
            // Attack 상태 공격 경계 호출
            if (_fsm != null)
            {
                _fsm.Attack.Hit();
            }
        }

        // Enemy 사망 요청
        public void Die()
        {
            // FSM terminal Dead 전환 요청
            if (_fsm != null)
            {
                _fsm.Die();
            }
        }

        #endregion

        #region FSM Commands

        // FSM이 Status 기반 Health 초기화
        internal bool InitHealth()
        {
            // 필수 Status와 Health 확인
            if (_status == null || _health == null)
            {
                // 초기화 실패 반환
                return false;
            }

            // Status 체력으로 Health 설정
            if (!_health.SetStartHealth(_status.MaxHealth))
            {
                // 초기화 실패 반환
                return false;
            }

            // 사망 이벤트 한 번만 연결
            if (!_healthLinked)
            {
                _health.Died += OnHealthDead;
                _healthLinked = true;
            }

            // 초기화 성공 반환
            return true;
        }

        // Idle 상태의 Spawn 행동 실행
        internal void Spawn()
        {
            // 파생 Enemy Spawn 행동 호출
            OnSpawn();
        }

        // Move 상태의 이동 행동 실행
        internal void Move()
        {
            // 현재 Target을 Navigation에 전달해 목적지 해석과 이동 요청 위임
            if (_fsm != null && _fsm.Target != null && _nav != null)
            {
                _nav.RequestMove(_fsm.Target, out _);
            }
        }

        // Attack 상태의 공격 행동 실행
        internal void Attack(Object target)
        {
            // 파생 Enemy 공격 행동 호출
            OnAttack(target);
        }

        // Dead 상태의 사망 행동 실행
        internal void Dead()
        {
            // Navigation의 보관된 이동 요청 정지
            if (_nav != null)
            {
                _nav.StopMovement();
            }

            // 현재 Enemy의 사망 보상 지급
            GrantDeathReward();

            // 현재 WaveController Singleton
            WaveController waveController = WaveController.Instance;

            // Wave에 등록된 Enemy라면 사망 수 반영
            if (waveController != null)
            {
                waveController.ReportEnemyDead(gameObject);
            }

            // 파생 Enemy 사망 행동 호출
            OnDead();
        }

        // FSM 상태 Animation 실행
        internal void PlayAnim(EnemyStateKind state)
        {
            // 파생 Enemy Animation 연결 호출
            OnAnim(state, _anim);
        }

        #endregion

        #region Enemy Overrides

        // 파생 Enemy 초기 스탯 생성
        protected virtual EnemyStatus MakeStatus()
        {
            // 기본 스탯 반환
            return new EnemyStatus();
        }

        // 파생 Enemy Spawn 행동
        protected virtual void OnSpawn()
        {
            // Spawn Animation과 대기 로직이 구현되어야 함
        }

        // 파생 Enemy 공격 행동
        protected virtual void OnAttack(Object target)
        {
            // 타겟과 Status를 사용하는 공격 로직이 구현되어야 함
        }

        // 파생 Enemy 사망 행동
        protected virtual void OnDead()
        {
            // 타겟팅·Collider·Animation·제거 로직이 구현되어야 함
        }

        // 파생 Enemy 상태 Animation
        protected virtual void OnAnim(EnemyStateKind state, Animator anim)
        {
            // Animator Parameter와 상태 연결이 구현되어야 함
        }

        // 전투 전 스탯 변경 후처리
        protected virtual void OnStatsBefore()
        {
            // 변경된 이동과 공격 수치 전달 로직이 구현되어야 함
        }

        // 전투 중 스탯 변경 후처리
        protected virtual void OnStatsNow()
        {
            // 지속 시간·중첩·복구 정책이 구현되어야 함
        }

        #endregion

        #region Helpers

        // Status 기반 직접 피해 적용
        protected bool TryDamage(Object target)
        {
            // Archive로 옮긴 HitZone·DamageType·DamageFlags 공격 흐름 비활성
            /*
            if (target == null || _status == null)
            {
                return false;
            }

            Transform targetTransform = GetTargetTransform(target);
            if (targetTransform == null)
            {
                return false;
            }

            HitZoneReceiver receiver = targetTransform.GetComponentInParent<HitZoneReceiver>();
            if (receiver == null)
            {
                receiver = targetTransform.GetComponentInChildren<HitZoneReceiver>();
            }

            if (receiver == null || _status.Damage <= 0f)
            {
                return false;
            }

            DamageInfo info = new DamageInfo(
                this,
                _status.Damage,
                new DamageType(_status.DamageTypeId),
                null,
                new HitContext(receiver.Zone),
                DamageFlags.None);

            receiver.TakeDamage(info);
            return true;
            */

            // 구형 공격 흐름 비활성 결과 반환
            return false;
        }

        // Status 공격 사거리를 현재 Grid 크기 기준 Cell 개수로 변환
        private bool UpdateAttackRangeCellCount()
        {
            // 변환에 필요한 Status와 초기화된 Grid 정보 확인
            if (_status == null || !GridSystem.Instance.IsInitialized)
            {
                // 사거리 변환 실패 반환
                return false;
            }

            // 직사각형 Cell에서도 사거리를 과소평가하지 않을 짧은 변 길이
            float cellLength = Mathf.Min(GridSystem.Instance.CellWidth, GridSystem.Instance.CellHeight);

            // Cell 길이가 유효한 양수인지 확인
            if (cellLength <= 0f || float.IsNaN(cellLength) || float.IsInfinity(cellLength))
            {
                // 잘못된 Cell 길이 변환 실패 반환
                return false;
            }

            // World 단위 공격 사거리를 포함해야 할 Cell 개수로 올림 계산
            _attackRangeCellCount = Mathf.Max(
                0,
                Mathf.CeilToInt(_status.Range / cellLength));

            // 공격 사거리 Cell 개수 갱신 성공 반환
            return true;
        }

        // Health 사망 이벤트 처리
        private void OnHealthDead()
        {
            // FSM 사망 전환 요청
            Die();
        }

        // 단일 Wallet에 Enemy 사망 보상 한 번 지급
        private void GrantDeathReward()
        {
            // 이미 지급했거나 보상이 없는 Enemy 차단
            if (_rewardGranted || _currencyReward <= 0L)
            {
                // 보상 지급 없이 종료
                return;
            }

            // Scene Wallet 준비 여부 확인
            Wallet wallet = Wallet.Instance;
            if (wallet == null)
            {
                // Wallet이 없는 Scene에서 지급 없이 종료
                return;
            }

            // Wallet이 보상을 실제 반영했을 때만 지급 완료 기록
            if (wallet.AddCurrency(_currencyReward))
            {
                _rewardGranted = true;
            }
        }

        // Archive 이전 피격 부위 탐색 Helper 비활성
        /*
        private static Transform GetTargetTransform(Object target)
        {
            Transform targetTransform = target as Transform;
            if (targetTransform != null)
            {
                return targetTransform;
            }

            GameObject targetObject = target as GameObject;
            if (targetObject != null)
            {
                return targetObject.transform;
            }

            Component targetComponent = target as Component;
            if (targetComponent != null)
            {
                return targetComponent.transform;
            }

            return null;
        }
        */

        #endregion
    }
}
