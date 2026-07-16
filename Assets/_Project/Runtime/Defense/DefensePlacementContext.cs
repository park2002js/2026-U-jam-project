using System;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Defense
{
    public readonly struct DefensePlacementContext
    {
        // 배치 원점 셀을 보관
        public GridCell Origin { get; }

        // 배치 영역을 보관
        public GridFootprint Footprint { get; }

        // 배치 시스템이 예약한 점유 핸들을 보관
        public long OccupancyHandle { get; }

        // 방어 시설 초기화에 필요한 배치 정보를 생성
        public DefensePlacementContext(GridCell origin, GridFootprint footprint, long occupancyHandle)
        {
            // 양수 핸들만 방어 시설이 소유
            if (occupancyHandle <= 0)
            {
                // 잘못된 핸들은 생성 단계에서 거부
                throw new System.ArgumentOutOfRangeException(nameof(occupancyHandle));
            }

            // 배치 원점을 저장
            Origin = origin;
            // 배치 영역을 저장
            Footprint = footprint;
            // 예약 핸들을 저장
            OccupancyHandle = occupancyHandle;
        }
    }
}
