using System;
using System.Collections.Generic;
using TMPro;
using UJam.Runtime.Enemy;
using UJam.Runtime.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 체력바 위에 적의 현재 속성 아이콘을 표시한다.
    /// 아이콘은 미리 만들어두고 활성 속성에 맞춰 켜고 끈다.
    /// 최대 표시 수를 넘으면 남은 개수를 +N 텍스트로 알린다.
    /// </summary>
    public class EnemyStatusIcons : MonoBehaviour
    {
        [Serializable]
        private class IconEntry
        {
            public ElementType type;
            public Sprite sprite;
        }

        [Header("아이콘 설정")]
        [SerializeField, Tooltip("속성별 아이콘 스프라이트")]
        private IconEntry[] _icons;

        [SerializeField, Tooltip("아이콘이 생성될 부모. 비우면 이 오브젝트를 사용한다.")]
        private RectTransform _iconParent;

        [SerializeField, Tooltip("아이콘 한 변의 크기(픽셀)")]
        private float _iconSize = 14f;

        [SerializeField, Tooltip("아이콘 사이 간격(픽셀)")]
        private float _spacing = 2f;

        [SerializeField, Tooltip("체력바 위로 띄울 높이(픽셀)")]
        private float _verticalOffset = 12f;

        [Header("초과 표시")]
        [SerializeField, Min(1), Tooltip("한 번에 표시할 최대 아이콘 수. 초과분은 +N으로 표시한다.")]
        private int _maxVisible = 3;

        [SerializeField, Tooltip("초과 표시 텍스트에 사용할 폰트. 비우면 기본 폰트를 사용한다.")]
        private TMP_FontAsset _overflowFont;

        [SerializeField, Tooltip("초과 표시 텍스트 크기")]
        private float _overflowFontSize = 12f;

        [SerializeField] private Color _overflowColor = Color.white;

        // 표시 순서를 고정하기 위한 목록. Wind는 효과가 없어 표시하지 않는다.
        private static readonly ElementType[] DisplayOrder =
        {
            ElementType.Burn, ElementType.Freeze, ElementType.Shock, ElementType.Bleed
        };

        private readonly Dictionary<ElementType, Image> _slots = new();
        private readonly List<RectTransform> _visible = new();
        private TMP_Text _overflowText;
        private bool _built;

        private void Awake() => Build();

        // 속성마다 Image를 하나씩 만들어두고 꺼둔다
        private void Build()
        {
            if (_built) return;
            _built = true;

            RectTransform parent = _iconParent != null ? _iconParent : (RectTransform)transform;

            foreach (ElementType type in DisplayOrder)
            {
                Sprite sprite = FindSprite(type);
                if (sprite == null) continue;

                _slots.Add(type, CreateIcon(parent, $"Icon_{type}", sprite));
            }

            BuildOverflowText(parent);
        }

        private Image CreateIcon(RectTransform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(_iconSize, _iconSize);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;   // 클릭을 가로채지 않게 한다

            go.SetActive(false);
            return image;
        }

        // 초과 개수를 알리는 +N 텍스트
        private void BuildOverflowText(RectTransform parent)
        {
            var go = new GameObject("Icon_Overflow", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(_iconSize, _iconSize);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (_overflowFont != null) text.font = _overflowFont;
            text.fontSize = _overflowFontSize;
            text.color = _overflowColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            go.SetActive(false);
            _overflowText = text;
        }

        private Sprite FindSprite(ElementType type)
        {
            if (_icons == null) return null;
            foreach (IconEntry entry in _icons)
            {
                if (entry != null && entry.type == type) return entry.sprite;
            }
            return null;
        }

        /// <summary>
        /// 해당 적의 활성 속성에 맞춰 아이콘을 갱신한다. 매니저가 매 프레임 호출한다.
        /// </summary>
        public void Refresh(EnemyBase enemy)
        {
            if (!_built) Build();

            _visible.Clear();
            int activeCount = 0;

            foreach (ElementType type in DisplayOrder)
            {
                if (!_slots.TryGetValue(type, out Image icon)) continue;

                bool active = enemy != null && ElementManager.Instance.TryGet(enemy, type, out _);
                if (active) activeCount++;

                // 최대 표시 수를 넘으면 숨기고 +N으로 대신한다
                bool show = active && _visible.Count < _maxVisible;
                if (icon.gameObject.activeSelf != show) icon.gameObject.SetActive(show);
                if (show) _visible.Add((RectTransform)icon.transform);
            }

            // 넘치는 경우에만 +N 텍스트를 켠다
            int hidden = activeCount - _maxVisible;
            bool overflow = hidden > 0;
            if (_overflowText != null)
            {
                if (_overflowText.gameObject.activeSelf != overflow) _overflowText.gameObject.SetActive(overflow);
                if (overflow)
                {
                    _overflowText.text = $"+{hidden}";
                    _visible.Add((RectTransform)_overflowText.transform);
                }
            }

            Layout();
        }

        /// <summary>속성이 모두 사라졌을 때 아이콘을 전부 끈다.</summary>
        public void Clear()
        {
            if (!_built) return;

            foreach (KeyValuePair<ElementType, Image> pair in _slots)
            {
                if (pair.Value.gameObject.activeSelf) pair.Value.gameObject.SetActive(false);
            }

            if (_overflowText != null && _overflowText.gameObject.activeSelf)
                _overflowText.gameObject.SetActive(false);

            _visible.Clear();
        }

        /// <summary>표시 중인 속성이 하나라도 있는지 확인한다.</summary>
        public bool HasVisible => _visible.Count > 0;

        // 켜져 있는 아이콘만 가로로 가운데 정렬한다
        private void Layout()
        {
            if (_visible.Count == 0) return;

            float step = _iconSize + _spacing;
            float totalWidth = step * _visible.Count - _spacing;
            float startX = -totalWidth * 0.5f + _iconSize * 0.5f;

            for (int i = 0; i < _visible.Count; i++)
            {
                _visible[i].anchoredPosition = new Vector2(startX + step * i, _verticalOffset);
            }
        }
    }
}