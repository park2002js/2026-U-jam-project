using System.Collections.Generic;
using UnityEngine;
using Ujam.Runtime.Item;
using UJam.Runtime.Player;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// Player_Items_UI에 부착하여 PlayerInventory가 전달한 ID 목록의 보유 아이템을 UI 프리팹으로 표시합니다.
    /// </summary>
    public class UIPlayerItems : MonoBehaviour
    {
        [SerializeField] private PlayerInventory _inventory;
        
        [SerializeField, Tooltip("Grid Layout Group이 있는 오브젝트를 할당합니다. 기존 레이아웃 설정은 변경하지 않습니다.")]
        private Transform _root;
        
        [SerializeField, Tooltip("루트에 UIItemIcon이 부착된 UI 컴포넌트 프리팹을 할당합니다.")]
        private UIItemIcon _itemPrefab;
        
        private readonly List<UIItemIcon> _spawnedIcons = new List<UIItemIcon>();

        // 최초 초기화에서만 Editor에 미리 배치한 자식들을 제거한다. OnEnable의 보유 아이콘 생성보다 먼저 실행된다.
        private void Awake()
        {
            if (_root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                Transform child = _root.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// PlayerInventory.OnItemsChanged를 구독하고 현재 보유 목록을 표시합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_inventory != null) _inventory.OnItemsChanged += OnSetItems;
            OnSetItems(_inventory != null ? _inventory.Items : null);
        }

        /// <summary>
        /// UI가 닫히거나 제거되면 PlayerInventory의 목록 변경 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (_inventory != null) _inventory.OnItemsChanged -= OnSetItems;
        }

        /// <summary>
        /// UIPlayerItems가 제거될 때 자신이 생성한 UI만 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            ClearItems();
        }

        /// <summary>
        /// 전달받은 ID를 ItemCatalog에서 조회하여 아이콘을 표시합니다. 빈 칸은 표시하지 않습니다.
        /// </summary>
        private void OnSetItems(IReadOnlyList<string> itemIds)
        {
            ClearItems();
            if (itemIds == null || _root == null || _itemPrefab == null) return;

            foreach (string id in itemIds)
            {
                if (id == Item.NullId) continue;
                ItemMeta item = ItemCatalog.GetMeta(id);
                if (item == null)
                {
                    Debug.LogWarning($"[UIPlayerItems] '{id}'의 ItemMeta가 없습니다.", this);
                    continue;
                }

                UIItemIcon icon = Instantiate(_itemPrefab, _root, false);
                icon.SetIcon(item.ItemSprite);
                icon.gameObject.SetActive(true);
                _spawnedIcons.Add(icon);
            }
        }

        /// <summary>
        /// 생성한 아이템 UI를 즉시 레이아웃에서 제외한 뒤 제거하며, Root의 기존 자식은 유지합니다.
        /// </summary>
        private void ClearItems()
        {
            foreach (UIItemIcon icon in _spawnedIcons)
            {
                if (icon == null) continue;
                icon.gameObject.SetActive(false);
                Destroy(icon.gameObject);
            }
            _spawnedIcons.Clear();
        }
    }
}
