using UnityEngine;

namespace UJam.Runtime.Navigation
{
    // Enemy가 구체 이동 구현에 의존하지 않고 목적지 이동을 요청할 경계
    public interface EnemyNavigationPort
    {
        // Target Object에서 해석한 World 목적지로 이동을 요청
        bool RequestMove(Object target, out Vector3 destination);

        // 보관한 이동 요청을 중단
        void StopMovement();
    }
}
