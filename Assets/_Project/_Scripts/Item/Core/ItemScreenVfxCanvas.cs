using System.Collections.Generic;
using UJam.Runtime.Player;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>기존 Canvas 아래 준비된 여섯 화면 연출을 플레이어와 연결한다.</summary>
    [DefaultExecutionOrder(-2100)]
    public sealed class ItemScreenVfxCanvas : MonoBehaviour
    {
        [SerializeField] private PlayerStatus player;
        [SerializeField] private ItemScreenVfx[] effects;
        private static readonly List<ItemScreenVfxCanvas> roots = new();

        public void Bind(PlayerStatus owner)
        {
            Clear();
            player = owner;
        }

        public void Clear()
        {
            if (effects == null) return;
            foreach (var effect in effects) if (effect != null) effect.Expire();
        }

        public ItemScreenVfx GetView(string id)
        {
            if (effects == null) return null;
            foreach (var effect in effects)
                if (effect != null && effect.ItemId == id) return effect;
            return null;
        }

        public static ItemScreenVfxCanvas FindFor(PlayerStatus owner)
        {
            if (owner == null) return null;
            foreach (var root in roots)
                if (root != null && root.isActiveAndEnabled && root.player == owner) return root;
            return null;
        }

        private void OnEnable() { if (!roots.Contains(this)) roots.Add(this); }
        private void OnDisable() { Clear(); roots.Remove(this); }
        private void OnDestroy() => roots.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => roots.Clear();
    }
}
