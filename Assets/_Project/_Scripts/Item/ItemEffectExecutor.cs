// 아이템 효과를 발동시키기 위해 사용하는 객체입니다.
// 아이템 내부에 정의된 여러 효과들을 모두 발동시킵니다. (피가 10 증가하고 힘이 10 증가합니다.. 등)
// 이 객체로 Apply하는 것은 단 하나의 아이템에 대해서만 가능합니다.
using UnityEngine;
using System.Collections.Generic;
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


        private void Awake()
        {
            // 효과들을 가져올 객체 정보를 받아온다거나 하는 등의 초기화
        }
        private void OnEnable()
        {
            // 이벤트 구독
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
        }

        // 아이템 장착 등, 어떤 효과를 등록할 때 사용
        public void RegisterItem(ItemData item)
        {
            activeItems.Add(item);
            // 패시브 효과 적용 : ApplyPassiveEffects(item);
        }

        // 아이템 제거 등, 어떤 효과를 해제할 때 사용
        public void UnregisterItem(ItemData item)
        {
            // 패시브 효과 제거 : RemovePassiveEffects(item);
            activeItems.Remove(item);
        }

        // 특정 아이템 효과를 발동
        public void Execute(ItemData item, ItemUseContext context)
        {
            if (item == null) return;

            foreach (ItemEffect effect in item.Effects)
            {
                if (effect == null) continue;
                activeEffects.Add(new ActiveEffect(effect, context));
            }
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