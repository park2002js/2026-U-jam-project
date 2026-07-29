using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UJam.Runtime.Shop;

namespace UJam.Runtime.UI
{
    public sealed class UIPlayer : MonoBehaviour
    {
        [Header("Player Health References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private TMP_Text playerHPText;
        [SerializeField] private Image hpFillImage;

        [Header("Wallet UI Reference")]
        [SerializeField] private TMP_Text currencyText; // 재화를 표시할 TextMeshPro

        private void OnEnable()
        {
            // 1. 체력 이벤트 구독 및 초기화
            if (playerHealth != null)
            {
                playerHealth.OnHPChanged += UpdatePlayerHPUI;
                UpdatePlayerHPUI(playerHealth.CurrentHP, playerHealth.MaxHP);
            }

            // 2. 재화 이벤트 구독 및 초기화
            if (Wallet.Instance != null)
            {
                Wallet.Instance.OnCurrencyChanged += UpdateCurrencyUI;
                UpdateCurrencyUI(Wallet.Instance.Currency);
            }
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (playerHealth != null)
            {
                playerHealth.OnHPChanged -= UpdatePlayerHPUI;
            }

            if (Wallet.Instance != null)
            {
                Wallet.Instance.OnCurrencyChanged -= UpdateCurrencyUI;
            }
        }

        /// <summary>
        /// 체력 UI를 업데이트합니다.
        /// </summary>
        private void UpdatePlayerHPUI(int currentHP, int maxHP)
        {
            float fillRatio = maxHP > 0 ? (float)currentHP / maxHP : 0f;

            if (playerHPText != null)
            {
                playerHPText.text = $"{currentHP} / {maxHP}";
            }

            if (hpFillImage != null)
            {
                hpFillImage.fillAmount = fillRatio;
            }
        }

        /// <summary>
        /// 재화 UI를 업데이트합니다.
        /// </summary>
        private void UpdateCurrencyUI(long currentCurrency)
        {
            if (currencyText != null)
            {
                // 천 단위 콤마(,) 표시 예: 1000 -> 1,000 / 단순 숫자는 $"{currentCurrency}" 사용
                currencyText.text = $"{currentCurrency:N0}";
            }
        }
    }
}