using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HitFeedbackUI : MonoBehaviour
{
    public static HitFeedbackUI Instance { get; private set; }

    [Header("히트마커 UI")]
    public Image hitmarkerImage;
    public float displayDuration = 0.1f; // 깜빡이는 시간

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 시작할 때는 무조건 숨김
        if (hitmarkerImage != null) hitmarkerImage.gameObject.SetActive(false);
    }

    public void ShowHitmarker()
    {
        if (hitmarkerImage != null)
        {
            StopAllCoroutines(); // 연속으로 맞출 경우 타이머 초기화
            StartCoroutine(HitmarkerRoutine());
        }
    }

    private IEnumerator HitmarkerRoutine()
    {
        hitmarkerImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(displayDuration);
        hitmarkerImage.gameObject.SetActive(false);
    }
}