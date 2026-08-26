using UnityEngine;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// 스킬 적용 범위를 보여줄 GameObject에 스크립트 컴포넌트로 들어간다.
    /// PlayerCombatManager의 Inspector에 할당된 뒤, PlayerSkillManager로 전달된다.
    /// 스킬 적용 범위에 해당되는 GameObject의 Local Scale은 무조건 (1, 1, 1) 인 상태로 그 형태가 정의되어 있어야 한다.
    /// 
    /// <para>플레이어 스킬의 적용 범위를 보여준다. </para>
    /// 
    /// <para> 기본적으로 원형이며, Skill 내부에 정의된 효과 범위를 나타내는 "EffectRadius" 값에 따라서 그에 따른 범위를 보여준다.
    /// 저 EffectRadius값은 일반 스킬이 발동될 때 PlayerSkillManager가 전달한다.
    /// 그 이후 매 프레임마다 PlayerSkillManager에서 마우스 커서의 위치를 추적하여 그 위치를 전달하면, 그 위치로 중심을 재설정하여 Preview를 보여준다.
    /// </para>
    /// </summary>

    public class PlayerSkillPreview : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        [Tooltip("Ground와 Preview가 겹쳐 깜빡이는 현상을 막기 위한 표면 오프셋")]
        private float surfaceOffset = 0.02f;

        public void Show(float radius)
        {
            gameObject.SetActive(true);
            transform.localScale = new Vector3(radius * 2f, transform.localScale.y,radius * 2f);
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
