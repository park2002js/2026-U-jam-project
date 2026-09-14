using System;
using System.Collections.Generic;
using Ujam.Runtime.Item;
using UnityEngine;

namespace UJam.Runtime.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private PlayerStatus _playerStatus;
        [SerializeField] private PlayerCombatManager _combatManager;
        [SerializeField] private PlayerSkillManager _skillManager;
        private readonly Dictionary<string, int> _items = new();
        private readonly List<ItemData> equipped = new();
        private bool changing;
        public event Action OnItemsChanged;
        public IReadOnlyDictionary<string, int> Items => _items;
        public IReadOnlyList<ItemData> EquippedItems => equipped.AsReadOnly();
        public int GetCount(string itemId) => _items.TryGetValue(ItemCatalog.Normalize(itemId), out int count) ? count : 0;

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

        // ShopBuy와 GUID 테스트 모두 이 경로로 들어온다. 빈 스킬칸이 없으면 구매도 실패/환불된다.
        public bool TryAdd(string itemId, int amount = 1)
        {
            string guid = ItemCatalog.Normalize(itemId);
            ItemMeta meta = ItemCatalog.GetMeta(guid);
            if (changing || !isActiveAndEnabled || meta == null || guid == ItemData.NullId || amount <= 0 ||
                GetCount(guid) > int.MaxValue - amount || !ResolvePlayer()) return false;
            if (meta.Kind == ItemKind.Active && (_skillManager == null || _skillManager.EmptySlots < amount)) return false;
            changing = true;
            var added = new List<ItemData>();
            try
            {
                for (int i = 0; i < amount; i++)
                {
                    var item = ItemCatalog.Create(guid);
                    added.Add(item);
                    item.Runtime.Equip(_playerStatus, _combatManager != null ? _combatManager.EnemyMask : (LayerMask)~0);
                    if (meta.Kind == ItemKind.Active && !_skillManager.EquipFirstEmpty(item))
                        throw new InvalidOperationException("아이템을 장착할 빈 스킬칸이 없습니다.");
                }
                equipped.AddRange(added);
                _items[guid] = GetCount(guid) + amount;
            }
            catch (Exception exception)
            {
                foreach (var item in added) { _skillManager?.Remove(item); item.Runtime.Dispose(); }
                Debug.LogException(exception, this);
                return false;
            }
            finally { changing = false; }
            OnItemsChanged?.Invoke();
            return true;
        }

        public bool TryRemove(string itemId, int amount = 1)
        {
            string guid = ItemCatalog.Normalize(itemId);
            int count = GetCount(guid);
            if (changing || amount <= 0 || count < amount) return false;
            changing = true;
            try
            {
                int left = amount;
                for (int i = equipped.Count - 1; i >= 0 && left > 0; i--)
                {
                    var item = equipped[i];
                    if (item.Id != guid) continue;
                    _skillManager?.Remove(item);
                    item.Runtime.Dispose();
                    equipped.RemoveAt(i);
                    left--;
                }
                if (count == amount) _items.Remove(guid); else _items[guid] = count - amount;
            }
            finally { changing = false; }
            OnItemsChanged?.Invoke();
            return true;
        }

        private void OnDisable()
        {
            foreach (var item in equipped) { _skillManager?.Remove(item); item.Runtime.Dispose(); }
        }
        private void OnEnable()
        {
            if (equipped.Count == 0 || !ResolvePlayer()) return;
            foreach (var item in equipped)
            {
                item.Runtime.Equip(_playerStatus, _combatManager != null ? _combatManager.EnemyMask : (LayerMask)~0);
                if (item.Meta.Kind == ItemKind.Active && (_skillManager == null || !_skillManager.EquipFirstEmpty(item)))
                {
                    item.Runtime.Dispose();
                    Debug.LogError($"[PlayerInventory] {item.Id} 재장착 실패: 스킬칸을 확인하세요.", this);
                }
            }
        }
    }
}
