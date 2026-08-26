using System.Collections.Generic;
using UJam.Runtime.Player;
using UnityEngine;

namespace UJam.Runtime.Item
{
    public class ItemEffectExecutor : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerShooter shooter;
        [SerializeField, Min(1)] private int maxActiveEffects = 2;

        private readonly List<ActiveEffect> activeEffects = new();

        private void OnEnable()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (shooter == null) shooter = GetComponent<PlayerShooter>();
            if (shooter != null) shooter.OnShootingHit += OnShootingHit;
        }

        private void OnDisable()
        {
            if (shooter != null) shooter.OnShootingHit -= OnShootingHit;
            foreach (ActiveEffect effect in activeEffects) effect.Finish();
            activeEffects.Clear();
        }

        private void Update()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (!activeEffects[i].Update(Time.deltaTime)) continue;
                activeEffects[i].Finish();
                activeEffects.RemoveAt(i);
            }
        }

        private void OnShootingHit(ItemUseContext context)
        {
            if (inventory == null) return;
            // 총알 도착 시 보유 중인 아이템만 적용한다. 최초 명중 적의 생존 여부와는 무관하다.
            foreach (string itemId in new List<string>(inventory.Items.Keys)) Execute(ItemData.Load(itemId), context);
        }

        public void Execute(ItemData item, ItemUseContext context)
        {
            if (item == null || item.Id == ItemData.NullId) return;
            foreach (ItemEffect effect in item.Effects)
            {
                if (effect == null || effect.IsShootingHitEffect != context.IsShootingHit) continue;
                if (effect.IsShootingHitEffect)
                {
                    effect.Apply(context);
                    continue;
                }
                if (activeEffects.Count < maxActiveEffects) activeEffects.Add(new ActiveEffect(effect, context));
            }
        }
    }
}
