using UnityEngine;

namespace UJam.Runtime.Item
{
    public sealed class ActiveEffect
    {
        public ItemEffect Effect { get; }
        public ItemUseContext Context { get; }

        // 이 효과가 대상에게 붙인 이펙트 (SO가 아닌 여기서 보관)
        private GameObject visualInstance;

        private float timeLeft;
        private float tickTimer;

        public ActiveEffect(ItemEffect effect, ItemUseContext context)
        {
            Effect = effect;
            Context = context;
            timeLeft = effect.Duration;
            tickTimer = effect.TickInterval;

            string targetName = context.Target != null ? context.Target.name : "없음";
            Debug.Log($"<color=lime>[효과 부여] {effect.name} → 대상: {targetName}, 지속 {effect.Duration}초</color>");

            effect.Apply(context);

            // 이펙트 프리팹이 있으면 대상에게 붙임
            visualInstance = effect.SpawnVisual(context);
        }

        public bool Update(float deltaTime)
        {
            tickTimer -= deltaTime;
            if (tickTimer <= 0f)
            {
                Effect.Tick(Context);
                tickTimer += Effect.TickInterval;
            }

            if (Effect.Duration <= 0f) return true;

            timeLeft -= deltaTime;
            return timeLeft <= 0f;
        }

        public void Finish()
        {
            Effect.Remove(Context);

            // 붙였던 이펙트 제거
            if (visualInstance != null) Object.Destroy(visualInstance);
        }
    }
}