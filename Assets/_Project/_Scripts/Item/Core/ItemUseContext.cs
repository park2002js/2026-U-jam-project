using System.Collections.Generic;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Player;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    public sealed class ItemUseContext
    {
        public ItemTrigger Trigger { get; }
        public PlayerStatus Player { get; }
        public ItemData SkillItem { get; }
        // Shooting이 빗나가면 빈 배열이 아니라 null이다.
        public IReadOnlyList<EnemyBase> Enemies { get; }
        public Vector3 Position { get; }
        public DamageInfo DamageInfo { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
        public bool Executed { get; internal set; }
        public ItemRuntime Runtime { get; internal set; }

        public ItemUseContext(ItemTrigger trigger, PlayerStatus player, Vector3 position = default,
            IReadOnlyList<EnemyBase> enemies = null, ItemData skillItem = null, DamageInfo damageInfo = default,
            float currentHealth = 0f, float maxHealth = 0f)
        {
            Trigger = trigger; Player = player; Position = position; Enemies = enemies;
            SkillItem = skillItem; DamageInfo = damageInfo; CurrentHealth = currentHealth; MaxHealth = maxHealth;
        }
    }
}
