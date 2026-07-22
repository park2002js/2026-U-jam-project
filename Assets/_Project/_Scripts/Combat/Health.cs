using System;
using UnityEngine;

namespace UJam.Runtime.Combat
{
    public sealed class Health : MonoBehaviour
    {
        // 대상이 가질 수 있는 최대 체력
        [SerializeField, Min(0.0001f)] private float _maxHealth = 100f;

        // 현재 체력 상태
        private float _currentHealth;

        // 사망 전환이 이미 발생했는지 나타내는 상태
        private bool _isDead;

        // 실제로 적용된 피해량을 알리는 이벤트
        public event Action<float> DamageApplied;

        // 생존에서 사망으로 처음 전환될 때 알리는 이벤트
        public event Action Died;

        // 외부에서 읽는 최대 체력
        public float MaxHealth
        {
            get
            {
                // 현재 설정된 최대 체력 반환
                return _maxHealth;
            }
        }

        // 외부에서 읽는 현재 체력
        public float CurrentHealth
        {
            get
            {
                // 현재 clamp된 체력 반환
                return _currentHealth;
            }
        }

        // 외부에서 읽는 사망 상태
        public bool IsDead
        {
            get
            {
                // 중복 피해를 막는 사망 상태 반환
                return _isDead;
            }
        }

        // Component가 활성화될 때 최대 체력으로 초기화
        private void Awake()
        {
            // 최대 체력이 양의 유한 값인지 확인
            if (!IsPositiveFinite(_maxHealth))
            {
                // 잘못된 최대 체력을 안전한 기본값으로 교체
                _maxHealth = 1f;
            }

            // 검증된 최대 체력으로 현재 체력 초기화
            _currentHealth = _maxHealth;

            // 생존 상태로 수명주기 시작
            _isDead = false;
        }

        // 외부 소유자가 최대 체력과 현재 체력을 같은 초기값으로 설정
        public bool SetStartHealth(float health)
        {
            // 초기 체력이 양의 유한 값인지 확인
            if (!IsPositiveFinite(health))
            {
                // 잘못된 초기 체력 설정 실패 반환
                return false;
            }

            // 외부에서 제공한 값을 최대 체력으로 저장
            _maxHealth = health;

            // 현재 체력을 새로운 최대 체력으로 초기화
            _currentHealth = health;

            // 초기화된 Health를 생존 상태로 설정
            _isDead = false;

            // 초기 체력 설정 성공 반환
            return true;
        }

        // 실제 피해량을 체력에 적용하고 감소한 체력 반환
        public float ApplyDamage(float amount)
        {
            // 이미 사망한 대상은 다시 피해를 받지 않음
            if (_isDead)
            {
                // 사망 상태의 실제 피해 0 반환
                return 0f;
            }

            // 피해량이 양의 유한 값인지 확인
            if (!IsPositiveFinite(amount))
            {
                // 유효하지 않은 실제 피해 0 반환
                return 0f;
            }

            // 피해 적용 전 체력 저장
            float previousHealth = _currentHealth;

            // 실제 체력을 0과 최대 체력 사이로 제한
            _currentHealth = Mathf.Clamp(_currentHealth - amount, 0f, _maxHealth);

            // clamp 이후 실제로 줄어든 체력 계산
            float actualDamage = previousHealth - _currentHealth;

            // 실제 체력 감소가 없으면 피해 이벤트를 보내지 않음
            if (actualDamage <= 0f)
            {
                // 감소하지 않은 실제 피해 0 반환
                return 0f;
            }

            // 실제 피해량을 한 번만 통지
            DamageApplied?.Invoke(actualDamage);

            // 이번 피해로 처음 사망했는지 확인
            if (_currentHealth <= 0f && !_isDead)
            {
                // 사망 상태를 먼저 기록해 중복 사망 통지를 차단
                _isDead = true;

                // 최초 사망 이벤트만 통지
                Died?.Invoke();
            }

            // 최종 감소한 실제 피해량 반환
            return actualDamage;
        }

        // 체력과 피해 값이 양의 유한 값인지 확인
        private static bool IsPositiveFinite(float value)
        {
            // NaN과 무한대 또는 0 이하 값인지 확인
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                // 유효하지 않은 값 결과 반환
                return false;
            }

            // 양의 유한 값 결과 반환
            return true;
        }
    }
}
