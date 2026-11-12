using System;
using System.Collections.Generic;
using UJam.Runtime.Player;
using UJam.Runtime.UI;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>씬 Canvas에 준비된 아이템별 연출. 소모품이 사라져도 시작한 연출은 끝까지 재생한다.</summary>
    public sealed class ItemScreenVfx : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField] private ScreenEffectManager visuals;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Range(0, 1)] private float alpha = .3f;
        [SerializeField, Min(0)] private float fadeIn = .2f, hold = 1f, fadeOut = .5f;
        [SerializeField] private Sprite sparkleIcon;
        [SerializeField, Min(0)] private int sparkleCount = 4;
        [SerializeField] private Vector2 sparkleSize = new Vector2(24, 48);
        [SerializeField] private float sparkleTilt;
        [SerializeField] private bool tintSparkle;
        [SerializeField] private Sprite centerIcon;
        [SerializeField] private Vector2 centerIconSize = new Vector2(240, 240);
        [SerializeField] private bool tintCenterIcon;
        [SerializeField] private bool persistent;

        private static readonly Dictionary<Item, ItemScreenVfx> active = new();
        private PlayerStatus owner;
        private Item source;
        public string ItemId => itemId;
        public bool IsPlaying => source != null;
        public int RageStep { get; private set; }

        public static ItemScreenVfx GetActive(Item item) => item != null && active.TryGetValue(item, out var view) ? view : null;

        public static void Play(ItemUseContext context)
        {
            var view = GetView(context);
            if (view == null) return;
            view.CancelInvoke(nameof(Expire));
            view.visuals.ClearAll();
            Canvas.ForceUpdateCanvases();
            view.visuals.Flash(view.color, view.alpha, view.fadeIn, view.hold, view.fadeOut);
            if (view.sparkleIcon != null)
                view.visuals.SpawnSparkles(view.sparkleIcon, view.tintSparkle ? view.color : Color.white,
                    view.sparkleCount, view.sparkleSize, view.sparkleTilt);
            if (view.centerIcon != null)
                view.visuals.ShowCenterIcon(view.centerIcon, view.tintCenterIcon ? view.color : Color.white, view.centerIconSize);
            float duration = view.fadeIn + view.hold + view.fadeOut;
            if (view.sparkleIcon != null && view.sparkleCount > 0) duration = Mathf.Max(duration, view.visuals.SparkleDuration);
            if (view.centerIcon != null) duration = Mathf.Max(duration, view.visuals.CenterIconDuration);
            view.Invoke(nameof(Expire), duration);
        }

        public static void SetRageStep(ItemUseContext context, int step)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            step = Mathf.Clamp(step, 0, 4);
            var view = GetActive(context.Item);
            if (view == null && step == 0) return;
            if (view == null) view = GetView(context);
            if (view == null || view.RageStep == step) return;
            view.CancelInvoke(nameof(Expire));
            view.RageStep = step;
            view.visuals.SetRageStep(step);
            if (step == 0) view.Invoke(nameof(Expire), view.visuals.RageFadeDuration + .05f);
        }

        public static void Stop(Item item)
        {
            var view = GetActive(item);
            if (view == null) return;
            view.Expire();
        }

        private static ItemScreenVfx GetView(ItemUseContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.Player == null || context.Item == null) return null;
            var current = GetActive(context.Item);
            if (current != null && current.owner == context.Player) return current;
            var canvas = ItemScreenVfxCanvas.FindFor(context.Player);
            var view = canvas != null ? canvas.GetView(context.Item.ID) : null;
            if (view == null || !view.isActiveAndEnabled || view.visuals == null || view.GetComponentInParent<Canvas>() == null)
            {
                Debug.LogError($"[ItemScreenVfx] 플레이어 Canvas의 {context.Item.ID}번 화면 연출 연결을 확인하세요.");
                return null;
            }
            if (current != null) current.Expire();
            view.Expire();
            view.owner = context.Player;
            view.source = context.Item;
            active[context.Item] = view;
            return view;
        }

        private void Update()
        {
            if (IsPlaying && (owner == null || owner.CurrentHealth <= 0 || persistent && !source.IsEquipped)) Expire();
        }

        internal void Expire()
        {
            if (source != null && active.TryGetValue(source, out var view) && view == this) active.Remove(source);
            CancelInvoke(nameof(Expire));
            if (visuals != null) visuals.ClearAll();
            source = null;
            owner = null;
            RageStep = 0;
        }

        private void OnDisable() => Expire();
        private void OnDestroy() => Expire();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => active.Clear();
    }
}
