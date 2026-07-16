using UnityEngine;
using UJam.Runtime.Combat;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Defense
{
    public sealed class Barricade : MonoBehaviour
    {
        // 미래 Barricade Prefab에서 연결할 Health
        [SerializeField] private Health _health;

        // 초기화 성공 여부
        private bool _isInitialized;

        // 파괴 처리 시작 여부
        private bool _isDestroyed;

        // 방어 시설이 채택한 점유 핸들
        private long _occupancyHandle;

        // 방어 시설이 채택한 점유 제공자
        private IGridOccupancy _gridOccupancy;

        // 외부에서 초기화 완료 상태를 확인
        public bool IsInitialized
        {
            get
            {
                // 초기화 상태를 반환
                return _isInitialized;
            }
        }

        // 외부에서 파괴 처리 상태를 확인
        public bool IsDestroyed
        {
            get
            {
                // 파괴 상태를 반환
                return _isDestroyed;
            }
        }

        // 외부에서 채택한 점유 핸들을 확인
        public long OccupancyHandle
        {
            get
            {
                // 점유 핸들을 반환
                return _occupancyHandle;
            }
        }

        // 같은 GameObject의 Health를 자동으로 보완
        private void Awake()
        {
            // Inspector 참조가 없을 때 같은 GameObject에서 Health를 확인
            if (_health == null)
            {
                // 같은 GameObject의 Health를 대체 참조로 저장
                _health = GetComponent<Health>();
            }
        }

        // 예약된 Grid 점유를 방어 시설이 채택
        public bool TryInitialize(DefensePlacementContext context, IGridOccupancy gridOccupancy)
        {
            // 이미 사용했거나 파괴된 객체는 다시 초기화하지 않음
            if (_isInitialized || _isDestroyed)
            {
                // 재초기화 실패를 반환
                return false;
            }

            // 점유 제공자가 없으면 소유권을 시작하지 않음
            if (gridOccupancy == null)
            {
                // 제공자 누락 실패를 반환
                return false;
            }

            // Health가 없으면 전투 생명주기를 연결하지 않음
            if (_health == null)
            {
                // Health 누락 실패를 반환
                return false;
            }

            // 양수 점유 핸들만 채택
            if (context.OccupancyHandle <= 0)
            {
                // 잘못된 핸들 실패를 반환
                return false;
            }

            // 요청 원점이 아직 예약되어 있는지 확인
            if (!gridOccupancy.IsOccupied(context.Origin))
            {
                // 예약되지 않은 원점 실패를 반환
                return false;
            }

            // 점유 제공자를 먼저 저장
            _gridOccupancy = gridOccupancy;
            // 예약 핸들을 채택
            _occupancyHandle = context.OccupancyHandle;
            // 초기화 상태를 기록
            _isInitialized = true;
            // 성공한 초기화 뒤에만 사망 이벤트를 연결
            _health.Died += HandleHealthDied;
            // 초기화 성공을 반환
            return true;
        }

        // Health 사망 이벤트를 파괴 생명주기로 연결
        private void HandleHealthDied()
        {
            // 사망 시 방어 시설과 점유를 함께 정리
            DestroyBarricade();
        }

        // 방어 시설을 한 번만 파괴하고 점유를 반환
        public void DestroyBarricade()
        {
            // 중복 파괴 요청을 차단
            if (_isDestroyed)
            {
                // 중복 요청 뒤에는 아무 작업도 하지 않음
                return;
            }

            // 파괴 상태를 먼저 기록해 재진입을 차단
            _isDestroyed = true;
            // Health 이벤트를 해제
            UnsubscribeHealth();
            // 채택한 점유 핸들을 한 번 반환
            ReleaseOccupancy();
            // 현재 GameObject를 Unity 파괴 큐에 추가
            Destroy(gameObject);
        }

        // Unity 파괴 시에도 동일한 정리 경로를 보장
        private void OnDestroy()
        {
            // 외부 파괴에서도 중복 없이 상태를 기록
            if (!_isDestroyed)
            {
                // 외부 파괴 상태를 기록
                _isDestroyed = true;
                // Health 이벤트를 해제
                UnsubscribeHealth();
                // 채택한 점유 핸들을 한 번 반환
                ReleaseOccupancy();
            }
        }

        // Health 사망 이벤트를 안전하게 해제
        private void UnsubscribeHealth()
        {
            // 연결된 Health가 있을 때만 이벤트를 해제
            if (_health != null)
            {
                // 사망 이벤트 연결을 제거
                _health.Died -= HandleHealthDied;
            }
        }

        // 채택한 점유 핸들을 최대 한 번 반환
        private void ReleaseOccupancy()
        {
            // 유효한 소유권이 있을 때만 반환
            if (_gridOccupancy == null || _occupancyHandle <= 0)
            {
                // 반환할 점유가 없음을 종료
                return;
            }

            // 현재 핸들을 반환 요청
            _gridOccupancy.TryRelease(_occupancyHandle);
            // 반환 완료 뒤 소유권을 비움
            _gridOccupancy = null;
            // 중복 반환을 막도록 핸들을 무효화
            _occupancyHandle = 0;
        }
    }
}
