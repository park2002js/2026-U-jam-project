using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 플레이어에게 적용된 버프/피해를 화면 테두리 색으로 알린다.
    /// 순간 효과(Flash)와 상시 효과(Rage)를 각각 다른 Image로 관리한다.
    /// </summary>
    public class ScreenEffectManager : MonoBehaviour
    {
        public static ScreenEffectManager Instance { get; private set; }

        [Header("References")]
        [SerializeField, Tooltip("순간 효과용 비네트 (아드레날린, 긴급수리, 집념, 리저렉션)")]
        private Image _flashVignette;

        [SerializeField, Tooltip("상시 효과용 비네트 (광전사의 분노)")]
        private Image _rageVignette;

        [Header("상시 효과 설정")]
        [SerializeField] private Color _rageColor = new Color(1f, 0.1f, 0.1f);
        [SerializeField, Min(0f)] private float _rageMaxAlpha = 0.5f;   // 4단계일 때의 알파
        [SerializeField, Min(0f)] private float _rageFadeSpeed = 3f;    // 단계 변화 시 전환 속도

        private Coroutine _flashRoutine;
        private float _rageTarget;   // 목표 알파 (단계에 따라 결정)

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            SetAlpha(_flashVignette, 0f);
            SetAlpha(_rageVignette, 0f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 순간 효과를 띄운다. 이미 떠 있으면 새 효과로 덮어쓴다.
        /// </summary>
        /// <param name="color">테두리 색</param>
        /// <param name="peakAlpha">가장 진할 때의 알파 (0~1)</param>
        /// <param name="fadeIn">최대 밝기까지 걸리는 시간</param>
        /// <param name="hold">최대 밝기를 유지하는 시간</param>
        /// <param name="fadeOut">사라지는 데 걸리는 시간</param>
        public void Flash(Color color, float peakAlpha, float fadeIn, float hold, float fadeOut)
        {
            if (_flashVignette == null) return;

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(color, peakAlpha, fadeIn, hold, fadeOut));
        }

        private IEnumerator FlashRoutine(Color color, float peakAlpha, float fadeIn, float hold, float fadeOut)
        {
            _flashVignette.color = new Color(color.r, color.g, color.b, 0f);

            // 나타남
            for (float t = 0f; t < fadeIn; t += Time.deltaTime)
            {
                SetAlpha(_flashVignette, Mathf.Lerp(0f, peakAlpha, t / fadeIn));
                yield return null;
            }
            SetAlpha(_flashVignette, peakAlpha);

            // 유지
            if (hold > 0f) yield return new WaitForSeconds(hold);

            // 사라짐
            for (float t = 0f; t < fadeOut; t += Time.deltaTime)
            {
                SetAlpha(_flashVignette, Mathf.Lerp(peakAlpha, 0f, t / fadeOut));
                yield return null;
            }
            SetAlpha(_flashVignette, 0f);

            _flashRoutine = null;
        }

        /// <summary>
        /// 광전사의 분노 단계를 설정한다. 0이면 꺼지고, 1~4 단계에 따라 진해진다.
        /// </summary>
        public void SetRageStep(int step, int maxStep = 4)
        {
            if (_rageVignette == null) return;

            step = Mathf.Clamp(step, 0, maxStep);
            _rageTarget = maxStep > 0 ? _rageMaxAlpha * step / maxStep : 0f;
        }

        /// <summary>모든 화면 효과를 즉시 끈다. (게임오버, 씬 전환 등)</summary>
        public void ClearAll()
        {
            if (_flashRoutine != null) { StopCoroutine(_flashRoutine); _flashRoutine = null; }
            SetAlpha(_flashVignette, 0f);

            _rageTarget = 0f;
            SetAlpha(_rageVignette, 0f);
        }

        // 분노 알파를 목표값으로 부드럽게 따라가게 한다
        private void Update()
        {
            if (_rageVignette == null) return;

            float current = _rageVignette.color.a;
            if (Mathf.Approximately(current, _rageTarget)) return;

            float next = Mathf.MoveTowards(current, _rageTarget, _rageFadeSpeed * Time.deltaTime * _rageMaxAlpha);
            _rageVignette.color = new Color(_rageColor.r, _rageColor.g, _rageColor.b, next);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            if (image == null) return;
            Color color = image.color;
            image.color = new Color(color.r, color.g, color.b, alpha);
        }
    }
}