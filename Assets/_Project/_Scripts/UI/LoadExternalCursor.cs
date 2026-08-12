using UnityEngine;

public class LoadExternalCursor : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("에셋 폴더의 aim.png 텍스처를 직접 넣어주세요.")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotSpot = Vector2.zero;

    void Start()
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
            Debug.Log("[Cursor Success] 커서가 변경되었습니다.");
        }
        else
        {
            Debug.LogError("[Cursor Error] 커서 텍스처가 할당되지 않았습니다!");
        }
    }
}