using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UJam.Runtime.Shop;
using UJam.Runtime.Item;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// Tablet_ItemBuy_UI에 부착하여 ShopManager → ShopBuy의 상품 목록과 구매 결과를 표시합니다.
    /// 가격 계산, 재화 차감, 아이템 지급 여부는 상점 시스템의 책임이며 UI에서 처리하지 않습니다.
    /// </summary>
    public class UIShopBuy : MonoBehaviour
    {
        [SerializeField] private Transform _itemRoot;
        [SerializeField] private UIShopItem _itemPrefab;
        private const int ItemCount = 6; // 아이템 진열대의 칸 갯수

        [SerializeField] private Button _rerollButton;

        private ShopManager _shopManager;
        private List<string> _items;
        private readonly List<UIShopItem> _slots = new List<UIShopItem>();

        /// <summary>
        /// 리롤 버튼의 클릭만 연결합니다. 구매 화면을 다시 여는 것만으로 상점 목록을 초기화하지 않습니다.
        /// </summary>
        private void OnEnable()
        {
            if (_rerollButton == null) return;
            _rerollButton.onClick.AddListener(Reroll);
            _rerollButton.interactable = _shopManager != null && _items != null;
        }

        /// <summary>
        /// 구매 화면이 숨겨지면 리롤 버튼 구독을 해제하며 현재 상품 목록은 보존합니다.
        /// </summary>
        private void OnDisable()
        {
            if (_rerollButton != null) _rerollButton.onClick.RemoveListener(Reroll);
        }

        /// <summary>
        /// UITablet.OnEnable에서 기존 진열 또는 최초 상품을 요청합니다. 싱글톤 준비 전에는 실패를 반환합니다.
        /// </summary>
        public bool InitializeShop()
        {
            _items = null;
            ClearItems();
            _shopManager = ShopManager.Instance;
            if (_rerollButton != null) _rerollButton.interactable = false;
            if (_shopManager == null || !_shopManager.IsInitialized || _itemRoot == null || _itemPrefab == null) return false;

            _items = _shopManager.OpenShop(ItemCount);
            RefreshItems();
            if (_rerollButton != null) _rerollButton.interactable = true;
            return true;
        }

        /// <summary>
        /// ItemReRoll_Btn의 클릭을 ShopManager에 전달하고 ShopBuy가 돌려준 새 상품으로 표시를 갱신합니다.
        /// </summary>
        public void Reroll()
        {
            if (!isActiveAndEnabled || _shopManager == null || _items == null) return;
            _items = _shopManager.Reroll(ItemCount);
            RefreshItems();
        }

        /// <summary>
        /// 슬롯 구매가 성공한 경우에만 Sold Out으로 표시합니다. 잔액 부족 등 실패 시 진열을 유지합니다.
        /// </summary>
        public void BuyItem(int slot)
        {
            if (!isActiveAndEnabled || _shopManager == null || _items == null || slot < 0 || slot >= _items.Count) return;
            string itemId = _items[slot];
            if (ShopBuy.IsPlaceholder(itemId) || FindItem(itemId) == null) return;
            if (!_shopManager.BuyItem(slot)) return;

            _items[slot] = null;
            if (slot < _slots.Count && _slots[slot] != null) _slots[slot].SetSoldOut();
        }

        /// <summary>
        /// ShopBuy의 ID를 ItemData에 연결하여 UI 컴포넌트 프리팹을 복제합니다. Grid Layout Group 설정은 변경하지 않습니다.
        /// </summary>
        private void RefreshItems()
        {
            ClearItems();
            if (_items == null) return;
            for (int slot = 0; slot < _items.Count; slot++)
            {
                ItemData item = FindItem(_items[slot]);
                if (item == null && !string.IsNullOrWhiteSpace(_items[slot])) Debug.LogWarning($"[UIShopBuy] '{_items[slot]}'에 해당하는 ItemData가 없습니다.", this);
                UIShopItem view = Instantiate(_itemPrefab, _itemRoot, false);
                view.SetItem(item, slot, BuyItem);
                if (_items[slot] == null) view.SetSoldOut();
                _slots.Add(view);
                view.gameObject.SetActive(true);
            }
        }

        /// <summary>ShopBuy의 문자열 ID에 대응하는 아이콘과 가격용 ItemData를 찾습니다.</summary>
        private ItemData FindItem(string itemId) => itemId == null ? null : ItemData.Load(itemId);

        /// <summary>Editor 미리보기 슬롯을 포함한 Layout 하위 자식을 모두 정리합니다. 프리팹 에셋은 유지합니다.</summary>
        private void ClearItems()
        {
            if (_itemRoot != null)
            {
                for (int i = _itemRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = _itemRoot.GetChild(i);
                    child.gameObject.SetActive(false);
                    child.SetParent(null, false);
                    Destroy(child.gameObject);
                }
            }
            _slots.Clear();
        }

        /// <summary>구매 UI가 제거되면 생성했던 상품 UI를 정리합니다.</summary>
        private void OnDestroy()
        {
            ClearItems();
        }
    }
}
