using UnityEngine;

namespace UJam.Runtime.Navigation
{
    // 이후 경로 탐색 구현이 Target Object와 목적지 좌표를 받는 최소 Component
    public sealed class NavigationDriver : MonoBehaviour, EnemyNavigationPort
    {
        // 이후 이동 구현이 소비할 마지막 World 목적지
        private Vector3 _destination;

        // 유효한 이동 요청 보관 여부
        private bool _hasDestination;

        // 이후 Navigation 구현이 처리할 목적지 요청 존재 여부
        public bool HasMoveRequest
        {
            get
            {
                // 현재 목적지 요청 보관 여부 반환
                return _hasDestination;
            }
        }

        // 이후 Navigation 구현이 처리할 마지막 World 목적지
        public Vector3 Destination
        {
            get
            {
                // 현재 보관한 World 목적지 반환
                return _destination;
            }
        }

        // Target Object에서 World 목적지를 해석하고 이후 이동 구현에 보관
        public bool RequestMove(Object target, out Vector3 destination)
        {
            // 실패 시 호출자에게 전달할 기본 목적지
            destination = Vector3.zero;

            // World 좌표를 제공하지 않는 Target은 이동 요청으로 사용하지 않음
            if (!TryGetWorldPosition(target, out destination))
            {
                // 이동 요청 실패 반환
                return false;
            }

            // 이후 경로 탐색 구현이 사용할 목적지 저장
            _destination = destination;

            // 유효한 목적지 요청 상태 저장
            _hasDestination = true;

            // 이동 요청과 목적지 해석 성공 반환
            return true;
        }

        // Enemy 사망 또는 상태 전환 시 현재 이동 요청을 제거
        public void StopMovement()
        {
            // 이후 이동 구현이 처리하지 않도록 목적지 요청 제거
            _hasDestination = false;
        }

        // Unity Object에서 현재 World 위치를 읽을 수 있는지 확인
        private static bool TryGetWorldPosition(Object target, out Vector3 position)
        {
            // 실패 시 사용할 기본 World 위치
            position = Vector3.zero;

            // 제거됐거나 비어 있는 Target을 좌표로 바꾸지 않음
            if (target == null)
            {
                // Target 좌표 해석 실패 반환
                return false;
            }

            // Transform Target의 현재 위치 사용
            Transform targetTransform = target as Transform;
            if (targetTransform != null)
            {
                position = targetTransform.position;

                // Transform 좌표의 유효 여부 반환
                return IsFinite(position);
            }

            // GameObject Target의 Transform 위치 사용
            GameObject targetObject = target as GameObject;
            if (targetObject != null)
            {
                position = targetObject.transform.position;

                // GameObject 좌표의 유효 여부 반환
                return IsFinite(position);
            }

            // Component Target이 붙은 GameObject의 Transform 위치 사용
            Component targetComponent = target as Component;
            if (targetComponent != null)
            {
                position = targetComponent.transform.position;

                // Component 좌표의 유효 여부 반환
                return IsFinite(position);
            }

            // 위치를 제공하지 않는 Unity Object는 현재 Navigation 범위에서 거부
            return false;
        }

        // World 목적지의 모든 좌표가 유한한지 확인
        private static bool IsFinite(Vector3 value)
        {
            // 모든 좌표의 유한 여부 반환
            return !float.IsNaN(value.x)
                && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y)
                && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z)
                && !float.IsInfinity(value.z);
        }
    }
}
