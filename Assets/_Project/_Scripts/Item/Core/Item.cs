using System;
using UJam.Runtime.Player;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    public enum ItemKind { Active, Passive }
    public enum ItemCastType { Instant, Normal }
    public enum ItemPreviewType { None, Circle, Rectangle }
    public enum ItemTarget { Player, Enemies }
    public enum ItemTrigger { Equipped, Shooting, ShootingResolved, SkillUse, SkillHit, HealthChanged, BeforeDeath, EnemyKilled }

    /// <summary>Preview의 형태, 직접 적용할 X/Z 스케일, 월드 XZ 방향. 한 스케일은 두 축에 동일하게 적용한다.</summary>
    public readonly struct ItemPreview
    {
        public ItemPreviewType Type { get; }
        public Vector2 Scale { get; }
        public Vector3 Direction { get; }

        public ItemPreview(ItemPreviewType type, float scale = 1f, Vector3? direction = null)
            : this(type, scale, scale, direction) { }

        public ItemPreview(ItemPreviewType type, float scaleX, float scaleZ, Vector3? direction = null)
        {
            if (type != ItemPreviewType.None && type != ItemPreviewType.Circle && type != ItemPreviewType.Rectangle)
                throw new ArgumentException("Preview 종류가 유효하지 않습니다.", nameof(type));
            if (!float.IsFinite(scaleX) || !float.IsFinite(scaleZ) || scaleX <= 0 || scaleZ <= 0)
                throw new ArgumentException("Preview 스케일은 유한한 양수여야 합니다.");

            Vector3 heading = direction ?? Vector3.forward;
            float magnitude = Mathf.Max(Mathf.Abs(heading.x), Mathf.Abs(heading.z));
            if (!float.IsFinite(heading.x) || !float.IsFinite(heading.y) || !float.IsFinite(heading.z) || magnitude <= 0)
                throw new ArgumentException("Preview 방향은 유한한 XZ 방향 벡터여야 합니다.", nameof(direction));

            Type = type;
            Scale = new Vector2(scaleX, scaleZ);
            Direction = new Vector3(heading.x / magnitude, 0, heading.z / magnitude).normalized;
        }
    }

    /// <summary>상점/인벤토리/UI가 읽는 표시 및 시전 설정. 수치는 ItemCatalog의 생성자에서 수정한다.</summary>
    public class ItemMeta
    {
        public string Name { get; }
        public string Description { get; }
        public string Tags { get; }
        public int Price { get; }
        public float Cooldown { get; }
        public int Grade { get; }
        public ItemKind Kind { get; }
        public ItemPreview Preview { get; }
        public string VfxPath { get; }
        public ItemCastType CastType => Preview.Type == ItemPreviewType.None ? ItemCastType.Instant : ItemCastType.Normal;
        public ItemTarget Target { get; }
        public Sprite ItemSprite { get; set; }
        public Sprite ActiveSkillIcon { get; set; }
        public Sprite SkillIcon => ActiveSkillIcon != null ? ActiveSkillIcon : ItemSprite;

        /// <summary>Catalog에서 아이템을 정의한다. Preview가 None이면 즉시 시전하고, 나머지는 조준 후 확정한다.</summary>
        public ItemMeta(string name, string tags, int price, float cooldown, int grade, ItemKind kind,
            ItemTarget target, string description = "", string iconName = null, ItemPreview preview = default, string vfxPath = null)
        {
            if (string.IsNullOrWhiteSpace(name) || price < 0 || !float.IsFinite(cooldown) || cooldown < 0 || grade < 1)
                throw new ArgumentException("아이템 메타 값이 유효하지 않습니다.");

            Name = name; Tags = tags; Price = price; Cooldown = cooldown; Grade = grade;
            Kind = kind; Target = target; Description = description;
            Preview = preview.Type == ItemPreviewType.None && preview.Direction == Vector3.zero
                ? new ItemPreview(ItemPreviewType.None) : preview;
            VfxPath = vfxPath;

            // Resources/Art/Item에서 확장자를 제외한 파일명으로 조회한다. 미지정이거나 찾지 못하면 null 이미지를 사용한다.
            string imageName = string.IsNullOrWhiteSpace(iconName) ? "null" : iconName;
            var sprites = Resources.LoadAll<Sprite>($"Art/Item/{imageName}");
            if (sprites.Length == 0) sprites = Resources.LoadAll<Sprite>("Art/Item/null");

            // Multiple로 임포트된 이미지도 읽을 수 있도록 해당 파일의 첫 번째 Sprite를 사용한다.
            ItemSprite = sprites.Length > 0 ? sprites[0] : null;
        }
    }

    /// <summary>보유 아이템 한 개. Meta는 공유하고 Runtime은 보유 개체마다 독립적이다. Unity 컴포넌트가 아니다.</summary>
    public class Item
    {
        public const string NullId = "Item_null";
        public string ID { get; }
        public ItemMeta Meta { get; }
        public ItemRuntime Runtime { get; }
        public bool IsEquipped => Runtime.Owner != null;
        private Action onConsumed;

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
        public void Equip(PlayerStatus player, LayerMask enemyMask, Action onConsumed = null)
        {
            this.onConsumed = onConsumed;
            Runtime.Equip(player, enemyMask);
        }

        /// <summary>장착 해제 시 구독, 시간 작업, 생성 오브젝트, 이 아이템의 버프를 함께 정리한다.</summary>
        public void Unequip()
        {
            onConsumed = null;
            Runtime.Unequip();
        }

        /// <summary>횟수 제한 효과가 소진되었을 때 호출한다. 정확히 이 개체를 인벤토리와 슬롯에서 제거한다.</summary>
        public void Consume()
        {
            if (onConsumed != null) onConsumed();
            else
            {
                var owner = Runtime.Owner;
                Unequip();

                if (owner != null) owner.ForgetItem(this);
            }
        }

        internal Item Copy() => new Item(ID, Meta, new ItemRuntime(Runtime.Trigger, Runtime.Effect));
    }
}
