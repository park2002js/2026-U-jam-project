// 아이템 효과를 발동시키기 위해 사용하는 객체입니다.
// 아이템 내부에 정의된 여러 효과들을 모두 발동시킵니다. (피가 10 증가하고 힘이 10 증가합니다.. 등)
// 이 객체로 Apply하는 것은 단 하나의 아이템에 대해서만 가능합니다.
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Item
{
    public sealed class ItemEffectExecutor : MonoBehaviour
    {
        [Header("효과 중첩 설정")]
        [SerializeField, Min(1)] private int maxActiveEffects = 2;  // 동시에 걸 수 있는 최대 효과 수

        private readonly List<ItemData> activeItems = new();
        private readonly List<ActiveEffect> activeEffects = new();

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].Update(dt))
                {
                    string effectName = activeEffects[i].Effect.name;
                    activeEffects[i].Finish();
                    activeEffects.RemoveAt(i);

                    Debug.Log($"<color=grey>[효과 종료] {effectName} 효과 사라짐 (남은 효과 {activeEffects.Count}/{maxActiveEffects})</color>");
                }
            }
        }

        public void RegisterItem(ItemData item)
        {
            if (item == null) return;
            activeItems.Add(item);
        }

        public void UnregisterItem(ItemData item)
        {
            if (item == null) return;
            activeItems.Remove(item);
        }

        // 아이템의 효과들을 발동 (최대 개수 제한 적용)
        public void Execute(ItemData item, ItemUseContext context)
        {
            if (item == null) return;

            foreach (ItemEffect effect in item.Effects)
            {
                if (effect == null) continue;

                // 최대 개수를 넘으면 등록 거부
                if (activeEffects.Count >= maxActiveEffects)
                {
                    Debug.Log($"<color=red>[효과 거부] {effect.name} — 최대 {maxActiveEffects}개까지만 중첩 가능</color>");
                    continue;
                }

                activeEffects.Add(new ActiveEffect(effect, context));
            }

            // 현재 걸려있는 효과 목록 출력
            PrintActiveEffects();
        }

        // 지금 걸려있는 효과들을 한 줄로 보여줌
        private void PrintActiveEffects()
        {
            if (activeEffects.Count == 0)
            {
                Debug.Log("[현재 효과] 없음");
                return;
            }

            string list = "";
            foreach (ActiveEffect ae in activeEffects)
                list += ae.Effect.name + ", ";

            Debug.Log($"<color=cyan>[현재 효과] {activeEffects.Count}/{maxActiveEffects}개 부여됨 → {list}</color>");
        }
    }
}
/*
    사용 예시
    
    private readonly ItemEffectExecutor effectExecutor = new();

    public void UseItem(ItemData item)
    {
        var context = new ItemUseContext(
            user: gameObject,
            target: gameObject);

        effectExecutor.Excute(item, context);
    }
*/