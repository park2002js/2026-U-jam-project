using UnityEngine;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// 스킬 적용 범위를 보여줄 GameObject에 스크립트 컴포넌트로 들어간다.
    /// PlayerSkillManager가 ItemMeta의 형태에 맞는 프리팹을 선택하고 생성한다.
    /// 
    /// <para>플레이어 스킬의 적용 범위를 보여준다. </para>
    /// 
    /// <para>PlayerSkillManager가 ItemMeta의 스케일과 방향을 전달하고, 매 프레임 지면 위치를 갱신한다.
    /// 형태는 매니저가 선택한 프리팹으로 결정하며 X/Z 스케일은 메타 값으로 덮어쓴다.
    /// </para>
    /// </summary>
    public class PlayerSkillPreview : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        [Tooltip("Ground와 Preview가 겹쳐 깜빡이는 현상을 막기 위한 표면 오프셋")]
        private float surfaceOffset = 0.03f;

        private Quaternion heading = Quaternion.identity;

        /// <summary>ItemMeta에서 정한 X/Z 스케일과 월드 XZ 방향을 그대로 적용한다.</summary>
        public void Show(Vector2 scale, Vector3 direction)
        {
            transform.localScale = new Vector3(scale.x, transform.localScale.y, scale.y);
            heading = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = heading;
            gameObject.SetActive(true);
        }

        // 지면 기울기에 맞추고 표면 위로 띄워 깊이 충돌을 막는다.
        public void SetPosition(Vector3 position, Vector3 surfaceNormal)
        {
            surfaceNormal = surfaceNormal.sqrMagnitude > 0.000001f ? surfaceNormal.normalized : Vector3.up;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal) * heading;
            transform.position = position + surfaceNormal * surfaceOffset;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
