using System;
using Ujam.Runtime.Item;
using UJam.Runtime.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// 두 스킬 슬롯과 쿨타임의 소유자. 사용이 정당하면 EventManager로 전달하며 Effect를 직접 호출하지 않는다.
    /// Normal 시전은 Preview로 위치를 고른 뒤 좌클릭으로 확정하고, Instant는 즉시 확정한다.
    /// </summary>
    public class PlayerSkillManager : MonoBehaviour
    {
        [SerializeField, Tooltip("스킬 범위 Preview 프리팹. 비우면 CombatManager에 연결된 것을 사용한다.")]
        private GameObject previewPrefab;

        private readonly Item[] slots = new Item[2];
        private readonly float[] readyAt = new float[2];
        private PlayerCombatManager combat;

        private PlayerSkillPreview preview;   // 생성된 Preview 인스턴스
        private int aimingSlot = -1;          // 조준 중인 슬롯 (-1이면 조준 아님)

        public event Action<int, Item> OnSkillChanged;
        public event Action<int, Item> OnSkillUsed;
        public int EmptySlots => (slots[0] == null ? 1 : 0) + (slots[1] == null ? 1 : 0);

        /// <summary>조준 중인지 여부. 입력 처리 쪽에서 기본 공격을 막을 때 참조한다.</summary>
        public bool IsAiming => aimingSlot >= 0;

        /// <summary>PlayerCombatManager/Inventory 초기화 시 공통 카메라/레이어 정보를 연결한다.</summary>
        public void Init(PlayerCombatManager manager)
        {
            combat = manager;

            // Preview 인스턴스를 한 번만 만들어 재사용한다.
            GameObject prefab = previewPrefab != null ? previewPrefab : manager != null ? manager.PlayerSkillPreviewPrefab : null;
            if (prefab != null && preview == null)
            {
                preview = Instantiate(prefab).GetComponent<PlayerSkillPreview>();
                if (preview == null) Debug.LogError("[SkillManager] Preview 프리팹에 PlayerSkillPreview가 없습니다.", this);
                else preview.Hide();
            }
        }

        /// <summary>UI/테스트에서 슬롯의 보유 Item을 읽는다.</summary>
        public Item GetSkill(int slot) => slot >= 0 && slot < slots.Length ? slots[slot] : null;
        /// <summary>UI가 실제 재사용 가능 시각을 읽는다. 쿨타임 감소율은 시전 시작 시 반영한다.</summary>
        public float GetCooldownEndTime(int slot) => slot >= 0 && slot < slots.Length ? readyAt[slot] : 0;
        /// <summary>UI/테스트에서 남은 쿨타임을 읽는다.</summary>
        public float GetRemainingCooldown(int slot) => Mathf.Max(0, GetCooldownEndTime(slot) - Time.time);

        /// <summary>Inventory가 빈 슬롯에 액티브를 배치할 때 호출한다. 기존 슬롯을 덮어쓰지 않는다.</summary>
        public bool Equip(int slot, Item item)
        {
            if (slot < 0 || slot >= slots.Length || item == null || !item.IsEquipped || item.Meta.Kind != ItemKind.Active ||
                slots[slot] != null || Array.IndexOf(slots, item) >= 0) return false;
            slots[slot] = item; readyAt[slot] = 0;
            OnSkillChanged?.Invoke(slot, item);
            return true;
        }
        /// <summary>구매/테스트 시 첫 빈 스킬칸을 사용한다.</summary>
        public bool EquipFirstEmpty(Item item)
        { for (int i = 0; i < slots.Length; i++) if (slots[i] == null) return Equip(i, item); return false; }
        /// <summary>슬롯만 해제한다. 보유 개체/구독의 제거는 Inventory.Remove 또는 Item.Unequip에서 처리한다.</summary>
        public void UnEquip(int slot)
        {
            if (GetSkill(slot) == null) return;
            if (aimingSlot == slot) CancelAiming();   // 조준 중이던 스킬이 사라지면 취소
            slots[slot] = null; readyAt[slot] = 0; OnSkillChanged?.Invoke(slot, null);
        }
        /// <summary>Inventory에서 특정 개체가 사라질 때 해당 슬롯을 정리한다.</summary>
        public void Remove(Item item) { int slot = Array.IndexOf(slots, item); if (slot >= 0) UnEquip(slot); }

        /// <summary>D/F 입력에서 호출한다. Normal은 조준을 시작하고 Instant는 즉시 시전한다.</summary>
        public void TryUse(int slot)
        {
            var item = GetSkill(slot);
            if (!CanUse(slot, item) || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 position = item.Runtime.Owner.transform.position;
            if (item.Meta.Target == ItemTarget.Enemies)
            {
                if (!TryGetGroundPoint(out Vector3 point, out _)) return;
                position = point;
            }

            // Normal이면 위치를 고르는 조준 모드로 들어간다.
            if (item.Meta.CastType == ItemCastType.Normal && item.Meta.Target == ItemTarget.Enemies && preview != null)
            {
                StartAiming(slot, item);
                return;
            }

            ConfirmUse(slot, position);
        }

        /// <summary>조준 모드 진입. 같은 슬롯을 다시 누르면 취소하고, 다른 슬롯이면 대상을 바꾼다.</summary>
        private void StartAiming(int slot, Item item)
        {
            if (aimingSlot == slot) { CancelAiming(); return; }

            aimingSlot = slot;
            // 반경은 칸 단위이므로 CellWidth로 환산해 실제 판정 범위와 맞춘다.
            float radius = item.Runtime.Effect.PreviewRadiusCells * ItemWorld.CellWidth;
            preview.Show(radius);
        }

        /// <summary>조준 취소. Preview를 숨기고 상태를 초기화한다.</summary>
        public void CancelAiming()
        {
            aimingSlot = -1;
            if (preview != null) preview.Hide();
        }

        private void Update()
        {
            if (aimingSlot < 0 || preview == null) return;

            // 조준 중인 스킬이 더 이상 쓸 수 없게 되면 취소
            var item = GetSkill(aimingSlot);
            if (item == null || !item.IsEquipped) { CancelAiming(); return; }

            // 마우스가 가리키는 바닥을 따라 Preview를 옮긴다.
            bool hasPoint = TryGetGroundPoint(out Vector3 point, out Vector3 normal);
            if (hasPoint) preview.SetPosition(point, normal);

            if (Mouse.current == null) return;

            // 좌클릭 확정 (UI 위 클릭은 무시)
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
                if (!hasPoint) return;

                int slot = aimingSlot;
                CancelAiming();
                ConfirmUse(slot, point);
            }
            // 우클릭 또는 Esc로 취소
            else if (Mouse.current.rightButton.wasPressedThisFrame ||
                     (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                CancelAiming();
            }
        }

        /// <summary>마우스가 가리키는 바닥 지점과 표면 방향을 구한다.</summary>
        private bool TryGetGroundPoint(out Vector3 point, out Vector3 normal)
        {
            point = default; normal = Vector3.up;

            Camera camera = combat != null && combat.AimCamera != null ? combat.AimCamera : Camera.main;
            if (camera == null || combat == null || Mouse.current == null) return false;

            if (!Physics.Raycast(camera.ScreenPointToRay(Mouse.current.position.ReadValue()), out var hit, 1000,
                combat.GroundMask, QueryTriggerInteraction.Ignore)) return false;

            point = hit.point; normal = hit.normal;
            return true;
        }

        /// <summary>위치가 확정된 시전의 단일 진입점. Preview의 클릭 확정과 테스트도 이 API를 사용한다.</summary>
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

        private bool CanUse(int slot, Item item) => isActiveAndEnabled && item != null && item.IsEquipped &&
            item.Runtime.Owner.CurrentHealth > 0 && Time.time >= readyAt[slot];
    }
}