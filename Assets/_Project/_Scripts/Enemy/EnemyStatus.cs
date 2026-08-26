using System;
using UJam.Runtime.Enemy.Movement;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    /// <summary>
    /// 외부에 의해 변화할 수 있는, Enemy의 기본 수치들을 관리하고 정의하는 Class
    /// </summary>
    [Serializable]
    public class EnemyStatus : MonoBehaviour
    {
        #region 기본 수치 값 & 속성들
        // Enemy 이름
        [SerializeField] private String _enemyName;

        // 쓰러질 때 주는 재화량
        [SerializeField, Min(0)] private int _credits;

        // 최대 체력
        [SerializeField, Min(0.0001f)] private float _maxHealth;

        // 이동속도
        [SerializeField, Min(0f)] private float _speed;

        // 공격력
        [SerializeField, Min(0f)] private float _attackDamage;

        // 공격 속도
        [SerializeField, Min(0f)] private float _attackSpeed;

        // 사거리
        [SerializeField, Min(0f)] private float _attackRange;

        // 이동 알고리즘 (Component로 넣은 객체를 할당)
        [SerializeField] private EnemyMovement _movement;


        // 죽었는지 여부를 나타내는 속성
        private bool _isDead = false;

        // 생존에서 사망으로 처음 전환될 때 알리는 이벤트 (UI, 소유중인 객체 용)
        public event Action Died;

        // 외부에게 체력 변경을 알리는 이벤트 (현재 체력, 최대 체력) (UI 용)
        public event Action<float, float> OnEnemyHpChanged;

        #endregion

        #region 실시간 수치 값
        // 현재 체력
        private float _hp;

        // 실제 이동속도
        private float _sp;

        // 실제 공격력
        private float _ad;

        // 실제 공격 속도
        private float _as;

        // 실제 사거리
        private float _range;

        #endregion

        #region 외부에서 읽을 수 있도록 하는 실시간 값
        public String EnemyName => _enemyName;
        public int Credits => _credits;
        public float HP => _hp;
        public float Speed => _sp;
        public float AttackDamage => _ad;
        public float AttackSpeed => _as;
        public float AttackRange => _range;
        public EnemyMovement Movement => _movement;

        #endregion

        /// <summary>
        /// 모든 기본 수치 값을 실시간 수치 값으로 전환하는 초기화 함수
        /// Idle 상태에서 호출한다.
        /// </summary>
        public void init()
        {
            // 1. 기본 값 최종 변경 사항 반영
            SetDefaults();

            // 2. 실시간 값으로 반영
            _hp = _maxHealth;
            _sp = _speed;
            _ad = _attackDamage;
            _as = _attackSpeed;
            _range = _attackRange;
        }

        // Enemy가 TakeDamage로 데미지를 입을 때마다 호출하는 함수. 인자만큼 체력을 깎고 죽었는지 여부를 체크한다.
        public float ApplyDamage(float damage)
        {
            // 이미 사망한 대상은 다시 피해를 받지 않도록 하기 위해 0을 반환하는 것으로 종료
            if (_isDead) return 0f;


            /*
                별도의 Damage 감소 정책이 존재한다면 이곳에 정의
            */

            
            // 혹은 피해량이 유효하지 않은 값이면 0을 반환하는 것으로 종료
            if (damage <= 0f || !float.IsFinite(damage)) return 0f;

            // 체력 감소 이행, 만약 깎인 채력이 0보다 작으면 0으로 보정
            float previousHp = _hp;
            _hp = Math.Max(0f, _hp - damage);
            float appliedDamage = previousHp - _hp;

            // 피해를 받은 객체와 실제 피해량과 남은 체력 출력
            Debug.Log($"[Health] {gameObject.name} 데미지 {appliedDamage} 받음 ({_hp}/{_maxHealth})");


            // 실제 체력 변화 이후의 상태를 한번만 통지
            OnEnemyHpChanged?.Invoke(_hp, _maxHealth);

            // 이번 피해로 처음 사망했는지 확인
            if (_hp <= 0f && !_isDead)
            {
                _isDead = true; // 사망 상태를 먼저 기록해 중복 사망 통지를 차단

                Died?.Invoke(); // 최초 사망시에만 이벤트 발생
            }

            // 최종 받은 피해량 반환
            return appliedDamage;
        }

        #region 기본 수치 값 버프/디버프

        /// <summary>
        /// 나중에 구체적으로 구현될, "기본 값"을 수정하는 버프 디버프를 모두 적용하는 함수이다.
        /// </summary>
        public void SetDefaults() {}

        // 최대 체력 변경
        public void SetDefaultHealth(float value)
        {
            _maxHealth += value;
        }

        // 이동 속도 변경
        public void SetDefaultSpeed(float value)
        {
            _speed += value;
        }

        // 공격력 변경
        public void SetDefaultAttackDamage(float value)
        {
            _attackDamage += value;
        }

        // 공격 속도 변경
        public void SetDefaultAttackSpeed(float value)
        {
            _attackSpeed += value;
        }
        
        // 사거리 변경
        public void SetDefaultAttackRange(float value)
        {
            _attackRange += value;
        }
        #endregion

        #region  실시간 수치 값 버프/디버프
        // 최대 체력 변경
        public void SetRuntimeHealth(float value)
        {
            _hp += value;
        }

        // 이동 속도 변경
        public void SetRuntimeSpeed(float value)
        {
            _sp += value;
        }

        // 공격력 변경
        public void SetRuntimeAttackDamage(float value)
        {
            _ad += value;
        }

        // 공격 속도 변경
        public void SetRuntimeAttackSpeed(float value)
        {
            _as += value;
        }
        
        // 사거리 변경
        public void SetRuntimeAttackRange(float value)
        {
            _range += value;
        }
        #endregion
    }
}
