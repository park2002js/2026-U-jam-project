using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UJam.Runtime.Systems;

namespace UJam.Runtime.Enemy
{
    /// <summary>적 체력바 위에 속성·디버프 아이콘을 띄운다.</summary>
    public class EnemyStatusIcon : MonoBehaviour
    {
        [Serializable]
        public class IconEntry
        {
            public ElementType type;
            public Sprite sprite;
        }

        [Serializable]
        public class DebuffIconEntry
        {
            public EnemyDebuffType type;
            public Sprite sprite;
        }

        [Header("속성 아이콘")]
        [SerializeField] private IconEntry[] _icons = Array.Empty<IconEntry>();

        [Header("디버프 아이콘")]
        [SerializeField] private DebuffIconEntry[] _debuffIcons = Array.Empty<DebuffIconEntry>();

        [Header("배치")]
        [SerializeField, Min(1f)]
        [Tooltip("아이콘 한 변의 길이. 원본이 32px이면 16이나 32처럼 정수 분할 값을 쓴다.")]
        private float _iconSize = 16f;

        [SerializeField] private float _spacing = 2f;

        [SerializeField]
        [Tooltip("체력바 위로 얼마나 띄울지.")]
        private float _verticalOffset = 12f;

        [Header("슬롯 테두리")]
        [SerializeField]
        [Tooltip("비워두면 테두리 없이 아이콘만 그린다.")]
        private Sprite _slotSprite;

        [SerializeField] private Color _slotColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private float _slotPadding = 3f;

        [Header("넘침 표시")]
        [SerializeField, Min(1)]
        [Tooltip("한 번에 보여줄 최대 개수. 넘으면 +N 으로 표시한다.")]
        private int _maxVisible = 3;

        [SerializeField] private TMP_FontAsset _overflowFont;
        [SerializeField] private float _overflowFontSize = 12f;
        [SerializeField] private Color _overflowColor = Color.white;

        /// <summary>표시 순서. 여기 없는 속성은 아이콘을 띄우지 않는다.</summary>
        private static readonly ElementType[] DisplayOrder =
        {
            ElementType.Burn,
            ElementType.Freeze,
            ElementType.Shock,
            ElementType.Bleed,
        };

        /// <summary>디버프 표시 순서.</summary>
        private static readonly EnemyDebuffType[] DebuffDisplayOrder =
        {
            EnemyDebuffType.AttackDown,
            EnemyDebuffType.DamageTakenUp,
            EnemyDebuffType.SlowDown,
            EnemyDebuffType.Marked,
        };

        private class Slot
        {
            public RectTransform Root;
            public Image Background;
            public Image Icon;
        }

        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<Sprite> _buffer = new List<Sprite>();

        private RectTransform _self;
        private RectTransform _overflowRoot;
        private TextMeshProUGUI _overflowText;
        private int _visibleCount;

        /// <summary>하나라도 떠 있으면 true. 체력바 자동 숨김을 막는 데 쓴다.</summary>
        public bool HasVisible => _visibleCount > 0;

        private float CellSize => _iconSize + _slotPadding * 2f;

        private RectTransform Self
        {
            get
            {
                if (_self == null) _self = (RectTransform)transform;
                return _self;
            }
        }

        /// <summary>현재 걸린 속성·디버프를 읽어 아이콘을 갱신한다. 매 프레임 호출해도 된다.</summary>
        public void Refresh(EnemyBase enemy)
        {
            _buffer.Clear();

            if (enemy != null)
            {
                CollectElements(enemy);
                CollectDebuffs(enemy);
            }

            int total = _buffer.Count;
            int shown = Mathf.Min(total, _maxVisible);

            for (int i = 0; i < shown; i++)
            {
                var slot = GetSlot(i);
                slot.Root.gameObject.SetActive(true);
                slot.Icon.sprite = _buffer[i];
                slot.Icon.enabled = _buffer[i] != null;
            }

            for (int i = shown; i < _slots.Count; i++)
                _slots[i].Root.gameObject.SetActive(false);

            int overflow = total - shown;
            ShowOverflow(overflow);

            _visibleCount = shown;
            Layout(shown, overflow > 0);
        }

        private void CollectElements(EnemyBase enemy)
        {
            var manager = ElementManager.Instance;
            if (manager == null) return;

            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                var type = DisplayOrder[i];
                if (!manager.TryGet(enemy, type, out _)) continue;

                var sprite = FindElementSprite(type);
                if (sprite != null) _buffer.Add(sprite);
            }
        }

        private void CollectDebuffs(EnemyBase enemy)
        {
            var manager = EnemyDebuffManager.Instance;
            if (manager == null) return;

            for (int i = 0; i < DebuffDisplayOrder.Length; i++)
            {
                var type = DebuffDisplayOrder[i];
                if (!manager.TryGet(enemy, type, out _)) continue;

                var sprite = FindDebuffSprite(type);
                if (sprite != null) _buffer.Add(sprite);
            }
        }

        private Sprite FindElementSprite(ElementType type)
        {
            for (int i = 0; i < _icons.Length; i++)
                if (_icons[i] != null && _icons[i].type == type)
                    return _icons[i].sprite;

            return null;
        }

        private Sprite FindDebuffSprite(EnemyDebuffType type)
        {
            for (int i = 0; i < _debuffIcons.Length; i++)
                if (_debuffIcons[i] != null && _debuffIcons[i].type == type)
                    return _debuffIcons[i].sprite;

            return null;
        }

        /// <summary>떠 있는 아이콘을 전부 끈다. 적이 풀로 돌아갈 때 호출한다.</summary>
        public void Clear()
        {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].Root.gameObject.SetActive(false);

            if (_overflowRoot != null)
                _overflowRoot.gameObject.SetActive(false);

            _visibleCount = 0;
        }

        private Slot GetSlot(int index)
        {
            while (_slots.Count <= index)
                _slots.Add(CreateSlot());

            return _slots[index];
        }

        private Slot CreateSlot()
        {
            var root = new GameObject($"Slot_{_slots.Count}", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.SetParent(Self, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image background = null;
            if (_slotSprite != null)
            {
                var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                var bgRt = (RectTransform)bgGo.transform;
                bgRt.SetParent(rt, false);
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;

                background = bgGo.GetComponent<Image>();
                background.sprite = _slotSprite;
                background.color = _slotColor;
                background.raycastTarget = false;
                // 테두리가 지정된 스프라이트만 9-slice로 늘린다.
                background.type = _slotSprite.border == Vector4.zero
                    ? Image.Type.Simple
                    : Image.Type.Sliced;
            }

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.SetParent(rt, false);
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;

            var icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            return new Slot { Root = rt, Background = background, Icon = icon };
        }

        private void ShowOverflow(int count)
        {
            if (count <= 0)
            {
                if (_overflowRoot != null) _overflowRoot.gameObject.SetActive(false);
                return;
            }

            if (_overflowRoot == null) CreateOverflow();

            _overflowRoot.gameObject.SetActive(true);
            _overflowText.text = $"+{count}";
        }

        private void CreateOverflow()
        {
            var go = new GameObject("Overflow", typeof(RectTransform), typeof(TextMeshProUGUI));
            _overflowRoot = (RectTransform)go.transform;
            _overflowRoot.SetParent(Self, false);
            _overflowRoot.anchorMin = _overflowRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _overflowRoot.pivot = new Vector2(0.5f, 0.5f);

            _overflowText = go.GetComponent<TextMeshProUGUI>();
            if (_overflowFont != null) _overflowText.font = _overflowFont;
            _overflowText.fontSize = _overflowFontSize;
            _overflowText.color = _overflowColor;
            _overflowText.alignment = TextAlignmentOptions.Center;
            _overflowText.raycastTarget = false;
            _overflowText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>보이는 슬롯과 +N 을 가로로 가운데 정렬한다.</summary>
        private void Layout(int shown, bool hasOverflow)
        {
            int cells = shown + (hasOverflow ? 1 : 0);
            if (cells == 0) return;

            float cell = CellSize;
            float totalWidth = cells * cell + (cells - 1) * _spacing;
            float startX = -totalWidth * 0.5f + cell * 0.5f;

            for (int i = 0; i < shown; i++)
            {
                var slot = _slots[i];
                slot.Root.sizeDelta = new Vector2(cell, cell);
                slot.Root.anchoredPosition = new Vector2(startX + i * (cell + _spacing), _verticalOffset);

                var iconRt = (RectTransform)slot.Icon.transform;
                iconRt.sizeDelta = new Vector2(_iconSize, _iconSize);
            }

            if (hasOverflow)
            {
                _overflowRoot.sizeDelta = new Vector2(cell, cell);
                _overflowRoot.anchoredPosition =
                    new Vector2(startX + shown * (cell + _spacing), _verticalOffset);
            }
        }
    }
}