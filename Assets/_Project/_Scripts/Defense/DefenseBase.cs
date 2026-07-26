using System;
using UnityEngine;

namespace UJam.Runtime.Defense
{
    public abstract class DefenseBase : MonoBehaviour
    {
        // Inspector에서 설정할 방어 건물 이름
        [SerializeField] private string _buildingName;

        // 구매 또는 설치에 필요한 비용
        [SerializeField, Min(0)] private long _cost;

        // 중복 파괴 요청을 막을 현재 상태
        private bool _isDestroyed;

        // 설치 시스템이 점유 해제를 요청할 파괴 알림
        public event Action<DefenseBase> Destroyed;

        // 외부에 공개할 방어 건물 이름
        public string BuildingName { get { return _buildingName; } } // 비어 있지 않은 현재 건물 이름 반환

        // 외부에 공개할 구매 또는 설치 비용
        public long Cost { get { return _cost; } } // 음수가 아닌 현재 비용 반환

        // 외부에서 확인할 파괴 처리 상태
        public bool IsDestroyed { get { return _isDestroyed; } } // 현재 파괴 처리 상태 반환

        // 공통 설정값을 안전한 범위로 보정
        protected virtual void Awake()
        {
            // 건물 이름이 비어 있으면 구체 Class 이름 사용
            if (string.IsNullOrWhiteSpace(_buildingName))
            {
                _buildingName = GetType().Name;
            }

            // 잘못된 음수 비용을 무료로 보정
            if (_cost < 0L) _cost = 0L;
        }

        // 방어 건물을 한 번만 파괴
        public void DestroyDefense()
        {
            // 이미 시작한 파괴 요청 차단
            if (!TryNotifyDestroyed())
            {
                // 중복 파괴 요청 종료
                return;
            }

            // 현재 방어 건물 GameObject 제거 예약
            UnityEngine.Object.Destroy(gameObject);
        }

        // 외부 제거에서도 설치 시스템에 파괴를 알림
        protected virtual void OnDestroy()
        {
            // 아직 알리지 않은 외부 제거를 한 번 통지
            TryNotifyDestroyed();
        }

        // 파생 방어 건물이 구체 파괴 동작을 정의할 경계
        protected virtual void OnDefenseDestroyed()
        {
            // Animation과 효과 같은 종류별 파괴 동작 확장 지점
        }

        // 파괴 상태와 종류별 동작과 외부 알림을 한 번 처리
        private bool TryNotifyDestroyed()
        {
            // 이미 처리한 파괴 알림 차단
            if (_isDestroyed)
            {
                // 중복 처리 실패 반환
                return false;
            }

            _isDestroyed = true;

            // 파생 방어 건물의 파괴 동작 호출
            OnDefenseDestroyed();

            // 설치 시스템과 외부 구독자에 파괴 통지
            Destroyed?.Invoke(this);

            // 최초 파괴 처리 성공 반환
            return true;
        }
    }
}
