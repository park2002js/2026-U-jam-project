using UnityEngine;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 화면 효과 확인용 임시 코드. 메인 로직과 무관하게 키 입력으로 발동한다.
    /// 각 효과의 깊이(peakAlpha)는 광전사의 분노 2~3단계 수준으로 맞춰 테두리에만 머물게 한다.
    /// </summary>
    public class ScreenEffectTester : MonoBehaviour
    {
        [Header("테스트 키")]
        [SerializeField] private KeyCode _adrenalineKey = KeyCode.Alpha5;   // 아드레날린
        [SerializeField] private KeyCode _repairKey = KeyCode.Alpha6;       // 긴급 수리
        [SerializeField] private KeyCode _tenacityKey = KeyCode.Alpha7;     // 집념
        [SerializeField] private KeyCode _resurrectKey = KeyCode.Alpha8;    // 리저렉션
        [SerializeField] private KeyCode _rageUpKey = KeyCode.Alpha9;       // 분노 단계 ↑
        [SerializeField] private KeyCode _clearKey = KeyCode.Minus;         // 전체 해제

        [Header("색 설정")]
        [SerializeField] private Color _adrenalineColor = new Color(0.78f, 0.14f, 1f);    // 보라 (분노마법)
        [SerializeField] private Color _repairColor = new Color(0.3f, 1f, 0.5f);          // 초록 (회복)
        [SerializeField] private Color _tenacityColor = new Color(1f, 0.85f, 0.2f);       // 노랑 (공속)
        [SerializeField] private Color _resurrectColor = new Color(1f, 0.85f, 0.4f);      // 금빛 (부활)

        [Header("깊이 (0~1, 값이 클수록 화면 안쪽까지 들어온다)")]
        [SerializeField, Range(0f, 1f)] private float _adrenalineAlpha = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _repairAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _tenacityAlpha = 0.28f;
        [SerializeField, Range(0f, 1f)] private float _resurrectAlpha = 0.4f;

        [Header("반짝임")]
        [SerializeField, Range(0, 10)]
        [Tooltip("긴급 수리 발동 시 좌우 각각 생성할 반짝임 개수")]
        private int _repairSparkleCount = 4;

        private int _rageStep;

        private void Update()
        {
            ScreenEffectManager manager = ScreenEffectManager.Instance;
            if (manager == null) return;

            // 아드레날린: 5초 버프라 은은하게 오래 유지
            if (Input.GetKeyDown(_adrenalineKey))
                manager.Flash(_adrenalineColor, _adrenalineAlpha, 0.3f, 4.2f, 0.5f);

            // 긴급 수리: 회복 순간만 짧게, 좌우에 반짝임 동반
            if (Input.GetKeyDown(_repairKey))
                manager.Flash(_repairColor, _repairAlpha, 0.1f, 0.15f, 0.5f, _repairSparkleCount);

            // 집념: 2초 공속 버프
            if (Input.GetKeyDown(_tenacityKey))
                manager.Flash(_tenacityColor, _tenacityAlpha, 0.15f, 1.5f, 0.35f);

            // 리저렉션: 가장 강하게 번쩍인 뒤 천천히 사라짐
            if (Input.GetKeyDown(_resurrectKey))
            {
                manager.Flash(_resurrectColor, _resurrectAlpha, 0.08f, 0.3f, 1.2f);
                manager.ShowWings();
            }
            // 분노: 누를 때마다 단계 상승, 최대를 넘기면 0으로 돌아감
            if (Input.GetKeyDown(_rageUpKey))
            {
                _rageStep = (_rageStep + 1) % 5;
                manager.SetRageStep(_rageStep);
                Debug.Log($"[ScreenEffect] 광전사의 분노 {_rageStep}단계", this);
            }

            if (Input.GetKeyDown(_clearKey))
            {
                _rageStep = 0;
                manager.ClearAll();
            }
        }
    }
}