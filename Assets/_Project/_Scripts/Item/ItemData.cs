using System.Collections.Generic;
using UnityEngine;

// 모든 Item들의 기본 원형입니다.

namespace UJam.Runtime.Item
{
    [CreateAssetMenu(
        fileName = "NewItem",
        menuName = "Game/Items/Item"
    )]
    public class ItemData : ScriptableObject
    {
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
    }
}
