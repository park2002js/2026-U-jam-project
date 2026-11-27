using System;
using TMPro;
using UJam.Runtime.Phase;
using UJam.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 스킬칸이 가득 찬 상태에서 액티브 아이템을 사려고 할 때 "제거할 아이템을 고르십시오"를 띄우고,
    /// 플레이어가 스킬칸 하나를 클릭하면 그 칸의 아이템과 교체하도록 결과를 전달합니다.
    /// 우클릭·ESC·정비 종료 시 취소되며, 취소하면 재화는 쓰지 않습니다.
    /// </summary>
    public class UIItemReplacePrompt : MonoBehaviour
    {
        public static UIItemReplacePrompt Instance { get; private set; }

        [SerializeField, Tooltip("안내 문구 패널입니다.")] private GameObject _promptRoot;
        [SerializeField, Tooltip("새로 구매하려는 아이템 아이콘입니다.")] private Image _newItemIcon;
        [SerializeField, Tooltip("교체 대상으로 고를 수 있는 스킬칸 강조 표시(슬롯 순서대로)입니다.")]
        private Image[] _slotHighlights = Array.Empty<Image>();
        [SerializeField] private PlayerSkillManager _skillManager;
        [SerializeField] private Color _highlightColor = new Color(1f, 0.62f, 0.22f, 1f);

        private Action<int> _onPick;

        /// <summary>교체할 스킬칸을 고르는 중인지 여부입니다.</summary>
        public bool IsSelecting => _onPick != null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (_skillManager == null) _skillManager = FindAnyObjectByType<PlayerSkillManager>();
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable() => Hide();

        /// <summary>교체 선택을 시작합니다. onPick은 플레이어가 고른 스킬칸 번호(0부터)를 받습니다.</summary>
        public void Begin(Sprite newItemIcon, Action<int> onPick)
        {
            _onPick = onPick;
            if (_newItemIcon != null)
            {
                _newItemIcon.sprite = newItemIcon;
                _newItemIcon.enabled = newItemIcon != null;
            }
            if (_promptRoot != null) _promptRoot.SetActive(true);
            for (int i = 0; i < _slotHighlights.Length; i++)
            {
                if (_slotHighlights[i] == null) continue;
                bool hasSkill = _skillManager == null || _skillManager.GetSkill(i) != null;
                _slotHighlights[i].gameObject.SetActive(hasSkill);
            }
        }

        /// <summary>UISkillSlot 클릭에서 호출합니다. 선택 중일 때만 교체 대상을 확정합니다.</summary>
        public bool TryPick(int slot)
        {
            if (!IsSelecting) return false;
            Action<int> pick = _onPick;
            Hide();
            pick?.Invoke(slot);
            return true;
        }

        /// <summary>교체 선택을 취소합니다. 재화는 사용되지 않습니다.</summary>
        public void Cancel() => Hide();

        private void Update()
        {
            if (!IsSelecting) return;

            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
            foreach (Image highlight in _slotHighlights)
            {
                if (highlight != null && highlight.gameObject.activeSelf)
                    highlight.color = new Color(_highlightColor.r, _highlightColor.g, _highlightColor.b, _highlightColor.a * pulse);
            }

            bool leftPreparation = GameManager.Instance != null && GameManager.Instance.CurrentPhase != PhaseState.Preparation;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1) || leftPreparation) Cancel();
        }

        private void Hide()
        {
            _onPick = null;
            if (_promptRoot != null) _promptRoot.SetActive(false);
            foreach (Image highlight in _slotHighlights)
            {
                if (highlight != null) highlight.gameObject.SetActive(false);
            }
        }
    }
}
