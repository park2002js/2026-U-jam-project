using UnityEngine;
using UnityEngine.InputSystem;
using UJam.Runtime.Item;

public class ItemTestTrigger : MonoBehaviour
{
    [SerializeField] private ItemEffectExecutor executor;
    [SerializeField] private GameObject targetEnemy;

    [Header("키별 아이템 (G / H / J)")]
    [SerializeField] private ItemData itemG;
    [SerializeField] private ItemData itemH;
    [SerializeField] private ItemData itemJ;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.gKey.wasPressedThisFrame) TryUse(itemG, "G");
        if (Keyboard.current.hKey.wasPressedThisFrame) TryUse(itemH, "H");
        if (Keyboard.current.jKey.wasPressedThisFrame) TryUse(itemJ, "J");
    }

    private void TryUse(ItemData item, string key)
    {
        if (item == null)
        {
            Debug.Log($"<color=yellow>[입력] {key} — 아이템이 지정되지 않음</color>");
            return;
        }

        Debug.Log($"<color=white>[입력] {key} 키 → '{item.DisplayName}' 사용</color>");

        var context = new ItemUseContext(user: gameObject, target: targetEnemy);
        executor.Execute(item, context);
    }
}