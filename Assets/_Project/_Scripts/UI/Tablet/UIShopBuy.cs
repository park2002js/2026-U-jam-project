using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UJam.Runtime.Shop;
using Ujam.Runtime.Item;

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
        [SerializeField] private RectTransform _descriptionPopup;
        [SerializeField] private TMP_Text _descriptionText;

        private UIShopItem _hoveredItem;
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

        internal void ShowDescription(UIShopItem item, PointerEventData pointer)
        {
            if (string.IsNullOrWhiteSpace(item.Description)) return;
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)_descriptionPopup.parent, pointer.position, pointer.enterEventCamera, out Vector3 position)) return;

            _hoveredItem = item;
            _descriptionText.text = item.Description;
            _descriptionPopup.position = position;
            _descriptionPopup.gameObject.SetActive(true);
        }

        internal void HideDescription(UIShopItem item)
        {
            if (_hoveredItem != item) return;
            _hoveredItem = null;
            _descriptionPopup.gameObject.SetActive(false);
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
            UIItemReplacePrompt.Instance?.Cancel(); // 진열이 바뀌면 진행 중이던 교체 선택은 취소한다.
            _items = _shopManager.Reroll(ItemCount);
            RefreshItems();
        }

        /// <summary>
        /// 슬롯 구매가 성공한 경우에만 Sold Out으로 표시합니다. 잔액 부족 등 실패 시 진열을 유지합니다.
        /// 액티브 아이템인데 스킬칸이 가득 차 있으면 "제거할 아이템을 고르십시오"를 띄우고,
        /// 플레이어가 고른 스킬칸의 아이템과 교체하며 구매합니다.
        /// </summary>
        public void BuyItem(int slot)
        {
            if (!isActiveAndEnabled || _shopManager == null || _items == null || slot < 0 || slot >= _items.Count) return;
            string itemId = _items[slot];
            ItemMeta meta = ItemCatalog.GetMeta(itemId);
            if (ShopBuy.IsPlaceholder(itemId) || meta == null) return;

            UIItemReplacePrompt prompt = UIItemReplacePrompt.Instance;
            if (prompt != null) prompt.Cancel();

            switch (_shopManager.TryBuy(slot))
            {
                case PurchaseResult.Purchased:
                    MarkSoldOut(slot);
                    break;
                case PurchaseResult.NeedsSkillReplacement:
                    if (prompt == null)
                    {
                        Debug.LogWarning("[UIShopBuy] 스킬칸이 가득 찼지만 교체 선택 UI(UIItemReplacePrompt)가 없습니다.", this);
                        break;
                    }
                    prompt.Begin(meta.ItemSprite, skillSlot =>
                    {
                        // 선택하는 사이 진열이 바뀌지 않았을 때만 구매를 마친다.
                        if (!isActiveAndEnabled || _items == null || slot >= _items.Count || _items[slot] != itemId) return;
                        if (_shopManager.BuyItemReplacing(slot, skillSlot)) MarkSoldOut(slot);
                    });
                    break;
            }
        }

        private void MarkSoldOut(int slot)
        {
            _items[slot] = null;
            if (slot < _slots.Count && _slots[slot] != null) _slots[slot].SetSoldOut();
        }

        /// <summary>
        /// ShopManager에서 받은 ID 목록을 Catalog에서 일괄 조회하고, 같은 순번의 메타데이터를 UIShopItem에 전달합니다.
        /// 빈 칸이나 판매 완료 칸도 순번을 유지합니다. Grid Layout Group 설정은 변경하지 않습니다.
        /// </summary>
        private void RefreshItems()
        {
            ClearItems();
            if (_items == null) return;

            List<ItemMeta> items = ItemCatalog.GetMetaList(_items);
            for (int slot = 0; slot < _items.Count; slot++)
            {
                ItemMeta item = items[slot];
                if (item == null && !ShopBuy.IsPlaceholder(_items[slot])) Debug.LogWarning($"[UIShopBuy] '{_items[slot]}'에 해당하는 ItemMeta가 없습니다.", this);
                UIShopItem view = Instantiate(_itemPrefab, _itemRoot, false);
                view.SetItem(item, slot, BuyItem);
                if (_items[slot] == null) view.SetSoldOut();
                _slots.Add(view);
                view.gameObject.SetActive(true);
            }
        }

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
