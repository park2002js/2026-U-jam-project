using TMPro;
using UJam.Runtime.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 설정창: 배경음(BGM)·효과음(SFX) 볼륨 슬라이더를 제공합니다. 값은 GameAudioSettings에 즉시 반영·저장됩니다.
    /// 게임 중에는 열려 있는 동안 게임을 일시정지합니다(타이틀에서는 _pauseGame을 끕니다). ESC·닫기 버튼으로 닫습니다.
    /// </summary>
    public class UISettingsPanel : MonoBehaviour
    {
        [SerializeField, Tooltip("열고 닫을 설정창 본체입니다.")] private GameObject _panel;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TMP_Text _bgmValueText;
        [SerializeField] private TMP_Text _sfxValueText;
        [SerializeField] private Button _closeButton;
        [SerializeField, Tooltip("열려 있는 동안 Time.timeScale을 0으로 멈춥니다.")] private bool _pauseGame = true;

        private float _previousTimeScale = 1f;
        private bool _paused;

        /// <summary>현재 설정창이 열려 있는지 여부입니다.</summary>
        public bool IsOpen => _panel != null && _panel.activeSelf;

        /// <summary>어느 설정창이든 열려 있으면 true입니다. 입력 처리에서 게임 조작을 막을 때 사용합니다.</summary>
        public static bool AnyOpen { get; private set; }

        private void Awake()
        {
            if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_bgmSlider != null) _bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
            Resume();
            if (IsOpen) AnyOpen = false;
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        /// <summary>설정 버튼(Button.OnClick)에 연결합니다.</summary>
        public void Open()
        {
            if (_panel == null || IsOpen) return;
            if (_bgmSlider != null) _bgmSlider.SetValueWithoutNotify(GameAudioSettings.BgmVolume);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(GameAudioSettings.SfxVolume);
            RefreshLabels();
            _panel.SetActive(true);
            transform.SetAsLastSibling();
            AnyOpen = true;

            if (_pauseGame && !_paused)
            {
                _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                _paused = true;
            }
        }

        public void Close()
        {
            if (_panel == null || !IsOpen) return;
            _panel.SetActive(false);
            AnyOpen = false;
            Resume();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Resume()
        {
            if (!_paused) return;
            _paused = false;
            Time.timeScale = _previousTimeScale;
        }

        private void OnBgmChanged(float value)
        {
            GameAudioSettings.BgmVolume = value;
            RefreshLabels();
        }

        private void OnSfxChanged(float value)
        {
            GameAudioSettings.SfxVolume = value;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (_bgmValueText != null) _bgmValueText.text = $"{Mathf.RoundToInt(GameAudioSettings.BgmVolume * 100f)}%";
            if (_sfxValueText != null) _sfxValueText.text = $"{Mathf.RoundToInt(GameAudioSettings.SfxVolume * 100f)}%";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => AnyOpen = false;
    }
}
