using System;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public interface IStatChange
    {
        // 외부 버프와 디버프가 전달받은 Enemy 스탯을 변경하는 경계
        void Apply(EnemyStatus status);
    }

    [Serializable]
    public sealed class EnemyStatus
    {
        // Enemy가 가질 수 있는 최대 체력
        [SerializeField, Min(0.0001f)] private float _maxHealth = 100f;

        // Navigation 이동 정책에서 사용할 이동 속도
        [SerializeField, Min(0f)] private float _speed = 3.5f;

        // 구체 공격 로직에서 사용할 기본 피해량
        [SerializeField, Min(0f)] private float _damage = 10f;

        // 연속 공격 사이의 기본 대기 시간
        [SerializeField, Min(0f)] private float _cooldown = 0.5f;

        // Grid 사거리 판정과 Navigation 요청에서 사용할 공격 거리
        [SerializeField, Min(0f)] private float _range = 1.5f;

        // 이동 중 점프 통과 가능 여부
        [SerializeField] private bool _canJump;

        // 이동 중 비행 통과 가능 여부
        [SerializeField] private bool _canFly;

        // 이동 중 장애물 파괴 가능 여부
        [SerializeField] private bool _canBreak;

        // Combat 공격자 계약에서 사용할 진영 식별자
        [SerializeField] private string _factionId = "Enemy";

        // Combat 공격자 계약에서 사용할 공격 식별자
        [SerializeField] private string _attackId = "Default";

        // Combat 피해 계약에서 사용할 피해 종류 식별자
        [SerializeField] private string _damageTypeId = "Physical";

        // Inspector와 파생 Enemy가 사용할 기본 스탯 생성자
        public EnemyStatus()
        {
            // 기본 필드 값을 유효 범위로 정리
            Sanitize();
        }

        // 파생 Enemy가 모든 초기 스탯을 명시하는 생성자
        public EnemyStatus(
            float maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackCooldown,
            float attackRange,
            bool canJump = false,
            bool canFly = false,
            bool canBreakObstacles = false,
            string factionId = "Enemy",
            string attackId = "Default",
            string damageTypeId = "Physical")
        {
            _maxHealth = maxHealth;
            _speed = moveSpeed;
            _damage = attackDamage;
            _cooldown = attackCooldown;
            _range = attackRange;
            _canJump = canJump;
            _canFly = canFly;
            _canBreak = canBreakObstacles;
            _factionId = factionId;
            _attackId = attackId;
            _damageTypeId = damageTypeId;

            // 전달받은 초기 값을 유효 범위로 정리
            Sanitize();
        }

        // 현재 최대 체력
        public float MaxHealth
        {
            get
            {
                // Health 초기화에 사용할 최대 체력 반환
                return _maxHealth;
            }
        }

        // 현재 이동 속도
        public float Speed
        {
            get
            {
                // 이동 정책에 전달할 속도 반환
                return _speed;
            }
        }

        // 현재 기본 공격 피해량
        public float Damage
        {
            get
            {
                // 구체 공격 로직에 전달할 피해량 반환
                return _damage;
            }
        }

        // 현재 공격 대기 시간
        public float Cooldown
        {
            get
            {
                // 구체 공격 로직에 전달할 대기 시간 반환
                return _cooldown;
            }
        }

        // 현재 공격 사거리
        public float Range
        {
            get
            {
                // Grid 사거리 계산에 전달할 거리 반환
                return _range;
            }
        }

        // 현재 점프 통과 가능 여부
        public bool CanJump
        {
            get
            {
                // Navigation 통과 프로필의 점프 값 반환
                return _canJump;
            }
        }

        // 현재 비행 통과 가능 여부
        public bool CanFly
        {
            get
            {
                // Navigation 통과 프로필의 비행 값 반환
                return _canFly;
            }
        }

        // 현재 장애물 파괴 가능 여부
        public bool CanBreak
        {
            get
            {
                // Navigation 통과 프로필의 장애물 파괴 값 반환
                return _canBreak;
            }
        }

        // 현재 진영 식별자
        public string FactionId
        {
            get
            {
                // Combat 공격자 계약에 전달할 진영 반환
                return _factionId;
            }
        }

        // 현재 공격 식별자
        public string AttackId
        {
            get
            {
                // Combat 공격자 계약에 전달할 공격 ID 반환
                return _attackId;
            }
        }

        // 현재 피해 종류 식별자
        public string DamageTypeId
        {
            get
            {
                // DamageInfo 생성에 전달할 피해 종류 반환
                return _damageTypeId;
            }
        }

        // 최대 체력을 버프와 디버프가 변경하는 경계
        public void SetHealth(float value)
        {
            // 양의 유한 값만 최대 체력으로 사용
            _maxHealth = SafePositive(value, 1f);
        }

        // 이동 속도를 버프와 디버프가 변경하는 경계
        public void SetMove(float value)
        {
            // 음수가 아닌 유한 값만 이동 속도로 사용
            _speed = SafeZero(value);
        }

        // 공격 피해량을 버프와 디버프가 변경하는 경계
        public void SetDamage(float value)
        {
            // 음수가 아닌 유한 값만 공격 피해량으로 사용
            _damage = SafeZero(value);
        }

        // 공격 대기 시간을 버프와 디버프가 변경하는 경계
        public void SetCooldown(float value)
        {
            // 음수가 아닌 유한 값만 공격 대기 시간으로 사용
            _cooldown = SafeZero(value);
        }

        // 공격 사거리를 버프와 디버프가 변경하는 경계
        public void SetRange(float value)
        {
            // 음수가 아닌 유한 값만 공격 사거리로 사용
            _range = SafeZero(value);
        }

        // 이동 통과 능력을 버프와 디버프가 변경하는 경계
        public void SetTravel(bool canJump, bool canFly, bool canBreakObstacles)
        {
            _canJump = canJump;
            _canFly = canFly;
            _canBreak = canBreakObstacles;
        }

        // 공격 식별 정보를 파생 Enemy가 변경하는 경계
        public void SetIds(string factionId, string attackId, string damageTypeId)
        {
            _factionId = SafeId(factionId, "Enemy");
            _attackId = SafeId(attackId, "Default");
            _damageTypeId = SafeId(damageTypeId, "Physical");
        }

        // 현재 스탯을 Enemy 인스턴스 전용 복사본으로 생성
        public EnemyStatus Copy()
        {
            // 모든 현재 값을 보존한 독립 스탯 생성
            EnemyStatus copy = new EnemyStatus(
                _maxHealth,
                _speed,
                _damage,
                _cooldown,
                _range,
                _canJump,
                _canFly,
                _canBreak,
                _factionId,
                _attackId,
                _damageTypeId);

            // 독립 스탯 복사본 반환
            return copy;
        }

        // 외부 Modifier 적용 뒤 모든 스탯을 다시 검증하는 경계
        public void Sanitize()
        {
            _maxHealth = SafePositive(_maxHealth, 1f);
            _speed = SafeZero(_speed);
            _damage = SafeZero(_damage);
            _cooldown = SafeZero(_cooldown);
            _range = SafeZero(_range);
            _factionId = SafeId(_factionId, "Enemy");
            _attackId = SafeId(_attackId, "Default");
            _damageTypeId = SafeId(_damageTypeId, "Physical");
        }

        // 양의 유한 스탯을 검증하고 잘못된 값을 기본값으로 교체
        private static float SafePositive(float value, float fallback)
        {
            // 사용할 수 없는 양수 값인지 확인
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                // 안전한 양수 기본값 반환
                return fallback;
            }

            // 검증된 양수 값 반환
            return value;
        }

        // 음수가 아닌 유한 스탯을 검증하고 잘못된 값을 0으로 교체
        private static float SafeZero(float value)
        {
            // 사용할 수 없는 비음수 값인지 확인
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                // 안전한 0 값 반환
                return 0f;
            }

            // 검증된 비음수 값 반환
            return value;
        }

        // 비어 있지 않은 식별자를 검증하고 기본 식별자로 교체
        private static string SafeId(string value, string fallback)
        {
            // 비어 있는 식별자인지 확인
            if (string.IsNullOrWhiteSpace(value))
            {
                // 안전한 기본 식별자 반환
                return fallback;
            }

            // 검증된 식별자 반환
            return value;
        }
    }
}
