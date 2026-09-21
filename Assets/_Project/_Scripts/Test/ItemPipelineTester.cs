using System.Collections.Generic;
using System.Linq;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Ujam.Runtime.Item
{
    /// <summary>
    /// 씬의 빈 오브젝트에 붙이고 itemIds에 CSV ID를 입력한다. 구매와 같은 TryAdd로 장착한다.
    /// 기본값은 11번 낙뢰/13번 아드레날린. D/F로 즉시 시전하며 일반형도 클릭 확정을 기다리지 않는다.
    /// 패시브 ID도 같은 목록에 추가할 수 있다. 최대 두 개의 액티브 슬롯만 사용한다.
    /// </summary>
    public sealed class ItemPipelineTester : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [UnityEngine.Serialization.FormerlySerializedAs("itemGuids")]
        [SerializeField] private List<string> itemIds = new() { "11", "13" };
        [SerializeField] private bool equipOnStart = true;
        private void Start() { if (equipOnStart) EquipItems(); }

        /// <summary>Play Mode 시작 또는 컴포넌트 메뉴에서 호출한다. 미등록 ID/빈 슬롯 부족을 로그로 알린다.</summary>
        [ContextMenu("Equip configured IDs")]
        public void EquipItems()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Play Mode에서 실행하세요.", this); return; }
            if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory == null) { Debug.LogError("PlayerInventory를 연결하세요.", this); return; }
            foreach (var id in itemIds)
            {
                var meta = ItemCatalog.GetMeta(id);
                if (meta == null) { Debug.LogError($"등록되지 않은 ID: {id}", this); continue; }
                if (inventory.GetCount(id) > 0) continue;
                if (inventory.TryAdd(id)) Debug.Log($"장착: {id} / {meta.Name} / {meta.Kind}", this);
                else Debug.LogError($"장착 실패: {id}. 플레이어 연결과 빈 스킬칸을 확인하세요.", this);
            }
        }

        /// <summary>테스트로 지정한 ID를 한 개씩 해제한다. 새로운 구성을 테스트하기 전에 호출한다.</summary>
        [ContextMenu("Unequip configured IDs")]
        public void UnequipItems()
        {
            if (!Application.isPlaying || inventory == null) return;
            foreach (var id in itemIds) if (inventory.GetCount(id) > 0) inventory.TryRemove(id);
        }

        /// <summary>카탈로그 개수, 개체 격리, 이벤트 해제, 버프 합산/갱신/출처별 해제를 검사한다. 실제 전투 수치는 변경하지 않는다.</summary>
        [ContextMenu("Check item contracts")]
        public void CheckContracts()
        {
            Assert.AreEqual(30, ItemCatalog.IDs.Count());
            Assert.AreEqual(15, ItemCatalog.IDs.Count(id => ItemCatalog.GetMeta(id).Kind == ItemKind.Active));
            var first = ItemCatalog.Create("11");
            var second = ItemCatalog.Create("Item011");
            Assert.AreEqual(first.Meta, second.Meta);
            Assert.IsFalse(ReferenceEquals(first, second));
            Assert.IsFalse(ReferenceEquals(first.Runtime, second.Runtime));
            Assert.AreEqual(ItemCastType.Normal, first.Meta.CastType);
            Assert.IsTrue(first.Runtime.Effect is LightningEffect);
            Assert.IsNull(ItemCatalog.Create("not-an-item"));

            var bus = new EventManager();
            int calls = 0;
            System.Action<ItemEvent> listener = signal => { Assert.IsNull(signal.Enemies); calls++; };
            bus.Subscribe(ItemTrigger.Shooting, listener);
            bus.Publish(new ItemEvent(ItemTrigger.Shooting, null));
            bus.Unsubscribe(ItemTrigger.Shooting, listener);
            bus.Publish(new ItemEvent(ItemTrigger.Shooting, null));
            Assert.AreEqual(1, calls);

            if (Application.isPlaying && PlayerStatus.Instance != null)
            {
                var player = PlayerStatus.Instance;
                var buffs = new BuffManager(); // 독립 인스턴스이므로 실제 전투에는 영향을 주지 않는다.
                var a = new object(); var b = new object();
                buffs.SetCondition(player, a, BuffStat.AttackDamage, 10);
                buffs.SetCondition(player, b, BuffStat.AttackDamage, 20);
                Assert.AreApproximatelyEqual(1.3f, buffs.Multiplier(player, BuffStat.AttackDamage));
                buffs.SetCondition(player, a, BuffStat.AttackDamage, 15);
                Assert.AreApproximatelyEqual(1.35f, buffs.Multiplier(player, BuffStat.AttackDamage));
                buffs.RemoveSource(a);
                Assert.AreApproximatelyEqual(1.2f, buffs.Multiplier(player, BuffStat.AttackDamage));
                buffs.RemoveSource(b);
                Assert.AreApproximatelyEqual(1f, buffs.Multiplier(player, BuffStat.AttackDamage));
            }
            Debug.Log("30개 아이템 및 개체/이벤트/버프 계약 검사 통과.", this);
        }
    }
}
