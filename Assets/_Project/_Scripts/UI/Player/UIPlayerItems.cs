using System.Collections.Generic;
using UnityEngine;
using Ujam.Runtime.Item;
using UJam.Runtime.Player;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// Player_Items_UI에 부착하여 PlayerInventory가 전달한 ID 목록의 보유 아이템을 고정된 정사각 슬롯 격자에 표시합니다.
    /// 슬롯은 시작 시 _frameCount개를 만들고, 보유 아이템을 앞에서부터 채웁니다.
    /// 인벤토리 보유 한도를 넘는 슬롯은 숨깁니다(_hideSlotsOverCapacity를 끄면 잠김으로 표시).
    /// </summary>
    public class UIPlayerItems : MonoBehaviour
    {
        [SerializeField] private PlayerInventory _inventory;

        [SerializeField, Tooltip("Grid Layout Group이 있는 오브젝트를 할당합니다. 기존 레이아웃 설정은 변경하지 않습니다.")]
        private Transform _root;

        [SerializeField, Tooltip("루트에 UIItemIcon이 부착된 UI 컴포넌트 프리팹을 할당합니다.")]
        private UIItemIcon _itemPrefab;

        [SerializeField, Min(1), Tooltip("아이템 목록 창에 표시할 정사각 슬롯 수입니다(문서 기준 4x5 = 20).")]
        private int _frameCount = 20;

        [SerializeField, Tooltip("선택: '보유 n / 최대' 형식으로 보유 수를 표시할 텍스트입니다.")]
        private TMPro.TMP_Text _capacityText;

        [SerializeField, Tooltip("켜면 인벤토리 보유 한도를 넘는 칸은 숨깁니다. 끄면 잠김(어두운 프레임)으로 표시합니다.")]
        private bool _hideSlotsOverCapacity = true;

        private readonly List<UIItemIcon> _frames = new List<UIItemIcon>();

        // 최초 초기화에서만 Editor에 미리 배치한 자식들을 제거하고 고정 슬롯을 만든다. OnEnable의 표시보다 먼저 실행된다.
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

            if (_itemPrefab == null) return;
            for (int i = 0; i < _frameCount; i++)
            {
                UIItemIcon frame = Instantiate(_itemPrefab, _root, false);
                frame.name = $"Item_Slot_{i + 1:00}";
                frame.SetIcon(null);
                frame.gameObject.SetActive(true);
                _frames.Add(frame);
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
        /// 전달받은 ID를 ItemCatalog에서 조회하여 앞 슬롯부터 아이콘을 채웁니다. 빈 칸은 건너뛰고, 보유 한도 밖 슬롯은 잠급니다.
        /// </summary>
        private void OnSetItems(IReadOnlyList<string> itemIds)
        {
            int capacity = itemIds != null ? itemIds.Count : 0;
            List<Sprite> sprites = new List<Sprite>();
            if (itemIds != null)
            {
                foreach (string id in itemIds)
                {
                    if (id == Item.NullId) continue;
                    ItemMeta item = ItemCatalog.GetMeta(id);
                    if (item == null)
                    {
                        Debug.LogWarning($"[UIPlayerItems] '{id}'의 ItemMeta가 없습니다.", this);
                        continue;
                    }
                    sprites.Add(item.ItemSprite);
                }
            }

            for (int i = 0; i < _frames.Count; i++)
            {
                UIItemIcon frame = _frames[i];
                if (frame == null) continue;
                bool locked = i >= capacity;
                frame.gameObject.SetActive(!locked || !_hideSlotsOverCapacity);
                frame.SetLocked(locked);
                if (!locked) frame.SetIcon(i < sprites.Count ? sprites[i] : null);
            }

            if (_capacityText != null) _capacityText.text = $"보유  {sprites.Count} / {capacity}";
        }
    }
}
