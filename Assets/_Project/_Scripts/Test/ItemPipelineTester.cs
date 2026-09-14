using System.Collections.Generic;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Ujam.Runtime.Item
{
    // 기존 플레이어 또는 빈 오브젝트에 추가하고 GUID를 입력한다.
    // D/F: 슬롯 1/2. 일반 시전은 좌클릭 확정, 같은 키 또는 Esc로 취소.
    // 실제 구매와 같은 Inventory.TryAdd 경로를 사용하며 테스트에서는 골드를 소비하지 않는다.
    public sealed class ItemPipelineTester : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private List<string> itemGuids = new() { "31", "33" };
        [SerializeField] private bool equipOnStart = true;

        private void Start() { if (equipOnStart) EquipItems(); }

        [ContextMenu("Equip configured GUIDs")]
        public void EquipItems()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Play Mode에서 장착하세요.", this); return; }
            if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory == null) { Debug.LogError("PlayerInventory를 연결하세요.", this); return; }
            foreach (string guid in itemGuids)
            {
                ItemMeta meta = ItemCatalog.GetMeta(guid);
                if (meta == null) { Debug.LogError($"등록되지 않은 GUID: {guid}", this); continue; }
                if (inventory.GetCount(guid) > 0) continue;
                if (inventory.TryAdd(guid)) Debug.Log($"장착 완료: {meta.GUID} / {meta.Name} / {meta.Kind}", this);
                else Debug.LogError($"장착 실패: {guid}. Player 연결과 빈 스킬칸을 확인하세요.", this);
            }
        }

        [ContextMenu("Unequip configured GUIDs")]
        public void UnequipItems()
        {
            if (!Application.isPlaying || inventory == null) return;
            foreach (string guid in itemGuids)
                if (inventory.GetCount(guid) > 0) inventory.TryRemove(guid);
        }

        [ContextMenu("Check catalog and event contracts")]
        public void CheckContracts()
        {
            // 별도의 프레임워크 없이 실행할 수 있는 최소 계약 검사. 실제 시전 검사는 위 장착 경로로 한다.
            var first = ItemCatalog.Create("31");
            var second = ItemCatalog.Create("Item031");
            Assert.AreEqual(first.Meta, second.Meta);
            Assert.IsFalse(ReferenceEquals(first.Runtime, second.Runtime));
            Assert.IsFalse(ReferenceEquals(first.Runtime.Effect, second.Runtime.Effect));
            Assert.AreEqual(first.Meta.ItemSprite, first.Meta.SkillIcon);
            Assert.AreEqual(ItemPreviewMode.None, first.Runtime.PreviewMode);
            Assert.AreEqual(ItemPreviewMode.Ground, ItemCatalog.Create("33").Runtime.PreviewMode);
            var bus = new CombatEvents();
            int received = 0;
            System.Action<ItemUseContext> listener = context => { Assert.IsNull(context.Enemies); received++; };
            bus.Subscribe(ItemTrigger.Shooting, listener);
            bus.Publish(new ItemUseContext(ItemTrigger.Shooting, null));
            bus.Unsubscribe(ItemTrigger.Shooting, listener);
            bus.Publish(new ItemUseContext(ItemTrigger.Shooting, null));
            Assert.AreEqual(1, received);
            Debug.Log("Item catalog / runtime isolation / icon fallback / event unsubscribe 검사 완료.", this);
        }
    }
}
