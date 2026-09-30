using TMPro;
using UnityEngine;

namespace UJam.Runtime.UI
{
    // 데미지 숫자 하나의 표시. 생성과 이동은 매니저가 관리한다.
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _group;

        public RectTransform Rect { get; private set; }

        // 생성 시점의 월드 좌표. 적을 따라가지 않고 이 자리에 남는다.
        public Vector3 WorldPos { get; set; }
        public float Elapsed { get; set; }

        private void Awake()
        {
            Rect = (RectTransform)transform;
        }

        public void SetValue(float damage, Color color)
        {
            if (_text == null) return;
            _text.text = Mathf.RoundToInt(damage).ToString();
            _text.color = color;
        }

        public void SetAlpha(float alpha)
        {
            if (_group != null) _group.alpha = alpha;
        }
    }
}