using System.Collections.Generic;
using UnityEngine;

// 모든 Item들의 기본 원형입니다.

namespace UJam.Runtime.Item
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Game/Items/Item")]
    public class ItemData : ScriptableObject
    {
        public const string NullId = "Item_null";
        public const string ResourceFolder = "Items";

        [Header("식별 정보")]
        [SerializeField] private string id;

        [Header("기본 정보")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private int cost;
        
        [Header("효과 SO 할당")]
        [SerializeField] private List<ItemEffect> effects = new();

        // 외부 식별용
        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int Cost => cost;
        public IReadOnlyList<ItemEffect> Effects => effects;

        // Shop, Inventory, UI 모두 Assets/**/Resources/Items/{id}.asset을 같은 규칙으로 조회한다.
        public static ItemData Load(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || itemId.Contains('/') || itemId.Contains('\\')) return null;
            ItemData item = Resources.Load<ItemData>($"{ResourceFolder}/{itemId}");
            if (item != null && item.Id == itemId) return item;
            Debug.LogWarning($"[ItemData] Resources/{ResourceFolder}/{itemId}.asset이 없거나 에셋의 Id가 일치하지 않습니다.");
            return null;
        }
    }
}
