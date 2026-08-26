using System;
using UnityEngine;
using UJam.Runtime.Combat;

namespace UJam.Runtime.Player
{
    public class PlayerStatus : MonoBehaviour
    {
        [SerializeField, Min(0.0001f)] private float _attackDamage = 10f;
        [SerializeField, Min(0.0001f)] private float _maxHealth = 100f;

        private float _currentHealth;

        public event Action<float, float> HealthChanged;

        public float AttackDamage { get { return _attackDamage; } }
        public float MaxHealth { get { return _maxHealth; } }
        public float CurrentHealth { get { return _currentHealth; } }

        private void Awake()
        {
            if (!IsPositiveFinite(_attackDamage))
            {
                _attackDamage = 1f;
            }

            if (!IsPositiveFinite(_maxHealth))
            {
                _maxHealth = 1f;
            }

            _currentHealth = _maxHealth;
            Debug.Log($"[PlayerStatus] 초기 HP: {_currentHealth:0.##}/{_maxHealth:0.##}", this);
        }

        public float TakeDamage(DamageInfo info)
        {
            if (!IsPositiveFinite(info.Damage) || _currentHealth <= 0f)
            {
                return 0f;
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - info.Damage);
            float appliedDamage = previousHealth - _currentHealth;
            Debug.Log($"[PlayerStatus] HP: {_currentHealth:0.##}/{_maxHealth:0.##} ({_currentHealth / _maxHealth * 100f:0.#}%), 받은 피해: {appliedDamage:0.##}, 공격자: {info.Source}", this);

            HealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0f)
            {
                if (GameManager.Instance != null) GameManager.Instance.GameOver();
                else Debug.LogError("[PlayerStatus] HP가 0이지만 GameManager.Instance가 없어 Game Over를 전달하지 못했습니다.", this);
            }

            return appliedDamage;
        }

        public bool SetAttackDamage(float value)
        {
            if (!IsPositiveFinite(value))
            {
                return false;
            }

            _attackDamage = value;
            return true;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
