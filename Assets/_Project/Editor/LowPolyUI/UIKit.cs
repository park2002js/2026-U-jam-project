using TMPro;
using UJam.Runtime.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UJam.EditorTools.LowPolyUI
{
    /// <summary>
    /// 로우폴리 UI 키트로 uGUI 오브젝트를 만드는 헬퍼입니다. 좌표는 1920x1080 기준 픽셀입니다.
    /// </summary>
    internal static class UIKit
    {
        public static readonly Color TextMain = LowPolyCanvas.Hex("#F6EBD9");
        public static readonly Color TextSub = LowPolyCanvas.Hex("#B9AFC9");
        public static readonly Color TextDark = LowPolyCanvas.Hex("#2B1C0E");
        public static readonly Color Gold = LowPolyCanvas.Hex("#FFC94D");
        public static readonly Color Orange = LowPolyCanvas.Hex("#F29A3A");
        public static readonly Color Slate = LowPolyCanvas.Hex("#6E6788");
        public static readonly Color ButtonGold = LowPolyCanvas.Hex("#F5B847");
        public static readonly Color ButtonOrange = LowPolyCanvas.Hex("#F08A36");
        public static readonly Color ButtonGreen = LowPolyCanvas.Hex("#86C95A");
        public static readonly Color Dim = new Color(0.04f, 0.03f, 0.07f, 0.72f);

        public static Sprite Panel => LowPolyUIArt.Load("LP_Panel");
        public static Sprite Header => LowPolyUIArt.Load("LP_Header");
        public static Sprite Slot => LowPolyUIArt.Load("LP_Slot");
        public static Sprite ButtonSprite => LowPolyUIArt.Load("LP_Button");

        public static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>anchor/pivot을 같은 점으로 두고 위치와 크기를 지정합니다.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>부모를 가득 채우고 상하좌우 여백을 지정합니다.</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static readonly Vector2 TopLeft = new Vector2(0, 1);
        public static readonly Vector2 TopRight = new Vector2(1, 1);
        public static readonly Vector2 TopCenter = new Vector2(0.5f, 1);
        public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0);
        public static readonly Vector2 BottomRight = new Vector2(1, 0);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 Left = new Vector2(0, 0.5f);
        public static readonly Vector2 Right = new Vector2(1, 0.5f);

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false, float pixelsPerUnitMultiplier = 1f)
        {
            RectTransform rt = Node(name, parent);
            Image image = rt.gameObject.AddComponent<Image>();
            ApplyImage(image, sprite, color, raycast, pixelsPerUnitMultiplier);
            return image;
        }

        /// <summary>테두리가 있는 스프라이트는 Sliced, 아이콘은 비율 유지 Simple로 설정합니다.</summary>
        public static void ApplyImage(Image image, Sprite sprite, Color color, bool raycast = false, float pixelsPerUnitMultiplier = 1f)
        {
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            bool sliced = sprite != null && sprite.border.sqrMagnitude > 0f;
            image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.fillCenter = true;
            image.preserveAspect = !sliced && sprite != null && sprite.name != "LP_White";
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool shadow = true)
        {
            RectTransform rt = Node(name, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            ApplyText(tmp, text, size, color, align, shadow);
            return tmp;
        }

        public static void ApplyText(TMP_Text tmp, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool shadow = true)
        {
            tmp.font = LowPolyUIArt.Font;
            if (shadow && LowPolyUIArt.FontShadow != null) tmp.fontSharedMaterial = LowPolyUIArt.FontShadow;
            else if (LowPolyUIArt.Font != null) tmp.fontSharedMaterial = LowPolyUIArt.Font.material;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.enableAutoSizing = false;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            tmp.fontStyle = FontStyles.Normal;
            tmp.characterSpacing = 0f;
            tmp.rectTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// 로우폴리 버튼(회색조 스프라이트 + 색 입힘)을 만듭니다. 마우스를 올리면 살짝 커집니다.
        /// </summary>
        public static Button Button(Transform parent, string name, Color tint, string label, float fontSize, Color labelColor, out TextMeshProUGUI text, bool shadow = false)
        {
            Image image = Image(parent, name, ButtonSprite, tint, true);
            Button button = image.gameObject.AddComponent<Button>();
            StyleButton(button, image, tint);
            image.gameObject.AddComponent<UITitleBtnFX>();
            text = Text(image.transform, "Label", label, fontSize, labelColor, TextAlignmentOptions.Center, shadow);
            Stretch(text.rectTransform, 12, 4, 12, 8);
            return button;
        }

        /// <summary>버튼 색 상태(평상/올림/누름/비활성)를 로우폴리 키트 규칙으로 설정합니다.</summary>
        public static void StyleButton(Button button, Image image, Color tint)
        {
            ApplyImage(image, ButtonSprite, tint, true);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.45f, 0.43f, 0.5f, 0.85f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
        }

        /// <summary>
        /// 로우폴리 볼륨 슬라이더: 오목한 홈(공용 슬롯 스프라이트) + 주황 채움 + 금색 손잡이. 값 범위는 0~1입니다.
        /// </summary>
        public static Slider Slider(Transform parent, string name, Vector2 size)
        {
            RectTransform root = Node(name, parent);
            root.sizeDelta = size;
            Slider slider = root.gameObject.AddComponent<Slider>();

            Image track = Image(root, "Background", Slot, Color.white, true, 2.6f);
            Stretch(track.rectTransform, 0, 8, 0, 8);

            RectTransform fillArea = Node("Fill Area", root);
            Stretch(fillArea, 10, 15, 10, 15);
            Image fill = Image(fillArea, "Fill", LowPolyUIArt.Load("LP_White"), Orange);
            Stretch(fill.rectTransform);

            RectTransform handleArea = Node("Handle Slide Area", root);
            Stretch(handleArea, 14, 0, 14, 0);
            Image handle = Image(handleArea, "Handle", ButtonSprite, ButtonGold, true, 2.6f);
            handle.rectTransform.anchorMin = new Vector2(0, 0);
            handle.rectTransform.anchorMax = new Vector2(0, 1);
            handle.rectTransform.sizeDelta = new Vector2(28, 0);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 0.8f;
            ColorBlock colors = slider.colors;
            colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            slider.colors = colors;
            Navigation nav = slider.navigation;
            nav.mode = Navigation.Mode.None;
            slider.navigation = nav;
            return slider;
        }

        /// <summary>아이콘 + 라벨이 들어간 패널 제목판을 만듭니다.</summary>
        public static RectTransform HeaderPlate(Transform parent, string title, Sprite icon, float width, float y, float fontSize = 32f)
        {
            Image plate = Image(parent, "Header", Header, Color.white);
            Place(plate.rectTransform, TopCenter, TopCenter, new Vector2(0, y), new Vector2(width, 68));
            TextMeshProUGUI label = Text(plate.transform, "Title", title, fontSize, TextMain);
            float textWidth = Mathf.Max(40f, label.GetPreferredValues(title).x);
            const float iconSize = 46f, gap = 10f;
            float groupWidth = icon != null ? iconSize + gap + textWidth : textWidth;
            float left = -groupWidth * 0.5f;
            if (icon != null)
            {
                Image iconImage = Image(plate.transform, "Icon", icon, Color.white);
                Place(iconImage.rectTransform, Center, Center, new Vector2(left + iconSize * 0.5f, 1), new Vector2(iconSize, iconSize));
                left += iconSize + gap;
            }
            Place(label.rectTransform, Center, Center, new Vector2(left + textWidth * 0.5f, -1), new Vector2(textWidth + 8f, 60));
            return plate.rectTransform;
        }

        public static void AddClick(Button button, UnityAction action)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        public static void SetRef(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[UIKit] {target.GetType().Name}.{field} 필드를 찾을 수 없습니다.", target);
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetValue(Object target, string field, System.Action<SerializedProperty> setter)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[UIKit] {target.GetType().Name}.{field} 필드를 찾을 수 없습니다.", target);
                return;
            }
            setter(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        public static T Ensure<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        public static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
