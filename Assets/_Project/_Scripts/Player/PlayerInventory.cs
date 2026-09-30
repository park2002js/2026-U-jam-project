using System;
using System.Collections.Generic;
using Ujam.Runtime.Item;
using UnityEngine;

/*
보유하기 시작한 Item들에게는 필요한 동작을 할 수 있도록 Equip함수를 호출해주며, 
ItemList가 변경되는 API가 호출되는 즉시, 그 결과를 반영할 수 있도록 Event를 발생시켜 UI쪽이 ID를 전달받아 업데이트 할 수 있도록 한다.
List는 기본값으로 8칸을 보유하며, 게임이 시작될 때 자체적으로 해당 칸 크기만큼 초기화 된다.
플레이어에게 보이는 "칸과 슬롯"은 PlayerInventory에서는 관리하지 않는다. 
PlayerInventory는 말 그대로 보유한 아이템들의 ID 정보를 다루는 것이 주 목적이며, 
그것이 어디에 위치하는지, 어떤 스킬 칸에 존재하는지는 상관 쓰지 않는다.
*/

namespace UJam.Runtime.Player
{
    /// <summary>
    /// PlayerInventory는 오로지 플레이어가 보유한 Item들의 ID를 저장하기 위한 목적이며, Item의 상태가 어떠한지는 관여하지 않는다.
    /// 목록 변경 시 UI에 전달한다. 실행 상태는 ItemEquipment가 관리한다.
    /// 만약 아이템이 강화되거나 조합되는 등 이전과 상태가 달라지는 경우, Remove를 한 뒤 Add를 하는 순서로 처리한다.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private PlayerStatus _playerStatus;
        [SerializeField] private PlayerCombatManager _combatManager;
        [SerializeField] private PlayerSkillManager _skillManager;
        [SerializeField, Min(1)] private int _slotCount = 8; // 플레이어가 보유할 수 있는 기본 아이템 슬롯 크기
        private readonly List<string> itemIds = new(); // 플레이어가 보유한 아이템 ID를 저장할 List
        private ItemEquipment equipment; // 보유하는 아이템들의 상태를 관리하는 객체
        private bool changing; // 동시에 ItemList를 변경하려는 요청이 들어왔을 때, 원자성을 보장하기 위한 변수
        public event Action<IReadOnlyList<string>> OnItemsChanged; // Item ID 리스트가 변경될 때마다 구독자들에게 알리기 위한 이벤트
        public IReadOnlyList<string> Items { get { InitializeSlots(); return itemIds.AsReadOnly(); } } // Item ID 리스트

        /// <summary>게임 시작시 내부 초기화.</summary>
        private void Awake()
        {
            InitializeSlots();
            NotifyItemsChanged();
        }

        /// <summary> 만약 equipment 객체가 없다면, 플레이어가 보유한 Item들의 ID를 보관하는 List를 기본값 크기로 초기화 함 </summary>
        private void InitializeSlots()
        {
            if (equipment != null) return;

            for (int i = 0; i < Mathf.Max(1, _slotCount); i++) itemIds.Add(Item.NullId);
            equipment = new ItemEquipment(slot => RemoveAt(slot));
        }
        /// <summary> 아이템 ID 리스트가 수정될 때마다 호출되어, 구독자들에게 최신상태의 '플레이어가 보유한 Item ID들'을 반영하도록 알린다. </summary>
        private void NotifyItemsChanged()
        {
            // 플레이어가 현재 보유한 Item들의 ID 리스트를 전달한다.
            // Readonly를 사용해 구독자는 전달받은 목록을 수정할 수 없도록 한다.
            OnItemsChanged?.Invoke(Array.AsReadOnly(itemIds.ToArray()));
        }

        /// <summary>상점/테스트에서 ID별 보유 수량을 확인한다. 그런데 사실상 중복 아이템은 고려하지 않으므로 사라질 예정</summary>
        public int GetCount(string id)
        {
            InitializeSlots();
            id = ItemCatalog.Normalize(id);
            return id == Item.NullId ? 0 : itemIds.FindAll(itemId => itemId == id).Count;
        }

        private bool ResolvePlayer()
        {
            // 만약에 Inspector를 통해 연결하지 않았다면, 게임 내에서 필요 객체를 찾도록 하는, 오류방지부분
            if (_combatManager == null) _combatManager = GetComponentInParent<PlayerCombatManager>();
            if (_combatManager == null) _combatManager = FindFirstObjectByType<PlayerCombatManager>();
            if (_playerStatus == null) _playerStatus = GetComponentInParent<PlayerStatus>();
            if (_playerStatus == null && _combatManager != null) _playerStatus = _combatManager.PlayerStatus;
            if (_skillManager == null && _combatManager != null) _skillManager = _combatManager.SkillManager;
            if (_skillManager == null) _skillManager = GetComponentInChildren<PlayerSkillManager>();
            if (_skillManager != null && _combatManager != null) _skillManager.Init(_combatManager);

            // 장착부분에 저장
            equipment.Initialize(_playerStatus, _skillManager, _combatManager != null ? _combatManager.EnemyMask : (LayerMask)~0);
            return _playerStatus != null;
        }

        /// <summary>플레이어가 Item이 추가될 때 사용하는 공통 획득 API. 슬롯이 부족하면 아무것도 추가하지 않고 False를 반환한다.</summary>
        public bool TryAdd(string id)
        {
            InitializeSlots(); // (없다면)equipment 객체 생성
            if (changing || !isActiveAndEnabled) return false;

            id = ItemCatalog.Normalize(id); // id 문자열 정규화
            if (id == Item.NullId || itemIds.Contains(id)) return false;

            int slot = itemIds.IndexOf(Item.NullId); // 비어있는 칸의 번호를 찾음
            if (slot < 0 || !ResolvePlayer()) return false;

            changing = true;
            try
            {
                if (!equipment.Equip(id, slot)) return false; // 비어있는 칸에 아이템 장착
                itemIds[slot] = id;

                NotifyItemsChanged(); // 아이템 변경이 성공적으로 일어났으므로 해당 이벤트를 남들에게도 알림
                return true;
            }
            finally { changing = false; }
        }

        /// <summary>전달받은 ID의 칸을 찾아 아이템 하나를 제거한다.</summary>
        public bool TryRemove(string id)
        {
            InitializeSlots(); // (없다면)equipment 객체 생성
            id = ItemCatalog.Normalize(id);
            return RemoveAt(itemIds.IndexOf(id)); // id가 비어있는 곳
        }

        /// <summary>지정한 칸의 아이템을 제거하고 변경된 ID 목록을 전달한다.</summary>
        public bool RemoveAt(int slot)
        {
            InitializeSlots();
            if (changing || slot < 0 || slot >= itemIds.Count || itemIds[slot] == Item.NullId) return false;

            changing = true;
            try
            {
                itemIds[slot] = Item.NullId;
                equipment.Remove(slot);

                NotifyItemsChanged();
                return true;
            }
            finally { changing = false; }
        }

        

        private void OnDisable() => equipment?.Suspend();

        private void OnEnable()
        {
            InitializeSlots();
            if (itemIds.TrueForAll(id => id == Item.NullId) || !ResolvePlayer()) return;

            equipment.Resume();
        }

        private void OnDestroy() => equipment?.Clear();
    }
}
