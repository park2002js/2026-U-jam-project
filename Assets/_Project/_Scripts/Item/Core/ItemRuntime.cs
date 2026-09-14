using System;
using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    public sealed class ItemRuntime : IDisposable
    {
        public ItemData Item { get; internal set; }
        public PlayerStatus Owner { get; private set; }
        public bool IsEquipped { get; private set; }
        public ItemTrigger Trigger { get; }
        public ItemPreviewMode PreviewMode { get; }
        public ItemPreviewDefinition Preview { get; }
        public float Cooldown { get; }
        public float RemainingCooldown { get; private set; }
        public ItemEffect Effect { get; }
        public LayerMask EnemyMask { get; private set; }
        public Func<ItemUseContext, bool> Condition { get; }
        private readonly List<IEnumerator> routines = new();
        private readonly List<Action> cleanup = new();
        private bool executing;

        public ItemRuntime(ItemTrigger trigger, ItemEffect effect, float cooldown = 0,
            ItemPreviewMode previewMode = ItemPreviewMode.None, ItemPreviewDefinition preview = null,
            Func<ItemUseContext, bool> condition = null)
        {
            if (!float.IsFinite(cooldown) || cooldown < 0) throw new ArgumentOutOfRangeException(nameof(cooldown));
            if (previewMode != ItemPreviewMode.None && preview == null) throw new ArgumentNullException(nameof(preview));
            Trigger = trigger; Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            Cooldown = cooldown; PreviewMode = previewMode; Preview = preview; Condition = condition;
        }

        public void Equip(PlayerStatus owner, LayerMask enemyMask)
        {
            if (IsEquipped) throw new InvalidOperationException("이미 장착된 Runtime입니다.");
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            Owner = owner; EnemyMask = enemyMask; IsEquipped = true;
            RuntimeTimer.Ensure();
            CombatEvents.Instance.Subscribe(Trigger, OnTrigger);
            RuntimeTimer.Instance.Tick += Tick;
            try { Effect.OnEquip(this); }
            catch { Dispose(); throw; }
        }

        private void OnTrigger(ItemUseContext signal)
        {
            if (!IsEquipped || executing || Owner == null || signal.Player != Owner || RemainingCooldown > 0) return;
            if (Item.Meta.Kind == ItemKind.Active && signal.SkillItem != Item) return;
            var context = new ItemUseContext(signal.Trigger, signal.Player, signal.Position, signal.Enemies,
                signal.SkillItem, signal.DamageInfo, signal.CurrentHealth, signal.MaxHealth) { Runtime = this };
            if (Condition != null && !Condition(context) || !Effect.CanExecute(context)) return;
            executing = true;
            try
            {
                Effect.Execute(context);
                RemainingCooldown = Cooldown;
                signal.Executed = true;
            }
            finally { executing = false; }
        }

        private void Tick(float deltaTime)
        {
            if (Owner == null) { Dispose(); return; }
            RemainingCooldown = Mathf.Max(0, RemainingCooldown - deltaTime *
                TemporaryBuffs.Instance.Multiplier(Owner, BuffStat.CooldownRate));
        }

        // ItemEffect에서 MonoBehaviour 없이 코루틴 사용. 장착 해제 시 모두 중지한다.
        public void Run(IEnumerator routine)
        {
            if (!IsEquipped) return;
            IEnumerator tracked = null;
            tracked = Track(routine, () => routines.Remove(tracked));
            routines.Add(tracked);
            RuntimeTimer.Instance.StartCoroutine(tracked);
        }

        private IEnumerator Track(IEnumerator routine, Action finished)
        {
            try { while (IsEquipped && routine.MoveNext()) yield return routine.Current; }
            finally { (routine as IDisposable)?.Dispose(); finished(); }
        }

        public void OnCleanup(Action action) => cleanup.Add(action);

        public void Dispose()
        {
            if (!IsEquipped) return;
            IsEquipped = false;
            CombatEvents.Instance.Unsubscribe(Trigger, OnTrigger);
            if (RuntimeTimer.Instance != null)
            {
                RuntimeTimer.Instance.Tick -= Tick;
                foreach (var routine in routines.ToArray()) RuntimeTimer.Instance.StopCoroutine(routine);
            }
            routines.Clear();
            foreach (var action in cleanup) action();
            cleanup.Clear();
            Effect.OnUnequip(this);
            Owner = null;
        }
    }
}
