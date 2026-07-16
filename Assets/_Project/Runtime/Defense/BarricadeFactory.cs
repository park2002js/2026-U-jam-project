using UnityEngine;
using UJam.Runtime.BuildingPlacement;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Defense
{
    public sealed class BarricadeFactory : MonoBehaviour, IDefenseFactory
    {
        // 미래 Barricade Prefab을 Inspector에서 연결
        [SerializeField] private Barricade _barricadePrefab;

        // 배치 시스템이 주입한 Grid 점유 제공자
        private IGridOccupancy _gridOccupancy;

        // Grid 점유 제공자를 명시적으로 주입
        public void ConfigureGridOccupancy(IGridOccupancy gridOccupancy)
        {
            // 전달받은 점유 제공자를 저장
            _gridOccupancy = gridOccupancy;
        }

        // 배치 요청으로 Barricade를 생성
        public bool TryCreate(DefenseSpawnRequest request)
        {
            // 요청이 없으면 생성하지 않음
            if (request == null)
            {
                // 요청 누락 실패를 반환
                return false;
            }

            // Prefab이 없으면 Instantiate를 호출하지 않음
            if (_barricadePrefab == null)
            {
                // Prefab 누락 실패를 반환
                return false;
            }

            // 점유 제공자가 없으면 생성하지 않음
            if (_gridOccupancy == null)
            {
                // 점유 제공자 누락 실패를 반환
                return false;
            }

            // 요청 원점의 예약이 없으면 생성하지 않음
            if (!_gridOccupancy.IsOccupied(request.Origin))
            {
                // 예약 누락 실패를 반환
                return false;
            }

            // 예약 핸들을 포함한 초기화 문맥을 생성
            var context = new DefensePlacementContext(request.Origin, request.Footprint, request.ReservationHandle);
            // Prefab 인스턴스를 한 번 생성
            var barricade = Instantiate(_barricadePrefab, request.WorldPosition, Quaternion.identity);

            // 생성된 인스턴스가 없으면 실패 처리
            if (barricade == null)
            {
                // 실패 시 핸들은 BuildingPlacement가 관리
                return false;
            }

            // 생성된 인스턴스가 예약 핸들을 채택
            if (!barricade.TryInitialize(context, _gridOccupancy))
            {
                // 초기화 실패 인스턴스만 제거
                Destroy(barricade.gameObject);
                // 실패 시 핸들은 BuildingPlacement가 관리
                return false;
            }

            // 성공 시 핸들은 Barricade가 소유
            return true;
        }
    }
}
