using System;
using UJam.Runtime.Player;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    public enum ItemKind { Active, Passive }
    public enum ItemCastType { Instant, Normal }
    public enum ItemTarget { Player, Enemies }
    public enum ItemTrigger { Equipped, Shooting, ShootingResolved, SkillUse, SkillHit, HealthChanged, BeforeDeath, EnemyKilled }

    /// <summary>상점/인벤토리/UI가 읽는 표시 및 시전 설정. 수치는 ItemCatalog의 생성자에서 수정한다.</summary>
    public sealed class ItemMeta
    {
        public string Name { get; }
        public string Description { get; }
        public string Tags { get; }
        public int Price { get; }
        public float Cooldown { get; }
        public int Grade { get; }
        public ItemKind Kind { get; }
        public ItemCastType CastType { get; }
        public ItemTarget Target { get; }
        public Sprite ItemSprite { get; set; }
        public Sprite ActiveSkillIcon { get; set; }
        public Sprite SkillIcon => ActiveSkillIcon != null ? ActiveSkillIcon : ItemSprite;

        /// <summary>Catalog에서 아이템을 정의할 때 호출한다. 가격/쿨타임은 0 이상이며 일반형도 현재는 즉시 실행한다.</summary>
        public ItemMeta(string name, string tags, int price, float cooldown, int grade, ItemKind kind,
            ItemCastType castType, ItemTarget target, string description = "", Sprite icon = null)
        {
            if (string.IsNullOrWhiteSpace(name) || price < 0 || !float.IsFinite(cooldown) || cooldown < 0 || grade < 1)
                throw new ArgumentException("아이템 메타 값이 유효하지 않습니다.");
            Name = name; Tags = tags; Price = price; Cooldown = cooldown; Grade = grade;
            Kind = kind; CastType = castType; Target = target; Description = description; ItemSprite = icon;
        }
    }

    /// <summary>보유 아이템 한 개. Meta는 공유하고 Runtime은 보유 개체마다 독립적이다. Unity 컴포넌트가 아니다.</summary>
    public sealed class Item
    {
        public const string NullId = "Item_null";
        public string ID { get; }
        public ItemMeta Meta { get; }
        public ItemRuntime Runtime { get; }
        public bool IsEquipped => Runtime.Owner != null;
        internal PlayerInventory Inventory { get; private set; }

        /// <summary>Catalog에서 new Item(ID, new ItemMeta(...), new ItemRuntime(...))으로 정의한다.</summary>
        public Item(string id, ItemMeta meta, ItemRuntime runtime)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(nameof(id));
            ID = id; Meta = meta ?? throw new ArgumentNullException(nameof(meta));
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            if (runtime.Item != null) throw new ArgumentException("Runtime을 다른 Item과 공유할 수 없습니다.");
            if (meta.Kind == ItemKind.Active && runtime.Trigger != ItemTrigger.SkillUse)
                throw new ArgumentException("액티브는 SkillUse 트리거를 사용하세요.");
            runtime.Item = this;
        }

        /// <summary>Inventory가 획득한 개체를 장착할 때 호출한다. 이벤트 구독과 장착 효과를 시작한다.</summary>
        public void Equip(PlayerStatus player, LayerMask enemyMask, PlayerInventory inventory = null)
        {
            Inventory = inventory;
            Runtime.Equip(player, enemyMask);
        }

        /// <summary>장착 해제 시 구독, 시간 작업, 생성 오브젝트, 이 아이템의 버프를 함께 정리한다.</summary>
        public void Unequip() => Runtime.Unequip();

        /// <summary>횟수 제한 효과가 소진되었을 때 호출한다. 정확히 이 개체를 인벤토리와 슬롯에서 제거한다.</summary>
        public void Consume()
        {
            if (Inventory != null) Inventory.Remove(this);
            else { var owner = Runtime.Owner; Unequip(); if (owner != null) owner.ForgetItem(this); }
        }

        internal Item Copy() => new Item(ID, Meta, new ItemRuntime(Runtime.Trigger, Runtime.Effect));
    }
}
