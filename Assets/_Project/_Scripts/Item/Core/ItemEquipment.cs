using System;
using System.Collections.Generic;
using UJam.Runtime.Player;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>인벤토리 칸에 대응하는 실제 Item과 실행 상태를 관리한다.</summary>
    internal class ItemEquipment
    {
        private readonly Dictionary<int, Item> equipped = new();
        private readonly Action<int> consumeSlot;
        private PlayerStatus player;
        private PlayerSkillManager skills;
        private LayerMask enemyMask;

        public ItemEquipment(Action<int> consumeSlot) => this.consumeSlot = consumeSlot;

        public void Initialize(PlayerStatus player, PlayerSkillManager skills, LayerMask enemyMask)
        {
            this.player = player;
            this.skills = skills;
            this.enemyMask = enemyMask;
        }

        /// <summary>ID로 실행 개체 하나를 만들어 지정한 칸에 장착한다. 실패하면 해당 개체를 정리한다.</summary>
        public bool Equip(string id, int slot)
        {
            var meta = ItemCatalog.GetMeta(id);
            if (meta == null || player == null || equipped.ContainsKey(slot)) return false;
            if (meta.Kind == ItemKind.Active && (skills == null || skills.EmptySlots == 0)) return false;

            try
            {
                var item = ItemCatalog.Create(id);
                equipped.Add(slot, item);
                EquipItem(slot, item);
                return true;
            }
            catch (Exception exception)
            {
                Remove(slot);
                Debug.LogException(exception);
                return false;
            }
        }

        private void EquipItem(int slot, Item item)
        {
            item.Equip(player, enemyMask, () => consumeSlot(slot));
            if (item.Meta.Kind == ItemKind.Active && (skills == null || !skills.EquipFirstEmpty(item)))
                throw new InvalidOperationException("빈 스킬칸이 없습니다.");
        }

        /// <summary>판매/소모된 칸의 실행 개체와 효과, 스킬칸, 카운터를 함께 정리한다.</summary>
        public void Remove(int slot)
        {
            if (!equipped.TryGetValue(slot, out var item)) return;
            equipped.Remove(slot);

            skills?.Remove(item);
            item.Unequip();
            if (player != null) player.ForgetItem(item);
        }

        public void Suspend()
        {
            foreach (var item in new List<Item>(equipped.Values))
            {
                skills?.Remove(item);
                item.Unequip();
            }
        }

        public void Resume()
        {
            foreach (var entry in new List<KeyValuePair<int, Item>>(equipped))
            {
                if (entry.Value.IsEquipped) continue;

                try { EquipItem(entry.Key, entry.Value); }
                catch (Exception exception)
                {
                    skills?.Remove(entry.Value);
                    entry.Value.Unequip();
                    Debug.LogException(exception);
                }
            }
        }

        public void Clear()
        {
            foreach (int slot in new List<int>(equipped.Keys)) Remove(slot);
        }
    }
}
