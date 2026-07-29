using UJam.Runtime.Combat;
using UJam.Runtime.Grid;
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

        // 사망 시 Wallet에 지급할 재화
        [SerializeField, Min(0)] private long _currencyReward = 1L;

        #endregion

        #region Runtime Data

        // 현재 Enemy 스탯
        private EnemyStatus _status;

        // 현재 Enemy 중앙 상태 머신
        private EnemyFSM _fsm;

        // 발표용 마지막 row 직선 이동 객체
        private TempNavi _tempNavi;

        // 활성화 전에 주입받을 기본 거점 Target
        private GameObject _configuredTarget;

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

            // 발표용 임시 직선 Navigation 생성
            _tempNavi = new TempNavi(transform);

            // Enemy와 Animator를 연결한 FSM 생성
            _fsm = new EnemyFSM(this, _anim);

            // Health와 최초 Idle 상태 초기화
            _fsm.Init();

            // 활성화 전에 전달받은 기본 거점 Target 반영
            if (_configuredTarget != null)
            {
                _fsm.SetTarget(_configuredTarget);
            }
        }

        // 발표용 거리 판정과 이동과 공격 상태 갱신
        private void Update()
        {
            // 준비된 FSM의 발표용 상태 흐름 실행
            if (_fsm != null)
            {
                _fsm.Tick();
            }
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

        // Enemy 활성화 전후에 기본 거점 Target 주입
        public void ConfigureTarget(GameObject target)
        {
            // 이후 초기화와 상태 전환에서 사용할 Target 저장
            _configuredTarget = target;

            // 이미 준비된 FSM에도 즉시 Target 전달
            if (_fsm != null)
            {
                _fsm.SetTarget(target);
            }
        }

        // 외부 효과로 공격 타겟 변경
        public void ChangeTarget(GameObject target)
        {
            // 준비된 FSM에 새 Target 전달
            if (_fsm != null)
            {
                _fsm.SetTarget(target);
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

        // Move 상태의 발표용 직선 이동 허용
        internal void StartMovement()
        {
            // 준비된 임시 Navigation 이동 허용
            if (_tempNavi != null)
            {
                _tempNavi.StartMovement();
            }
        }

        // 발표용 목표 Cell을 향한 직선 이동 실행
        internal void Move(Vector2Int targetCell)
        {
            // 준비된 스탯과 임시 Navigation 확인
            if (_status != null && _tempNavi != null)
            {
                _tempNavi.Move(targetCell, AttackRangeCellCount, _status.Speed);
            }
        }

        // Attack과 Dead 상태의 발표용 이동 중단
        internal void StopMovement()
        {
            // 준비된 임시 Navigation 이동 차단
            if (_tempNavi != null)
            {
                _tempNavi.StopMovement();
            }
        }

        // Attack 상태의 공격 행동 실행
        internal void Attack(GameObject target, Vector3 attackPoint)
        {
            // 파생 Enemy에 피해 대상과 실제 Grid 공격 지점 전달
            OnAttack(target, attackPoint);
        }

        // Dead 상태의 사망 행동 실행
        internal void Dead()
        {
            // 발표용 임시 이동 중단
            StopMovement();

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

            // 공통 사망 처리를 마친 Enemy 객체 즉시 제거 예약
            Destroy(gameObject);
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
        protected virtual void OnAttack(GameObject target, Vector3 attackPoint)
        {
            // 타겟과 Grid 공격 지점과 Status를 사용하는 공격 로직이 구현되어야 함
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
        protected bool TryDamage(GameObject target)
        {
            // Target과 Status와 피해량 확인
            if (target == null || _status == null)
            {
                // 공격할 수 없는 Target 실패 반환
                return false;
            }

            // Target 자신에서 피해 계약 확인
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable == null)
            {
                // Target 자식에서 피해 계약 보완
                damageable = target.GetComponentInChildren<IDamageable>();
            }

            // 피해 계약과 양수 피해량 확인
            if (damageable == null || _status.Damage <= 0f)
            {
                // 피해 전달 실패 반환
                return false;
            }

            // 현재 Enemy 스탯 기반 피해 정보 생성
            DamageInfo info = new DamageInfo(
                _status.Damage,
                gameObject.name,
                DamageSourceKind.Enemy);

            // 실제 체력이 감소했는지 반환
            return damageable.TakeDamage(info) > 0f;
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

        #endregion
    }
}
