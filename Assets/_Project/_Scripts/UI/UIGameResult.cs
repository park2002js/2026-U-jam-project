using System.Collections;
using TMPro;
using UJam.Runtime.Phase;
using UJam.Runtime.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 모든 Wave를 클리어했을 때 띄우는 결산 창입니다.
    /// 쓰러뜨린 적 수, 레벨 시작부터 클리어까지 걸린 시간, 사용한 재화를 숫자가 올라가는 연출과 함께 표시합니다.
    /// 표시 시점은 UIManager가 GameManager.OnGameClear를 받아 ShowClear()로 요청합니다.
    /// </summary>
    public class UIGameResult : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField, Tooltip("결산 창 전체(배경 dim 포함)입니다. 비어 있으면 이 오브젝트를 사용합니다.")]
        private GameObject _root;
        [SerializeField, Tooltip("팝업 연출을 적용할 창 본체입니다.")]
        private RectTransform _window;
        [SerializeField, Tooltip("배경 dim 이미지입니다. 서서히 어두워집니다.")]
        private Graphic _dim;
        [SerializeField, Min(0f), Tooltip("마지막 적이 쓰러진 뒤 창을 띄우기까지 기다리는 시간(초)입니다.")]
        private float _showDelay = 1.2f;

        [Header("Texts")]
        [SerializeField] private TMP_Text _subtitleText;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _spentText;
        [SerializeField, Tooltip("통계 행들(순서대로 하나씩 등장)입니다.")]
        private RectTransform[] _statRows = new RectTransform[0];
        [SerializeField, Min(0.05f)] private float _countDuration = 0.9f;

        [Header("Buttons")]
        [SerializeField] private Button _levelSelectButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;
        [SerializeField] private string _titleSceneName = SceneFlow.DefaultTitleSceneName;

        private bool _shown;

        private GameObject Root => _root != null ? _root : gameObject;

        private void Awake()
        {
            if (_levelSelectButton != null) _levelSelectButton.onClick.AddListener(GoLevelSelect);
            if (_retryButton != null) _retryButton.onClick.AddListener(Retry);
            if (_titleButton != null) _titleButton.onClick.AddListener(GoTitle);
        }

        private void OnDestroy()
        {
            if (_levelSelectButton != null) _levelSelectButton.onClick.RemoveListener(GoLevelSelect);
            if (_retryButton != null) _retryButton.onClick.RemoveListener(Retry);
            if (_titleButton != null) _titleButton.onClick.RemoveListener(GoTitle);
        }

        /// <summary>
        /// UIManager가 게임 클리어 신호를 받은 뒤 호출합니다. 통계를 확정하고 창을 연출과 함께 표시합니다.
        /// </summary>
        public void ShowClear()
        {
            if (_shown) return;
            _shown = true;

            int kills = WaveController.Instance != null ? WaveController.Instance.TotalDeadEnemyCount : 0;
            int waves = WaveController.Instance != null ? WaveController.Instance.WaveCount : 0;
            float playTime = GameManager.Instance != null ? GameManager.Instance.PlayTime : Time.timeSinceLevelLoad;
            long spent = Wallet.Instance != null ? Wallet.Instance.TotalSpent : 0L;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            StartCoroutine(Present(kills, waves, playTime, spent));
        }

        private IEnumerator Present(int kills, int waves, float playTime, long spent)
        {
            float wait = 0f;
            while (wait < _showDelay)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            Root.SetActive(true);
            if (_subtitleText != null) _subtitleText.text = waves > 0 ? $"모든 웨이브 클리어  ·  {waves} / {waves}" : "모든 웨이브 클리어";
            SetText(_killsText, "0");
            SetText(_timeText, FormatTime(0f));
            SetText(_spentText, "0");
            foreach (RectTransform row in _statRows) if (row != null) row.localScale = new Vector3(1f, 0f, 1f);
            SetButtonsInteractable(false);

            // 배경 dim + 창 팝업
            Color dimColor = _dim != null ? _dim.color : Color.clear;
            float t = 0f;
            const float popDuration = 0.45f;
            while (t < popDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / popDuration);
                if (_dim != null) _dim.color = new Color(dimColor.r, dimColor.g, dimColor.b, dimColor.a * p);
                if (_window != null) _window.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(p));
                yield return null;
            }
            if (_window != null) _window.localScale = Vector3.one;

            // 통계 행 순차 등장 + 숫자 카운트 업
            TMP_Text[] texts = { _killsText, _timeText, _spentText };
            for (int i = 0; i < _statRows.Length; i++)
            {
                RectTransform row = _statRows[i];
                if (row == null) continue;
                float rt = 0f;
                const float rowDuration = 0.18f;
                while (rt < rowDuration)
                {
                    rt += Time.unscaledDeltaTime;
                    row.localScale = new Vector3(1f, EaseOutBack(Mathf.Clamp01(rt / rowDuration)), 1f);
                    yield return null;
                }
                row.localScale = Vector3.one;
            }

            float c = 0f;
            while (c < _countDuration)
            {
                c += Time.unscaledDeltaTime;
                float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(c / _countDuration), 3f);
                SetText(texts[0], Mathf.RoundToInt(kills * p).ToString("N0"));
                SetText(texts[1], FormatTime(playTime * p));
                SetText(texts[2], Mathf.RoundToInt(spent * p).ToString("N0"));
                yield return null;
            }
            SetText(_killsText, kills.ToString("N0"));
            SetText(_timeText, FormatTime(playTime));
            SetText(_spentText, spent.ToString("N0"));
            SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (_levelSelectButton != null) _levelSelectButton.interactable = value;
            if (_retryButton != null) _retryButton.interactable = value;
            if (_titleButton != null) _titleButton.interactable = value;
        }

        /// <summary>타이틀 씬으로 돌아가 레벨 선택 화면을 바로 엽니다.</summary>
        public void GoLevelSelect() => SceneFlow.LoadTitle(true, _titleSceneName);

        /// <summary>같은 레벨을 처음부터 다시 시작합니다.</summary>
        public void Retry() => SceneFlow.ReloadActiveScene();

        /// <summary>타이틀 화면으로 돌아갑니다.</summary>
        public void GoTitle() => SceneFlow.LoadTitle(false, _titleSceneName);

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }

        /// <summary>시간을 mm:ss(1시간 이상이면 h:mm:ss) 형식으로 바꿉니다.</summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int h = total / 3600, m = (total % 3600) / 60, s = total % 60;
            return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
