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
        [SerializeField, Min(0f)]
        [Tooltip("Ground와 Preview가 겹쳐 깜빡이는 현상을 막기 위한 표면 오프셋")]
        private float surfaceOffset = 0.03f;

        [SerializeField, Min(0.0001f)]
        [Tooltip("Prefab 원본 크기 보정 배율. 메시가 지름 1보다 크면 그만큼 줄여서 지정한다.")]
        private float scaleMultiplier = 1f;

        [SerializeField]
        [Tooltip("조준 중 Y축 회전 속도(도/초). 0이면 회전하지 않고, 음수면 반대로 돈다.")]
        private float rotateSpeed = 30f;

        [SerializeField]
        [Tooltip("조준을 시작할 때 회전 각도를 0으로 되돌린다.")]
        private bool resetRotationOnShow = true;

        /// <summary>조준을 시작할 때 호출한다. 반경(월드 거리)에 맞춰 지름으로 환산한다.</summary>
        public void Show(float radius)
        {
            gameObject.SetActive(true);

            // 반경을 지름으로 바꾸고 Prefab 원본 크기를 보정한다
            float size = radius * 2f * scaleMultiplier;
            transform.localScale = new Vector3(size, transform.localScale.y, size);

            if (resetRotationOnShow) transform.rotation = Quaternion.identity;
        }

        /// <summary>단위 Quad의 X/Z 크기를 월드 거리 기준 폭과 길이로 지정한다.</summary>
        public void ShowRectangle(float width, float length)
        {
            gameObject.SetActive(true);
            transform.localScale = new Vector3(width, transform.localScale.y, length);
        }

        // 지면 기울기에 맞추고 표면 위로 띄워 깊이 충돌을 막는다.
        public void SetPosition(Vector3 position, Vector3 surfaceNormal)
        {
            surfaceNormal = surfaceNormal.sqrMagnitude > 0.000001f ? surfaceNormal.normalized : Vector3.up;
            transform.up = surfaceNormal;
            transform.position = position + surfaceNormal * surfaceOffset;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // 조준 중에만 활성화되므로 여기서 회전시키면 표시되는 동안만 돈다
        private void Update()
        {
            if (rotateSpeed == 0f) return;

            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }
    }
}
