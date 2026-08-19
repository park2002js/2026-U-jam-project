using UnityEngine;
using UnityEngine.InputSystem;
using UJam.Runtime.Item;

public class ItemTestTrigger : MonoBehaviour
{
    [SerializeField] private ItemEffectExecutor executor;  // 씬의 Executor
    [SerializeField] private ItemData item;                // 발동할 아이템 (도트뎀 효과 담긴 SO)
    [SerializeField] private GameObject targetEnemy;       // 도트뎀 걸 대상 적

    private void Update()
    {
        // J를 누르면 조건 참 → 효과 발동
        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            var context = new ItemUseContext(
                user: gameObject,
                target: targetEnemy);

            executor.Execute(item, context);
            Debug.Log($"[ItemTest] J 입력 → {item?.DisplayName} 발동");
        }
    }
}