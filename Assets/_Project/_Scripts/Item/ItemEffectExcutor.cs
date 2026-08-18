// 아이템 효과를 발동시키기 위해 사용하는 객체입니다.
// 아이템 내부에 정의된 여러 효과들을 모두 발동시킵니다. (피가 10 증가하고 힘이 10 증가합니다.. 등)
// 이 객체로 Apply하는 것은 단 하나의 아이템에 대해서만 가능합니다.
using UnityEngine;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Item
{
    public sealed class ItemEffectExecutor : MonoBehaviour
    {
        private readonly List<ItemData> activeItems = new();
        private readonly List<ActiveEffect> activeEffects = new();

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].Update(dt))
                {
                    activeEffects[i].Finish();
                    activeEffects.RemoveAt(i);
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

        // 아이템의 효과들을 발동 (지속시간 동안 유지)
        public void Execute(ItemData item, ItemUseContext context)
        {
            if (item == null) return;

            // 이번 아이템이 효과를 몇 개 거는지 미리 로그
            Debug.Log($"[Executor] '{item.DisplayName}' 발동 → 효과 {item.Effects.Count}개 등록 예정");

            foreach (ItemEffect effect in item.Effects)
            {
                if (effect == null) continue;
                activeEffects.Add(new ActiveEffect(effect, context));
            }

            Debug.Log($"[Executor] 현재 총 활성 효과 수: {activeEffects.Count}");
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
