using UnityEngine;

namespace UJam.Runtime.Item
{
    // 효과 하나가 한 대상에게 걸린 인스턴스. 남은시간·틱타이머·대상을 보유한다.
    public sealed class ActiveEffect
    {
        public ItemEffect Effect { get; }
        public ItemUseContext Context { get; }

        private float timeLeft;
        private float tickTimer;

        public ActiveEffect(ItemEffect effect, ItemUseContext context)
        {
            Effect = effect;
            Context = context;
            timeLeft = effect.Duration;
            tickTimer = effect.TickInterval;

            string targetName = context.Target != null ? context.Target.name : "null";
            Debug.Log($"[Effect] {effect.name} 시작 → 대상: {targetName}, 지속시간: {effect.Duration}s");

            effect.Apply(context);
        }

        public bool Update(float deltaTime)
        {
            tickTimer -= deltaTime;
            if (tickTimer <= 0f)
            {
                string targetName = Context.Target != null ? Context.Target.name : "null";
                Debug.Log($"[Effect] {Effect.name} Tick → 대상: {targetName}, 남은시간: {timeLeft:F1}s");

                Effect.Tick(Context);
                tickTimer += Effect.TickInterval;
            }

            if (Effect.Duration <= 0f)
                return true;

            timeLeft -= deltaTime;
            return timeLeft <= 0f;
        }

        public void Finish()
        {
            string targetName = Context.Target != null ? Context.Target.name : "null";
            Debug.Log($"[Effect] {Effect.name} 종료 → 대상: {targetName}");

            Effect.Remove(Context);
        }
    }
}
