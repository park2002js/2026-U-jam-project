using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    // 체력바 하나의 표시를 담당. 생성과 위치 갱신은 매니저가 한다.
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Image _fill;          // Image Type을 Filled로 설정
        [SerializeField] private CanvasGroup _group;   // 보이기/숨기기용
        [SerializeField] private EnemyStatusIcons _icons;   // 체력바 위 속성 아이콘

        public RectTransform Rect => (RectTransform)transform;
        public float HideTimer { get; set; }           // 매니저가 관리하는 자동 숨김 타이머\
        public EnemyStatusIcons Icons => _icons;

        // 체력 비율 반영
        public void SetRatio(float ratio)
        {
            if (_fill != null) _fill.fillAmount = Mathf.Clamp01(ratio);
        }

        // 화면 밖이거나 카메라 뒤일 때 숨김
        public void SetVisible(bool visible)
        {
            if (_group != null) _group.alpha = visible ? 1f : 0f;
        }
    }
}
