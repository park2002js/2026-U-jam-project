using System;
using Ujam.Runtime.Item;
using UnityEngine;

namespace UJam.Runtime.Player
{
    // 기존 Combat/Input/UI 진입점을 유지하고, Item 내부의 일반 C# Skill/Preview에 위임한다.
    public class PlayerSkillManager : MonoBehaviour
    {
        private readonly ItemSkill skills = new();
        public event Action<int, ItemData> OnSkillChanged { add => skills.Changed += value; remove => skills.Changed -= value; }
        public event Action<int, ItemData> OnSkillUsed { add => skills.Used += value; remove => skills.Used -= value; }
        public int EmptySlots => skills.EmptySlots;
        public ItemData GetSkill(int slot) => skills.Get(slot);
        public float GetRemainingCooldown(int slot) => skills.Remaining(slot);
        public float GetCooldownEndTime(int slot) => Time.time + skills.Remaining(slot);
        public void Init(PlayerCombatManager combatManager) => skills.Init(combatManager);
        public void TryUse(int slot) { if (isActiveAndEnabled) skills.TryUse(slot); }
        public bool Equip(int slot, ItemData item) => skills.Equip(slot, item);
        public bool EquipFirstEmpty(ItemData item) => skills.EquipFirstEmpty(item);
        public void UnEquip(int slot) => skills.UnEquip(slot);
        public void Remove(ItemData item) => skills.Remove(item);
        private void Update() => skills.Update();
        private void OnDisable() => skills.Cancel();
    }
}
