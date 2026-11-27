using System;
using TMPro;
using Ujam.Runtime.Item;
using UnityEngine;
using UnityEngine.UI;
using UJam.Runtime.Shop;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 상점 상품 컴포넌트 프리팹의 루트에 부착하여 내부 아이콘·가격·구매 버튼을 UIShopBuy와 연결합니다.
    /// 가격은 사각 박스(구매 버튼) 안에 코인 아이콘과 함께 표시되며, 이 박스를 눌러야 결제됩니다.
    /// </summary>
    public class UIShopItem : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private Button _buyButton;
        [SerializeField, Tooltip("선택: 가격 옆 코인 아이콘입니다. Sold Out일 때 숨깁니다.")]
        private GameObject _coinIcon;
        [SerializeField] private Color _affordableColor = new Color(0.17f, 0.11f, 0.05f, 1f);
        [SerializeField] private Color _unaffordableColor = new Color(0.62f, 0.12f, 0.1f, 1f);

        private ItemMeta _item;
        private int _slot;
        private Action<int> _buy;
        private bool _soldOut;

        internal string Description => _item?.Description;

        /// <summary>상품 구매 버튼에 자신의 슬롯을 전달하는 클릭 함수를 등록하고 재화 변화에 따라 가격 색을 갱신합니다.</summary>
        private void OnEnable()
        {
            if (_buyButton != null) _buyButton.onClick.AddListener(Buy);
            if (Wallet.Instance != null) Wallet.Instance.OnGoldChanged += OnGoldChanged;
            RefreshAffordable();
        }

        /// <summary>상품 UI가 숨겨지거나 제거될 때 구매 버튼과 재화 구독을 해제합니다.</summary>
        private void OnDisable()
        {
            if (_buyButton != null) _buyButton.onClick.RemoveListener(Buy);
            if (Wallet.Instance != null) Wallet.Instance.OnGoldChanged -= OnGoldChanged;
        }

        /// <summary>
        /// UIShopBuy가 Catalog에서 조회한 메타데이터의 아이콘과 가격을 표시합니다.
        /// 아이콘을 지정하지 않은 아이템도 ItemMeta 생성자가 선택한 기본 null 이미지를 표시합니다.
        /// 메타데이터가 null이면 아이콘을 비우고 가격을 '-'로 표시하며 구매를 차단합니다.
        /// </summary>
        public void SetItem(ItemMeta item, int slot, Action<int> buy)
        {
            _item = item;
            _slot = slot;
            _buy = item != null ? buy : null;
            _soldOut = false;
            if (_icon != null)
            {
                _icon.sprite = item != null ? item.ItemSprite : null;
                _icon.enabled = _icon.sprite != null;
                _icon.color = Color.white;
            }
            if (_priceText != null) _priceText.text = item != null ? $"{item.Price:N0}" : "-";
            if (_coinIcon != null) _coinIcon.SetActive(item != null);
            if (_buyButton != null) _buyButton.interactable = _buy != null;
            RefreshAffordable();
        }

        /// <summary>유효한 상품의 클릭만 UIShopBuy.BuyItem에 전달하며 구매 성공 여부는 상점 시스템에 맡깁니다.</summary>
        private void Buy()
        {
            if (_item != null) _buy?.Invoke(_slot);
        }

        /// <summary>판매 완료된 ID 칸을 Sold Out으로 표시하고 구매 버튼을 비활성화합니다.</summary>
        public void SetSoldOut()
        {
            _buy = null;
            _soldOut = true;
            if (_priceText != null)
            {
                _priceText.text = "SOLD OUT";
                _priceText.color = _affordableColor;
                // 코인 아이콘이 사라진 자리까지 문구를 가운데로 넓힙니다.
                _priceText.rectTransform.offsetMin = new Vector2(14f, _priceText.rectTransform.offsetMin.y);
            }
            if (_coinIcon != null) _coinIcon.SetActive(false);
            if (_buyButton != null) _buyButton.interactable = false;
            if (_icon != null) _icon.color = new Color(1f, 1f, 1f, 0.35f);
        }

        private void OnGoldChanged(long gold) => RefreshAffordable();

        /// <summary>현재 재화로 살 수 없는 상품은 가격을 붉게 표시합니다(구매 판단은 상점 시스템이 합니다).</summary>
        private void RefreshAffordable()
        {
            if (_priceText == null || _soldOut || _item == null) return;
            long gold = Wallet.Instance != null ? Wallet.Instance.Gold : long.MaxValue;
            _priceText.color = _item.Price <= gold ? _affordableColor : _unaffordableColor;
        }
    }
}
