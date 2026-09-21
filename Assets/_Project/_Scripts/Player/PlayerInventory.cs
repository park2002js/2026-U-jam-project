using System;
using System.Collections.Generic;
using Ujam.Runtime.Item;
using UnityEngine;
using RuntimeItem = Ujam.Runtime.Item.Item;

namespace UJam.Runtime.Player
{
    /// <summary>보유 Item 개체와 수량을 관리한다. 구매/테스트는 TryAdd 하나를 사용하고 액티브는 첫 빈 스킬칸에 배치한다.</summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private PlayerStatus _playerStatus;
        [SerializeField] private PlayerCombatManager _combatManager;
        [SerializeField] private PlayerSkillManager _skillManager;
        private readonly Dictionary<string, int> counts = new();
        private readonly List<RuntimeItem> equipped = new();
        private bool changing;
        public event Action OnItemsChanged;
        public IReadOnlyDictionary<string, int> Items => counts;
        public IReadOnlyList<RuntimeItem> EquippedItems => equipped.AsReadOnly();

        /// <summary>상점/테스트에서 ID별 보유 수량을 확인한다.</summary>
        public int GetCount(string id) => counts.TryGetValue(ItemCatalog.Normalize(id), out int count) ? count : 0;
        private bool ResolvePlayer()
        {
            if (_combatManager == null) _combatManager = GetComponentInParent<PlayerCombatManager>();
            if (_combatManager == null) _combatManager = FindFirstObjectByType<PlayerCombatManager>();
            if (_playerStatus == null) _playerStatus = GetComponentInParent<PlayerStatus>();
            if (_playerStatus == null && _combatManager != null) _playerStatus = _combatManager.PlayerStatus;
            if (_skillManager == null && _combatManager != null) _skillManager = _combatManager.SkillManager;
            if (_skillManager == null) _skillManager = GetComponentInChildren<PlayerSkillManager>();
            if (_skillManager != null && _combatManager != null) _skillManager.Init(_combatManager);
            return _playerStatus != null;
        }

        /// <summary>구매/테스트 공통 획득 API. 슬롯이 부족하면 아무것도 추가하지 않아 ShopBuy가 환불할 수 있다.</summary>
        public bool TryAdd(string id, int amount = 1)
        {
            id = ItemCatalog.Normalize(id);
            var meta = ItemCatalog.GetMeta(id);
            if (changing || !isActiveAndEnabled || meta == null || amount <= 0 || GetCount(id) > int.MaxValue - amount || !ResolvePlayer()) return false;
            if (meta.Kind == ItemKind.Active && (_skillManager == null || _skillManager.EmptySlots < amount)) return false;
            changing = true;
            var added = new List<RuntimeItem>();
            try
            {
                for (int i = 0; i < amount; i++)
                {
                    var item = ItemCatalog.Create(id);
                    added.Add(item);
                    item.Equip(_playerStatus, _combatManager != null ? _combatManager.EnemyMask : (LayerMask)~0, this);
                    if (meta.Kind == ItemKind.Active && !_skillManager.EquipFirstEmpty(item)) throw new InvalidOperationException("빈 스킬칸이 없습니다.");
                }
                equipped.AddRange(added);
                counts[id] = GetCount(id) + amount;
            }
            catch (Exception exception)
            {
                foreach (var item in added) { _skillManager?.Remove(item); item.Unequip(); _playerStatus.ForgetItem(item); }
                Debug.LogException(exception, this);
                return false;
            }
            finally { changing = false; }
            OnItemsChanged?.Invoke();
            return true;
        }

        /// <summary>판매/테스트에서 ID와 수량으로 제거한다. 마지막에 추가한 개체부터 해제한다.</summary>
        public bool TryRemove(string id, int amount = 1)
        {
            id = ItemCatalog.Normalize(id);
            if (changing || amount <= 0 || GetCount(id) < amount) return false;
            for (int i = equipped.Count - 1; i >= 0 && amount > 0; i--)
                if (equipped[i].ID == id) { Remove(equipped[i]); amount--; }
            return true;
        }

        /// <summary>소모 횟수 종료/부활처럼 정확한 보유 개체를 삭제한다. 동일 ID의 다른 개체는 유지한다.</summary>
        public bool Remove(RuntimeItem item)
        {
            if (changing || item == null || !equipped.Contains(item)) return false;
            changing = true;
            try
            {
                equipped.Remove(item);
                if (--counts[item.ID] == 0) counts.Remove(item.ID);
                _skillManager?.Remove(item);
                item.Unequip();
                if (_playerStatus != null) _playerStatus.ForgetItem(item);
            }
            finally { changing = false; }
            OnItemsChanged?.Invoke();
            return true;
        }

        private void OnDisable()
        {
            foreach (var item in equipped.ToArray()) { _skillManager?.Remove(item); item.Unequip(); }
        }
        private void OnEnable()
        {
            if (equipped.Count == 0 || !ResolvePlayer()) return;
            foreach (var item in equipped)
            {
                item.Equip(_playerStatus, _combatManager != null ? _combatManager.EnemyMask : (LayerMask)~0, this);
                if (item.Meta.Kind == ItemKind.Active && (_skillManager == null || !_skillManager.EquipFirstEmpty(item)))
                { item.Unequip(); Debug.LogError($"[Inventory] {item.ID}의 빈 스킬칸이 없습니다.", this); }
            }
        }
        private void OnDestroy()
        {
            foreach (var item in equipped) { item.Unequip(); if (_playerStatus != null) _playerStatus.ForgetItem(item); }
        }
    }
}
