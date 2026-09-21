using System;
using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>선택된 Trigger를 Execute에 연결하고 장착 수명을 관리한다. 쿨타임 판단은 SkillManager의 책임이다.</summary>
    public sealed class ItemRuntime
    {
        public ItemTrigger Trigger { get; }
        public ItemEffect Effect { get; }
        public PlayerStatus Owner { get; private set; }
        public LayerMask EnemyMask { get; private set; }
        public Item Item { get; internal set; }
        private bool equipped;
        private readonly List<IEnumerator> routines = new();
        private readonly List<GameObject> objects = new();
        private bool executing;

        /// <summary>Catalog에서 트리거 하나와 해당 아이템 전용 Effect를 지정한다.</summary>
        public ItemRuntime(ItemTrigger trigger, ItemEffect effect)
        { Trigger = trigger; Effect = effect ?? throw new ArgumentNullException(nameof(effect)); }

        internal void Equip(PlayerStatus owner, LayerMask enemyMask)
        {
            if (owner == null || equipped) throw new InvalidOperationException("소유자를 확인하거나 먼저 장착 해제하세요.");
            equipped = true;
            Owner = owner; EnemyMask = enemyMask;
            RuntimeTimer.Ensure();
            EventManager.Instance.Subscribe(Trigger, Execute);
            var context = new ItemUseContext(Item, new ItemEvent(ItemTrigger.Equipped, owner));
            try
            {
                Effect.OnEquip(context);
                if (Trigger == ItemTrigger.Equipped) Execute(context.Signal);
            }
            catch { Unequip(); throw; }
        }

        /// <summary>중앙 이벤트가 자동 호출한다. 소유자와 액티브 아이템 식별 후 Effect를 실행한다.</summary>
        public void Execute(ItemEvent signal)
        {
            // 폭발이 만든 새 처치도 집계한다. 그 외 이벤트의 재진입은 중복 발동을 막는다.
            if (Owner == null || signal.Player != Owner || (executing && signal.Trigger != ItemTrigger.EnemyKilled)) return;
            if (Item.Meta.Kind == ItemKind.Active && signal.SkillItem != Item) return;
            if (signal.Trigger == ItemTrigger.BeforeDeath && signal.DeathPrevented) return;
            var context = new ItemUseContext(Item, signal);
            if (!Effect.CanExecute(context)) return;
            executing = true;
            try { Effect.Execute(context); signal.Executed = true; }
            finally { executing = false; }
        }

        /// <summary>Effect의 지연 작업을 등록한다. Unequip에서 중지하고 IEnumerator의 finally도 실행한다.</summary>
        public void Run(IEnumerator routine)
        {
            if (Owner == null) return;
            IEnumerator tracked = null;
            tracked = TrackRoutine(routine, () => routines.Remove(tracked));
            routines.Add(tracked);
            RuntimeTimer.Ensure().StartCoroutine(tracked);
        }
        private IEnumerator TrackRoutine(IEnumerator routine, Action done)
        {
            try { while (Owner != null && routine.MoveNext()) yield return routine.Current; }
            finally { (routine as IDisposable)?.Dispose(); done(); }
        }

        /// <summary>효과가 만든 장판/미끼/VFX를 등록하여 장착 해제 시 남지 않도록 한다.</summary>
        public void Track(GameObject instance) { if (instance != null) { objects.RemoveAll(x => x == null); objects.Add(instance); } }

        internal void Unequip()
        {
            if (!equipped) return;
            equipped = false;
            var owner = Owner;
            EventManager.Instance.Unsubscribe(Trigger, Execute);
            Owner = null;
            foreach (var routine in routines.ToArray())
            {
                if (RuntimeTimer.Instance != null) RuntimeTimer.Instance.StopCoroutine(routine);
                (routine as IDisposable)?.Dispose();
            }
            routines.Clear();
            foreach (var instance in objects)
                if (instance != null) { instance.SetActive(false); UnityEngine.Object.Destroy(instance); }
            objects.Clear();
            if (owner != null) Effect.OnUnequip(new ItemUseContext(Item, new ItemEvent(ItemTrigger.Equipped, owner)));
            BuffManager.Instance.RemoveSource(Item);
        }
    }
}
