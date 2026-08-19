using UnityEngine;

namespace UJam.Runtime.Item
{
    // 효과 하나가 한 대상에게 걸린 인스턴스. 남은시간·틱타이머·대상을 보유한다.
    public sealed class ActiveEffect
    {
        public ItemEffect Effect { get; }
        public ItemUseContext Context { get; }

        private float timeLeft;    // 남은 지속시간
        private float tickTimer;   // 다음 틱까지 남은 시간

        public ActiveEffect(ItemEffect effect, ItemUseContext context)
        {
            Effect = effect;
            Context = context;
            timeLeft = effect.Duration;
            tickTimer = effect.TickInterval;

            // 걸리는 순간 1회 Apply (버프 적용 등)
            effect.Apply(context);
        }

        // 매 프레임 Executor가 호출. 수명이 끝났으면 true 반환.
        public bool Update(float deltaTime)
        {
            // 도트 등 반복(Tick) 처리
            tickTimer -= deltaTime;
            if (tickTimer <= 0f)
            {
                Debug.Log($"[ActiveEffect] Tick 실행: {Effect.GetType().Name}");
                Effect.Tick(Context);
                tickTimer += Effect.TickInterval;
            }

            // 지속시간이 0인 효과는 Apply만 하고 바로 종료
            if (Effect.Duration <= 0f)
                return true;

            timeLeft -= deltaTime;
            // Debug.Log($"[ActiveEffect] 남은 시간: {timeLeft}");
            return timeLeft <= 0f;
        }

        // 종료 시 Executor가 호출 (버프 되돌리기 등)
        public void Finish()
        {
            Effect.Remove(Context);
        }
    }
}