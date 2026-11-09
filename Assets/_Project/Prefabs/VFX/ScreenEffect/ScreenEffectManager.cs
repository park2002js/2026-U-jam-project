using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 플레이어에게 적용된 버프/피해를 화면 테두리 색으로 알린다.
    /// 순간 효과(Flash)와 상시 효과(Rage)를 각각 다른 Image로 관리하며,
    /// 회복이나 부활처럼 강조가 필요한 경우 반짝임과 날개를 함께 띄운다.
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
        [SerializeField, Min(0f)] private float _rageMaxAlpha = 0.375f;   // 최대 단계일 때의 알파
        [SerializeField, Min(0f)] private float _rageFadeSpeed = 3f;      // 단계 변화 시 전환 속도

        [Header("반짝임 설정 (긴급 수리)")]
        [SerializeField, Tooltip("십자 모양 스프라이트")]
        private Sprite _sparkleSprite;

        [SerializeField, Tooltip("반짝임이 생성될 부모. 비우면 이 오브젝트를 사용한다.")]
        private RectTransform _sparkleParent;

        [SerializeField, Min(1), Tooltip("미리 만들어둘 반짝임 개수. 한쪽당 개수 × 2보다 커야 한다.")]
        private int _sparklePoolSize = 16;

        [SerializeField, Tooltip("반짝임 크기 범위(픽셀)")]
        private Vector2 _sparkleSizeRange = new Vector2(24f, 48f);

        [SerializeField, Range(0f, 0.5f), Tooltip("화면 폭 대비 좌우 띠 너비. 작을수록 가장자리에 붙는다.")]
        private float _sparkleEdgeRatio = 0.18f;

        [SerializeField, Tooltip("위로 떠오르는 속도(픽셀/초)")]
        private float _sparkleRiseSpeed = 40f;

        [SerializeField, Min(0.1f)] private float _sparkleLifetime = 0.8f;

        [Header("날개 설정 (리저렉션)")]
        [SerializeField, Tooltip("날개 스프라이트. 한쪽을 향한 한 장이면 된다.")]
        private Sprite _wingSprite;

        [SerializeField, Tooltip("날개가 생성될 부모. 비우면 이 오브젝트를 사용한다.")]
        private RectTransform _wingParent;

        [SerializeField] private Color _wingColor = new Color(1f, 0.85f, 0.3f);

        [SerializeField, Range(0f, 1f), Tooltip("날개가 가장 진할 때의 알파")]
        private float _wingAlpha = 0.8f;

        [SerializeField, Tooltip("날개 하나의 크기(픽셀)")]
        private Vector2 _wingSize = new Vector2(320f, 320f);

        [SerializeField, Tooltip("화면 중앙에서 좌우로 벌어지는 거리(픽셀)")]
        private float _wingSpread = 180f;

        [SerializeField, Tooltip("펼쳐지며 추가로 더 벌어지는 거리(픽셀)")]
        private float _wingSpreadMotion = 60f;

        [SerializeField, Min(0.1f)] private float _wingLifetime = 1.4f;

        private Coroutine _flashRoutine;
        private float _rageTarget;   // 목표 알파 (단계에 따라 결정)

        private readonly Queue<Image> _sparklePool = new();
        private readonly List<Image> _activeSparkles = new();

        private Image _leftWing;
        private Image _rightWing;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            SetAlpha(_flashVignette, 0f);
            SetAlpha(_rageVignette, 0f);

            BuildSparklePool();
            BuildWings();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 반짝임용 Image를 미리 만들어 풀에 넣어둔다
        private void BuildSparklePool()
        {
            if (_sparkleSprite == null) return;

            RectTransform parent = _sparkleParent != null ? _sparkleParent : (RectTransform)transform;

            for (int i = 0; i < _sparklePoolSize; i++)
            {
                var go = new GameObject("Sparkle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                var image = go.GetComponent<Image>();
                image.sprite = _sparkleSprite;
                image.raycastTarget = false;   // 클릭을 가로채지 않게 한다

                go.SetActive(false);
                _sparklePool.Enqueue(image);
            }
        }

        // 좌우 날개 Image를 하나씩 만들어둔다. 한쪽은 스케일을 반전시켜 대칭을 만든다.
        private void BuildWings()
        {
            if (_wingSprite == null) return;

            RectTransform parent = _wingParent != null ? _wingParent : (RectTransform)transform;

            _rightWing = CreateWing(parent, "Wing_Right", 1f);
            _leftWing = CreateWing(parent, "Wing_Left", -1f);
        }

        private Image CreateWing(RectTransform parent, string name, float direction)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = _wingSize;

            // direction이 -1이면 좌우 반전되어 반대쪽 날개가 된다
            rect.localScale = new Vector3(direction, 1f, 1f);

            var image = go.GetComponent<Image>();
            image.sprite = _wingSprite;
            image.raycastTarget = false;

            go.SetActive(false);
            return image;
        }

        /// <summary>
        /// 순간 효과를 띄운다. 이미 떠 있으면 새 효과로 덮어쓴다.
        /// </summary>
        /// <param name="color">테두리 색</param>
        /// <param name="peakAlpha">가장 진할 때의 알파 (0~1)</param>
        /// <param name="fadeIn">최대 밝기까지 걸리는 시간</param>
        /// <param name="hold">최대 밝기를 유지하는 시간</param>
        /// <param name="fadeOut">사라지는 데 걸리는 시간</param>
        /// <param name="sparkleCount">한쪽 가장자리당 반짝임 개수. 0이면 띄우지 않는다.</param>
        public void Flash(Color color, float peakAlpha, float fadeIn, float hold, float fadeOut, int sparkleCount = 0)
        {
            if (_flashVignette == null) return;

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(color, peakAlpha, fadeIn, hold, fadeOut));

            if (sparkleCount > 0) SpawnSparkles(color, sparkleCount);
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

        // 좌우 가장자리 띠 안의 임의 위치에 반짝임을 흩뿌린다. countPerSide는 한쪽당 개수다.
        private void SpawnSparkles(Color color, int countPerSide)
        {
            if (_sparkleSprite == null) return;

            RectTransform parent = _sparkleParent != null ? _sparkleParent : (RectTransform)transform;
            float halfWidth = parent.rect.width * 0.5f;
            float halfHeight = parent.rect.height * 0.5f;
            float bandWidth = parent.rect.width * _sparkleEdgeRatio;

            // 왼쪽(-1)과 오른쪽(+1)에 각각 countPerSide개씩 생성한다
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < countPerSide; i++)
                {
                    Image sparkle = GetSparkle();
                    if (sparkle == null) return;   // 풀이 비면 더 만들지 않는다

                    float x = side * (halfWidth - Random.Range(0f, bandWidth));
                    float y = Random.Range(-halfHeight * 0.7f, halfHeight * 0.7f);

                    var rect = (RectTransform)sparkle.transform;
                    rect.anchoredPosition = new Vector2(x, y);

                    float size = Random.Range(_sparkleSizeRange.x, _sparkleSizeRange.y);
                    rect.sizeDelta = new Vector2(size, size);
                    rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f));

                    sparkle.color = new Color(color.r, color.g, color.b, 0f);
                    sparkle.gameObject.SetActive(true);
                    _activeSparkles.Add(sparkle);

                    // 각자 조금씩 늦게 시작해 동시에 터지지 않게 한다
                    StartCoroutine(SparkleRoutine(sparkle, Random.Range(0f, 0.25f)));
                }
            }
        }

        private IEnumerator SparkleRoutine(Image sparkle, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            var rect = (RectTransform)sparkle.transform;
            Color baseColor = sparkle.color;
            float half = _sparkleLifetime * 0.5f;

            for (float t = 0f; t < _sparkleLifetime; t += Time.deltaTime)
            {
                // 전반부는 밝아지고 후반부는 사라진다
                float alpha = t < half ? t / half : 1f - (t - half) / half;
                sparkle.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                rect.anchoredPosition += Vector2.up * _sparkleRiseSpeed * Time.deltaTime;
                yield return null;
            }

            ReturnSparkle(sparkle);
        }

        private Image GetSparkle()
        {
            return _sparklePool.Count > 0 ? _sparklePool.Dequeue() : null;
        }

        private void ReturnSparkle(Image sparkle)
        {
            if (sparkle == null) return;

            sparkle.gameObject.SetActive(false);
            _activeSparkles.Remove(sparkle);
            _sparklePool.Enqueue(sparkle);
        }

        /// <summary>부활 등에서 화면 중앙에 날개를 펼친다.</summary>
        public void ShowWings()
        {
            if (_leftWing == null || _rightWing == null) return;

            StartCoroutine(WingRoutine());
        }

        private IEnumerator WingRoutine()
        {
            var leftRect = (RectTransform)_leftWing.transform;
            var rightRect = (RectTransform)_rightWing.transform;

            // 고정된 자리에 배치한다
            leftRect.anchoredPosition = new Vector2(-_wingSpread, 0f);
            rightRect.anchoredPosition = new Vector2(_wingSpread, 0f);

            _leftWing.gameObject.SetActive(true);
            _rightWing.gameObject.SetActive(true);

            float half = _wingLifetime * 0.5f;

            for (float t = 0f; t < _wingLifetime; t += Time.deltaTime)
            {
                // 전반부에 나타나고 후반부에 사라진다
                float alpha = (t < half ? t / half : 1f - (t - half) / half) * _wingAlpha;
                _leftWing.color = new Color(_wingColor.r, _wingColor.g, _wingColor.b, alpha);
                _rightWing.color = _leftWing.color;

                yield return null;
            }

            _leftWing.gameObject.SetActive(false);
            _rightWing.gameObject.SetActive(false);
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
            StopAllCoroutines();
            _flashRoutine = null;

            SetAlpha(_flashVignette, 0f);
            _rageTarget = 0f;
            SetAlpha(_rageVignette, 0f);

            foreach (Image sparkle in _activeSparkles.ToArray()) ReturnSparkle(sparkle);

            if (_leftWing != null) _leftWing.gameObject.SetActive(false);
            if (_rightWing != null) _rightWing.gameObject.SetActive(false);
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