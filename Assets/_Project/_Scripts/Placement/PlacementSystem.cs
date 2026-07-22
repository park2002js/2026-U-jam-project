using System;
using UnityEngine;
using UJam.Runtime.Defense;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Placement
{
    public sealed class PlacementSystem
    {
        // 설치 가능 여부와 시각화 정보를 담당할 조회 객체
        private PlacementQuery Query { get; }

        // Prefab 설치와 기존 객체 등록과 점유 해제를 담당할 수명주기 객체
        private PlacementLifecycle Lifecycle { get; }

        // Grid만 받아 역할별 객체를 조립
        public PlacementSystem(GridSystem gridSystem)
        {
            // 누락된 Grid 정보 허브를 거부
            if (gridSystem == null)
            {
                // 잘못된 생성 요청을 호출자에게 알림
                throw new ArgumentNullException(nameof(gridSystem));
            }

            // 읽기 전용 설치 판단 객체 생성
            Query = new PlacementQuery(gridSystem);
            // Grid 상태를 바꿀 설치 수명주기 객체 생성
            Lifecycle = new PlacementLifecycle(gridSystem, Query);
        }

        // 지정한 직사각형 영역의 현재 설치 가능 여부 요청
        public bool CanPlace(int row, int col, int rowCount, int columnCount)
        {
            // 설치 가능 여부 담당 객체의 결과 반환
            return Query.CanPlace(row, col, rowCount, columnCount);
        }

        // 설치 시각화에 필요한 위치와 영역과 가능 여부 요청
        public bool TryGetPlacementPreview(
            int row,
            int col,
            int rowCount,
            int columnCount,
            out Vector3 worldPosition,
            out Bounds bounds,
            out bool canPlace)
        {
            // 설치 미리보기 담당 객체의 결과 반환
            return Query.TryGetPlacementPreview(
                row,
                col,
                rowCount,
                columnCount,
                out worldPosition,
                out bounds,
                out canPlace);
        }

        // Prefab 생성과 Grid 점유 요청
        public bool TryPlacePrefab(
            GameObject prefab,
            CellState state,
            int row,
            int col,
            int rowCount,
            int columnCount,
            out long placementId,
            out GameObject instance)
        {
            // Prefab 설치 담당 객체의 결과 반환
            return Lifecycle.TryPlacePrefab(
                prefab,
                state,
                row,
                col,
                rowCount,
                columnCount,
                out placementId,
                out instance);
        }

        // DefenseBase를 상속한 Prefab 생성과 Grid 점유 요청
        public bool TryPlaceDefense(
            DefenseBase prefab,
            int row,
            int col,
            int rowCount,
            int columnCount,
            out long placementId,
            out DefenseBase instance)
        {
            // Defense 설치 담당 객체의 결과 반환
            return Lifecycle.TryPlaceDefense(
                prefab,
                row,
                col,
                rowCount,
                columnCount,
                out placementId,
                out instance);
        }

        // Scene에 미리 존재하는 객체의 Grid 등록 요청
        public bool TryRegisterExistingObject(
            GameObject instance,
            CellState state,
            int row,
            int col,
            int rowCount,
            int columnCount,
            out long placementId)
        {
            // 기존 객체 등록 담당 객체의 결과 반환
            return Lifecycle.TryRegisterExistingObject(
                instance,
                state,
                row,
                col,
                rowCount,
                columnCount,
                out placementId);
        }

        // 설치 식별자의 Grid 점유 해제 요청
        public bool TryReleasePlacement(long placementId)
        {
            // 설치 점유 해제 담당 객체의 결과 반환
            return Lifecycle.TryReleasePlacement(placementId);
        }
    }
}
