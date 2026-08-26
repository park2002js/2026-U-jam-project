using TMPro;
using UnityEngine;
using UJam.Runtime.Shop;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// Player_Gold_UI에 부착하여 Wallet의 현재 재화를 TextMeshPro 텍스트에 표시합니다.
    /// </summary>
    public class UIPlayerGold : MonoBehaviour
    {
        [SerializeField] private Wallet _wallet;
        [SerializeField] private TMP_Text _currencyText;

        /// <summary>
        /// Wallet.OnCurrencyChanged를 구독하고 UI가 다시 열릴 때도 현재 재화를 표시합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_wallet != null) _wallet.OnGoldChanged += OnSetGold;
            OnSetGold(_wallet != null ? _wallet.Gold : 0L);
        }

        /// <summary>
        /// Wallet.Awake에서 시작 재화가 보정된 뒤 초기 표시를 동기화합니다.
        /// </summary>
        private void Start()
        {
            OnSetGold(_wallet != null ? _wallet.Gold : 0L);
        }

        /// <summary>
        /// UI가 닫히거나 제거되면 Wallet의 재화 변경 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (_wallet != null) _wallet.OnGoldChanged -= OnSetGold;
        }

        /// <summary>
        /// Wallet이 전달한 최신 재화를 천 단위 구분자가 있는 정수로 표시합니다.
        /// </summary>
        private void OnSetGold(long currency)
        {
            if (_currencyText != null) _currencyText.text = $"{currency:N0} $";
        }
    }
}
