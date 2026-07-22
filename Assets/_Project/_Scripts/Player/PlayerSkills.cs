using UnityEngine;

namespace UJam.Runtime.Player
{
    public sealed class PlayerSkills : MonoBehaviour
    {
        // 요청한 슬롯의 스킬 사용 경계
        public bool TryUse(int slot)
        {
            // 잘못된 슬롯 번호 차단
            if (slot < 0)
            {
                // 스킬 사용 실패 반환
                return false;
            }

            // 구체 스킬 계약 확정 전 사용 실패 반환
            return false;
        }
    }
}
