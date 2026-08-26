using System;
using TMPro;
using UJam.Runtime.Item;
using UnityEngine;
using UnityEngine.UI;
using UJam.Runtime.Shop;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 상점 상품 컴포넌트 프리팹의 루트에 부착하여 내부 아이콘·가격·구매 버튼을 UIShopBuy와 연결합니다.
    /// </summary>
    public class UIShopItem : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private Button _buyButton;

        private ItemData _item;
        private int _slot;
        private Action<int> _buy;

        /// <summary>상품 구매 버튼에 자신의 슬롯을 전달하는 클릭 함수를 등록합니다.</summary>
        private void OnEnable()
        {
            if (_buyButton != null) _buyButton.onClick.AddListener(Buy);
        }

        /// <summary>상품 UI가 숨겨지거나 제거될 때 구매 버튼 구독을 해제합니다.</summary>
        private void OnDisable()
        {
            if (_buyButton != null) _buyButton.onClick.RemoveListener(Buy);
        }

        /// <summary>
        /// ItemData의 아이콘과 가격을 표시합니다. Item_null은 아이콘만 표시하고 구매를 차단합니다.
        /// </summary>
        public void SetItem(ItemData item, int slot, Action<int> buy)
        {
            _item = item;
            _slot = slot;
            _buy = item != null && !ShopBuy.IsPlaceholder(item.Id) ? buy : null;
            if (_icon != null)
            {
                _icon.sprite = item != null ? item.Icon : null;
                _icon.enabled = _icon.sprite != null;
            }
            if (_priceText != null) _priceText.text = item != null && !ShopBuy.IsPlaceholder(item.Id) ? $"{item.Cost:N0} $" : "-";
            if (_buyButton != null) _buyButton.interactable = _buy != null;
        }

        /// <summary>유효한 상품의 클릭만 UIShopBuy.BuyItem에 전달하며 구매 성공 여부는 상점 시스템에 맡깁니다.</summary>
        private void Buy()
        {
            if (_item != null) _buy?.Invoke(_slot);
        }

        public void SetSoldOut()
        {
            _buy = null;
            if (_priceText != null) _priceText.text = "Sold Out";
            if (_buyButton != null) _buyButton.interactable = false;
        }
    }
}
