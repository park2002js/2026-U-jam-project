using System;
using Ujam.Runtime.Item;
using UJam.Runtime.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using RuntimeItem = Ujam.Runtime.Item.Item;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// 두 스킬 슬롯과 쿨타임의 소유자. 사용이 정당하면 EventManager로 전달하며 Effect를 직접 호출하지 않는다.
    /// 현재 일반/즉발 모두 즉시 확정한다. 향후 Preview는 TryUse에서 시작하고 클릭 시 ConfirmUse를 호출하면 된다.
    /// </summary>
    public class PlayerSkillManager : MonoBehaviour
    {
        private readonly RuntimeItem[] slots = new RuntimeItem[2];
        private readonly float[] readyAt = new float[2];
        private PlayerCombatManager combat;
        public event Action<int, RuntimeItem> OnSkillChanged;
        public event Action<int, RuntimeItem> OnSkillUsed;
        public int EmptySlots => (slots[0] == null ? 1 : 0) + (slots[1] == null ? 1 : 0);

        /// <summary>PlayerCombatManager/Inventory 초기화 시 공통 카메라/레이어 정보를 연결한다.</summary>
        public void Init(PlayerCombatManager manager) => combat = manager;
        /// <summary>UI/테스트에서 슬롯의 보유 Item을 읽는다.</summary>
        public RuntimeItem GetSkill(int slot) => slot >= 0 && slot < slots.Length ? slots[slot] : null;
        /// <summary>UI가 실제 재사용 가능 시각을 읽는다. 쿨타임 감소율은 시전 시작 시 반영한다.</summary>
        public float GetCooldownEndTime(int slot) => slot >= 0 && slot < slots.Length ? readyAt[slot] : 0;
        /// <summary>UI/테스트에서 남은 쿨타임을 읽는다.</summary>
        public float GetRemainingCooldown(int slot) => Mathf.Max(0, GetCooldownEndTime(slot) - Time.time);

        /// <summary>Inventory가 빈 슬롯에 액티브를 배치할 때 호출한다. 기존 슬롯을 덮어쓰지 않는다.</summary>
        public bool Equip(int slot, RuntimeItem item)
        {
            if (slot < 0 || slot >= slots.Length || item == null || !item.IsEquipped || item.Meta.Kind != ItemKind.Active ||
                slots[slot] != null || Array.IndexOf(slots, item) >= 0) return false;
            slots[slot] = item; readyAt[slot] = 0;
            OnSkillChanged?.Invoke(slot, item);
            return true;
        }
        /// <summary>구매/테스트 시 첫 빈 스킬칸을 사용한다.</summary>
        public bool EquipFirstEmpty(RuntimeItem item)
        { for (int i = 0; i < slots.Length; i++) if (slots[i] == null) return Equip(i, item); return false; }
        /// <summary>슬롯만 해제한다. 보유 개체/구독의 제거는 Inventory.Remove 또는 Item.Unequip에서 처리한다.</summary>
        public void UnEquip(int slot)
        {
            if (GetSkill(slot) == null) return;
            slots[slot] = null; readyAt[slot] = 0; OnSkillChanged?.Invoke(slot, null);
        }
        /// <summary>Inventory에서 특정 개체가 사라질 때 해당 슬롯을 정리한다.</summary>
        public void Remove(RuntimeItem item) { int slot = Array.IndexOf(slots, item); if (slot >= 0) UnEquip(slot); }

        /// <summary>D/F 입력에서 호출한다. 적 대상 스킬은 현재 마우스의 바닥 위치, 자기 버프는 플레이어 위치를 사용한다.</summary>
        public void TryUse(int slot)
        {
            var item = GetSkill(slot);
            if (!CanUse(slot, item) || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Vector3 position = item.Runtime.Owner.transform.position;
            if (item.Meta.Target == ItemTarget.Enemies)
            {
                Camera camera = combat != null && combat.AimCamera != null ? combat.AimCamera : Camera.main;
                if (camera == null || combat == null || Mouse.current == null) return;
                if (!Physics.Raycast(camera.ScreenPointToRay(Mouse.current.position.ReadValue()), out var hit, 1000,
                    combat.GroundMask, QueryTriggerInteraction.Ignore)) return;
                position = hit.point;
            }
            // Preview 도입 지점: Normal이면 위치 선택을 시작하고 나중에 ConfirmUse를 호출한다.
            ConfirmUse(slot, position);
        }

        /// <summary>위치가 확정된 시전의 단일 진입점. 미래 Preview의 클릭 확정과 테스트도 이 API를 사용한다.</summary>
        public bool ConfirmUse(int slot, Vector3 position)
        {
            var item = GetSkill(slot);
            if (!CanUse(slot, item)) return false;
            var player = item.Runtime.Owner;
            var probe = new ItemEvent(ItemTrigger.SkillUse, player, position, skillItem: item);
            if (!item.Runtime.Effect.CanExecute(new ItemUseContext(item, probe))) return false;
            var signal = new ItemEvent(ItemTrigger.SkillUse, player, position, skillItem: item, cast: player.BeginSkill(item));
            float previous = readyAt[slot];
            readyAt[slot] = Time.time + item.Meta.Cooldown * player.CooldownMultiplier;
            EventManager.Instance.Publish(signal);
            if (!signal.Executed) { readyAt[slot] = previous; return false; }
            if (slots[slot] == item) OnSkillUsed?.Invoke(slot, item); // 소모 아이템은 이미 슬롯에서 사라졌을 수 있다.
            return true;
        }
        private bool CanUse(int slot, RuntimeItem item) => isActiveAndEnabled && item != null && item.IsEquipped &&
            item.Runtime.Owner.CurrentHealth > 0 && Time.time >= readyAt[slot];
    }
}
