using System.Collections;
using UnityEngine;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 오브젝트가 활성화될 때마다 대상 RectTransform을 아래에서 원래 위치로 밀어 올리는 등장 연출입니다.
    /// 정비 Phase의 Tablet, 레벨 선택 창, 결산 창 등에서 같은 움직임을 공유합니다.
    /// Time.timeScale과 관계없이(unscaled) 재생되며, 연출 중에는 CanvasGroup으로 입력을 막습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class UISlideIn : MonoBehaviour
    {
        [SerializeField, Tooltip("움직일 대상입니다. 비어 있으면 자기 자신을 움직입니다.")]
        private RectTransform _target;
        [SerializeField, Tooltip("연출 중 투명도와 입력 차단에 사용할 CanvasGroup입니다. 비어 있으면 대상에서 찾거나 추가합니다.")]
        private CanvasGroup _canvasGroup;
        [SerializeField, Min(0.01f)] private float _duration = 0.5f;
        [SerializeField, Min(0f)] private float _delay;
        [SerializeField, Tooltip("0이면 대상 높이 + 여유만큼 화면 아래에서 출발합니다.")]
        private float _distance;
        [SerializeField, Range(0f, 3f), Tooltip("도착 지점을 살짝 지나쳤다가 돌아오는 정도입니다(0이면 감속만).")]
        private float _overshoot = 1.1f;
        [SerializeField, Tooltip("투명도도 함께 0에서 1로 올립니다.")]
        private bool _fade = true;

        private Vector2 _restPosition;
        private bool _hasRest;

        /// <summary>현재 연출이 재생 중인지 여부입니다.</summary>
        public bool IsPlaying { get; private set; }

        private RectTransform Target => _target != null ? _target : (RectTransform)transform;

        private void Awake()
        {
            CaptureRest();
            if (_canvasGroup == null) _canvasGroup = Target.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = Target.gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            CaptureRest();
            StopAllCoroutines();
            StartCoroutine(Play());
        }

        /// <summary>
        /// 비활성화될 때 원위치로 되돌려 다음 등장 연출이 항상 같은 지점에서 끝나도록 합니다.
        /// </summary>
        private void OnDisable()
        {
            StopAllCoroutines();
            if (_hasRest) Target.anchoredPosition = _restPosition;
            SetGroup(1f, true);
            IsPlaying = false;
        }

        private void CaptureRest()
        {
            if (_hasRest) return;
            _restPosition = Target.anchoredPosition;
            _hasRest = true;
        }

        private IEnumerator Play()
        {
            IsPlaying = true;
            RectTransform target = Target;
            float distance = _distance > 0f ? _distance : target.rect.height + 200f;
            Vector2 start = _restPosition + Vector2.down * distance;
            target.anchoredPosition = start;
            SetGroup(_fade ? 0f : 1f, false);

            float wait = 0f;
            while (wait < _delay)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            float t = 0f;
            while (t < _duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / _duration);
                target.anchoredPosition = Vector2.LerpUnclamped(start, _restPosition, EaseOutBack(p, _overshoot));
                if (_fade) SetGroup(Mathf.Clamp01(p * 2.5f), false);
                yield return null;
            }

            target.anchoredPosition = _restPosition;
            SetGroup(1f, true);
            IsPlaying = false;
        }

        private void SetGroup(float alpha, bool interactive)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = alpha;
            _canvasGroup.interactable = interactive;
            _canvasGroup.blocksRaycasts = interactive;
        }

        private static float EaseOutBack(float x, float overshoot)
        {
            if (overshoot <= 0f) return 1f - Mathf.Pow(1f - x, 3f);
            float c3 = overshoot + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + overshoot * Mathf.Pow(x - 1f, 2f);
        }
    }
}
