using UnityEngine;
using UnityEngine.EventSystems;

namespace UJam.Runtime.UI
{
    public class UITitleBtnFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Vector3 originalScale;
        private Vector3 targetScale;

        void Start()
        {
            // 원래 크기 저장
            originalScale = transform.localScale;
            // 10% 커진 목표 크기 (1.1배)
            targetScale = originalScale * 1.1f;
        }

        // 마우스 커서가 버튼 위로 들어왔을 때 (10% 확대)
        public void OnPointerEnter(PointerEventData eventData)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleOverTime(targetScale, 0.1f));
        }

        // 마우스 커서가 버튼 밖으로 나갔을 때 (원래 크기로)
        public void OnPointerExit(PointerEventData eventData)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleOverTime(originalScale, 0.1f));
        }

        private System.Collections.IEnumerator ScaleOverTime(Vector3 toScale, float duration)
        {
            float counter = 0;
            Vector3 startScale = transform.localScale;

            while (counter < duration)
            {
                counter += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, toScale, counter / duration);
                yield return null;
            }
            transform.localScale = toScale;
        }
    }
}