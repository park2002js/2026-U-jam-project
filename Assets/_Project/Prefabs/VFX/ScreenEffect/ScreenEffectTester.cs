using System;
using UnityEngine;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 화면 효과 확인용 임시 코드. 메인 로직과 무관하게 키 입력으로 발동한다.
    /// 효과마다 테두리, 파티클 아이콘, 중앙 아이콘 설정을 따로 가진다.
    /// </summary>
    public class ScreenEffectTester : MonoBehaviour
    {
        /// <summary>효과 하나의 전체 설정.</summary>
        [Serializable]
        private class EffectSetting
        {
            [Header("발동 키")]
            public KeyCode key = KeyCode.Alpha5;

            [Header("테두리")]
            public Color color = Color.white;
            [Range(0f, 1f), Tooltip("클수록 화면 안쪽까지 들어온다")]
            public float alpha = 0.3f;
            [Tooltip("나타남 / 유지 / 사라짐 시간(초)")]
            public float fadeIn = 0.2f;
            public float hold = 1f;
            public float fadeOut = 0.5f;

            [Header("좌우 파티클 아이콘")]
            [Tooltip("비우면 파티클을 띄우지 않는다")]
            public Sprite sparkleIcon;
            [Range(0, 10), Tooltip("좌우 각각의 개수")]
            public int sparkleCount = 4;
            [Tooltip("크기 범위(픽셀)")]
            public Vector2 sparkleSize = new Vector2(24f, 48f);
            [Range(0f, 45f), Tooltip("기울어지는 최대 각도. 0이면 똑바로 세운다")]
            public float sparkleTilt = 0f;
            [Tooltip("켜면 테두리 색으로 물들이고, 끄면 아이콘 원본 색을 쓴다")]
            public bool tintSparkle = false;

            [Header("중앙 아이콘")]
            [Tooltip("비우면 중앙 아이콘을 띄우지 않는다")]
            public Sprite centerIcon;
            public Vector2 centerIconSize = new Vector2(240f, 240f);
            [Tooltip("켜면 테두리 색으로 물들이고, 끄면 아이콘 원본 색을 쓴다")]
            public bool tintCenterIcon = false;

            /// <summary>설정대로 화면 효과를 발동한다.</summary>
            public void Play(ScreenEffectManager manager)
            {
                manager.Flash(color, alpha, fadeIn, hold, fadeOut);

                if (sparkleIcon != null)
                {
                    manager.SpawnSparkles(sparkleIcon, tintSparkle ? color : Color.white,
                        sparkleCount, sparkleSize, sparkleTilt);
                }

                if (centerIcon != null)
                {
                    manager.ShowCenterIcon(centerIcon, tintCenterIcon ? color : Color.white, centerIconSize);
                }
            }
        }

        [Header("아드레날린 (13)")]
        [SerializeField] private EffectSetting _adrenaline = new EffectSetting
        {
            key = KeyCode.Alpha5,
            color = new Color(0.78f, 0.14f, 1f),   // 보라 (분노마법)
            alpha = 0.3f, fadeIn = 0.3f, hold = 4.2f, fadeOut = 0.5f,
            sparkleTilt = 0f
        };

        [Header("긴급 수리 (14)")]
        [SerializeField] private EffectSetting _repair = new EffectSetting
        {
            key = KeyCode.Alpha6,
            color = new Color(0.3f, 1f, 0.5f),     // 초록 (회복)
            alpha = 0.35f, fadeIn = 0.1f, hold = 0.15f, fadeOut = 0.5f,
            sparkleTilt = 15f, tintSparkle = true
        };

        [Header("집념 (17)")]
        [SerializeField] private EffectSetting _tenacity = new EffectSetting
        {
            key = KeyCode.Alpha7,
            color = new Color(1f, 0.85f, 0.2f),    // 노랑 (공속)
            alpha = 0.28f, fadeIn = 0.15f, hold = 1.5f, fadeOut = 0.35f,
            sparkleTilt = 0f
        };

        [Header("리저렉션 (25)")]
        [SerializeField] private EffectSetting _resurrect = new EffectSetting
        {
            key = KeyCode.Alpha8,
            color = new Color(1f, 0.85f, 0.4f),    // 금빛 (부활)
            alpha = 0.4f, fadeIn = 0.08f, hold = 0.3f, fadeOut = 1.2f,
            sparkleCount = 0                        // 파티클 없음
        };

        [Header("광전사의 분노 (27)")]
        [SerializeField] private KeyCode _rageUpKey = KeyCode.Alpha9;
        [SerializeField] private KeyCode _clearKey = KeyCode.Minus;

        private int _rageStep;

        private void Update()
        {
            ScreenEffectManager manager = ScreenEffectManager.Instance;
            if (manager == null) return;

            if (Input.GetKeyDown(_adrenaline.key)) _adrenaline.Play(manager);
            if (Input.GetKeyDown(_repair.key)) _repair.Play(manager);
            if (Input.GetKeyDown(_tenacity.key)) _tenacity.Play(manager);
            if (Input.GetKeyDown(_resurrect.key)) _resurrect.Play(manager);

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