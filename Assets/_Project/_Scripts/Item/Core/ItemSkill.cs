using System;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Ujam.Runtime.Item
{
    // 슬롯, 일반/즉시 시전과 Preview의 실제 로직. PlayerSkillManager는 이 객체에 위임한다.
    public sealed class ItemSkill
    {
        private readonly ItemData[] slots = new ItemData[2];
        private PlayerCombatManager combat;
        private ItemPreview preview;
        private int castingSlot = -1;
        public event Action<int, ItemData> Changed;
        public event Action<int, ItemData> Used;
        public int EmptySlots => (slots[0] == null ? 1 : 0) + (slots[1] == null ? 1 : 0);
        public void Init(PlayerCombatManager manager) => combat = manager;
        public ItemData Get(int slot) => slot >= 0 && slot < slots.Length ? slots[slot] : null;
        public float Remaining(int slot) => Get(slot)?.Runtime.RemainingCooldown ?? 0f;

        public bool Equip(int slot, ItemData item)
        {
            if (slot < 0 || slot >= slots.Length || item == null || item.Meta.Kind != ItemKind.Active ||
                !item.Runtime.IsEquipped || slots[slot] != null || Array.IndexOf(slots, item) >= 0) return false;
            slots[slot] = item;
            Changed?.Invoke(slot, item);
            return true;
        }
        public bool EquipFirstEmpty(ItemData item)
        {
            for (int i = 0; i < slots.Length; i++) if (slots[i] == null) return Equip(i, item);
            return false;
        }
        public void UnEquip(int slot)
        {
            if (Get(slot) == null) return;
            if (castingSlot == slot) Cancel();
            slots[slot] = null;
            Changed?.Invoke(slot, null);
        }
        public void Remove(ItemData item)
        {
            int index = Array.IndexOf(slots, item);
            if (index >= 0) UnEquip(index);
        }
        public void TryUse(int slot)
        {
            ItemData item = Get(slot);
            if (item == null || !item.Runtime.IsEquipped || item.Runtime.RemainingCooldown > 0) return;
            bool toggled = castingSlot == slot;
            Cancel();
            if (toggled) return;
            if (item.Runtime.PreviewMode == ItemPreviewMode.None)
            {
                Execute(slot, item.Runtime.Owner.transform.position);
                return;
            }
            castingSlot = slot;
            preview = new ItemPreview(item.Runtime.Preview);
        }
        public void Update()
        {
            if (castingSlot < 0) return;
            if (Get(castingSlot) == null || !Get(castingSlot).Runtime.IsEquipped || preview.Failed ||
                Keyboard.current?.escapeKey.wasPressedThisFrame == true) { Cancel(); return; }
            Camera camera = combat != null && combat.AimCamera != null ? combat.AimCamera : Camera.main;
            if (combat == null || camera == null || Mouse.current == null || !preview.IsReady ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            { preview.Hide(); return; }
            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 1000f, combat.GroundMask, QueryTriggerInteraction.Ignore))
            { preview.Hide(); return; }
            preview.Show(hit.point, hit.normal);
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;
            Execute(castingSlot, hit.point);
            Cancel();
        }
        private void Execute(int slot, Vector3 position)
        {
            ItemData item = Get(slot);
            if (item == null || !item.Runtime.IsEquipped || item.Runtime.RemainingCooldown > 0) return;
            var signal = new ItemUseContext(ItemTrigger.SkillUse, item.Runtime.Owner, position, skillItem: item);
            CombatEvents.Instance.Publish(signal);
            if (signal.Executed) Used?.Invoke(slot, item);
        }
        public void Cancel()
        {
            preview?.Dispose(); preview = null; castingSlot = -1;
        }
    }
}
