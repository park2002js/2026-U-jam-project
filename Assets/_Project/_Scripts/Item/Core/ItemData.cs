using System;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    public enum ItemKind { Active, Passive }
    public enum ItemTrigger { Shooting, SkillUse, HealthChanged }
    public enum ItemPreviewMode { None, Ground }

    public sealed class ItemMeta
    {
        public string GUID { get; }
        public string Name { get; }
        public string Description { get; }
        public int Price { get; }
        public ItemKind Kind { get; }
        public Sprite ItemSprite { get; set; }
        public Sprite ActiveSkillIcon { get; set; }
        // 파괴되었거나 불러오지 못한 스킬 아이콘은 아이템 이미지로 대체한다.
        public Sprite SkillIcon => ActiveSkillIcon != null ? ActiveSkillIcon : ItemSprite;

        public ItemMeta(string guid, string name, string description, int price, ItemKind kind,
            Sprite itemSprite = null, Sprite activeSkillIcon = null)
        {
            if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(name) || price < 0)
                throw new ArgumentException("아이템 GUID, 이름, 가격을 확인하세요.");
            GUID = guid; Name = name; Description = description ?? ""; Price = price; Kind = kind;
            ItemSprite = itemSprite; ActiveSkillIcon = activeSkillIcon;
        }
    }

    public sealed class ItemData
    {
        public const string NullId = "Item_null";
        public ItemMeta Meta { get; }
        public ItemRuntime Runtime { get; }
        // 기존 Shop/Inventory UI에서 사용하던 읽기 API.
        public string Id => Meta.GUID;
        public string DisplayName => Meta.Name;
        public Sprite Icon => Meta.ItemSprite;
        public int Cost => Meta.Price;
        public Sprite SkillIcon => Meta.SkillIcon;
        public float CoolTime => Runtime.Cooldown;

        public ItemData(ItemMeta meta, ItemRuntime runtime)
        {
            Meta = meta ?? throw new ArgumentNullException(nameof(meta));
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            if (runtime.Item != null) throw new ArgumentException("Runtime은 아이템마다 새로 생성해야 합니다.");
            if (meta.Kind == ItemKind.Active && runtime.Trigger != ItemTrigger.SkillUse)
                throw new ArgumentException("액티브 아이템은 SkillUse 트리거만 사용합니다.");
            runtime.Item = this;
        }

        // 호출마다 새 Runtime/Effect를 생성한다. 메타만 필요하면 Catalog.GetMeta를 사용한다.
        public static ItemData Load(string guid) => ItemCatalog.Create(guid);
    }
}
