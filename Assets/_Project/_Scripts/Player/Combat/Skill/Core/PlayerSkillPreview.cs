using UnityEngine;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// 스킬 적용 범위를 보여줄 GameObject에 스크립트 컴포넌트로 들어간다.
    /// PlayerCombatManager의 Inspector에 할당된 뒤, PlayerSkillManager로 전달된다.
    /// 
    /// <para>플레이어 스킬의 적용 범위를 보여준다. </para>
    /// 
    /// <para> 기본적으로 원형이며, Skill 내부에 정의된 효과 범위에 따라 그에 맞는 범위를 보여준다.
    /// 그 반경 값은 일반 스킬이 발동될 때 PlayerSkillManager가 전달한다.
    /// 그 이후 매 프레임마다 PlayerSkillManager에서 마우스 커서의 위치를 추적하여 그 위치를 전달하면, 그 위치로 중심을 재설정하여 Preview를 보여준다.
    /// </para>
    /// 
    /// <para> Prefab의 원본 메시가 지름 1 기준이 아니라면 Scale Multiplier로 보정한다.
    /// Show에서 X/Z Scale을 덮어쓰므로 Prefab의 Scale 값을 직접 수정해도 반영되지 않는다.
    /// </para>
    /// </summary>
    public class PlayerSkillPreview : MonoBehaviour
    {
        [SerializeField, Min(0.01f)]
        [Tooltip("Ground와 Preview가 겹쳐 깜빡이는 현상을 막기 위한 표면 오프셋")]
        private float surfaceOffset = 0.02f;

        [SerializeField, Min(0.0001f)]
        [Tooltip("Prefab 원본 크기 보정 배율. 메시가 지름 1보다 크면 그만큼 줄여서 지정한다.")]
        private float scaleMultiplier = 1f;

        /// <summary>조준을 시작할 때 호출한다. 반경(월드 거리)에 맞춰 지름으로 환산한다.</summary>
        public void Show(float radius)
        {
            gameObject.SetActive(true);

            // 반경을 지름으로 바꾸고 Prefab 원본 크기를 보정한다
            float size = radius * 2f * scaleMultiplier;
            transform.localScale = new Vector3(size, transform.localScale.y, size);
        }

        // Preview를 약간 위로 띄워서 처리함
        public void SetPosition(Vector3 position, Vector3 surfaceNormal)
        {
            transform.position = position + surfaceNormal * surfaceOffset;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}