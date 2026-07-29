using System;
using UnityEngine;

namespace UJam.Runtime
{
    public class PlayerHealth : MonoBehaviour
    {
        // currentHP, maxHP 두 인자를 전달하는 Action 이벤트
        public event Action<int, int> OnHPChanged;

        [SerializeField] private int maxHP = 100;
        private int currentHP;

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;

        private void Awake()
        {
            currentHP = maxHP;
        }

        /// <summary>
        /// 데미지를 받거나 체력을 변경할 때 호출
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);

            // 구독된 모든 이벤트 리스너(UI 등)에 전달
            OnHPChanged?.Invoke(currentHP, maxHP);
        }

        /// <summary>
        /// 체력 회복 시 호출
        /// </summary>
        public void Heal(int amount)
        {
            currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);

            OnHPChanged?.Invoke(currentHP, maxHP);
        }
    }
}