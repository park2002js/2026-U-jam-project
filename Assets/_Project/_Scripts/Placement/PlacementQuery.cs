using System;
using UnityEngine;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Placement
{
    internal sealed class PlacementQuery
    {
        // Cell 상태와 월드 좌표를 제공할 Grid 정보 허브
        private readonly GridSystem _gridSystem;

        // 읽기 전용 설치 판단에 사용할 Grid 저장
        internal PlacementQuery(GridSystem gridSystem)
        {
            // 누락된 Grid 정보 허브를 거부
            if (gridSystem == null)
            {
                // 잘못된 생성 요청을 호출자에게 알림
                throw new ArgumentNullException(nameof(gridSystem));
            }

            // 설치 판단에 사용할 Grid 저장
            _gridSystem = gridSystem;
        }

        // 지정한 직사각형 영역의 현재 설치 가능 여부 확인
        internal bool CanPlace(int row, int col, int rowCount, int columnCount)
        {
            // Grid 범위 안의 양수 영역만 조회
            if (!IsValidArea(row, col, rowCount, columnCount))
            {
                // 잘못된 영역은 설치 불가 처리
                return false;
            }

            // 영역의 모든 세로 Cell 순회
            for (int rowOffset = 0; rowOffset < rowCount; rowOffset += 1)
            {
                // 현재 세로 위치의 모든 가로 Cell 순회
                for (int colOffset = 0; colOffset < columnCount; colOffset += 1)
                {
                    // 현재 Cell 상태를 받을 값
                    CellState state;

                    // 조회할 수 없거나 비어 있지 않은 Cell 차단
                    if (!_gridSystem.TryGetCellState(row + rowOffset, col + colOffset, out state)
                        || state != CellState.None)
                    {
                        // 하나라도 사용할 수 없는 영역 결과 반환
                        return false;
                    }
                }
            }

            // 전체 Cell이 비어 있는 영역 결과 반환
            return true;
        }

        // 설치 시각화에 필요한 월드 위치와 영역과 가능 여부 계산
        internal bool TryGetPlacementPreview(
            int row,
            int col,
            int rowCount,
            int columnCount,
            out Vector3 worldPosition,
            out Bounds bounds,
            out bool canPlace)
        {
            // 잘못된 요청에서 사용할 기본 설치 위치
            worldPosition = default;
            // 잘못된 요청에서 사용할 빈 시각화 영역
            bounds = default;
            // 잘못된 요청의 설치 불가 기본값
            canPlace = false;

            // Grid 범위 안의 양수 영역만 시각화
            if (!IsValidArea(row, col, rowCount, columnCount))
            {
                // 시각화 정보 생성 실패 반환
                return false;
            }

            // Prefab을 설치할 첫 Cell의 월드 위치
            worldPosition = GetWorldPosition(row, col);
            // 전체 영역 중심의 월드 X 좌표
            float centerX = worldPosition.x + (columnCount - 1) * _gridSystem.CellWidth * 0.5f;
            // 전체 영역 중심의 월드 Z 좌표
            float centerZ = worldPosition.z + (rowCount - 1) * _gridSystem.CellHeight * 0.5f;
            // 시각화할 전체 영역 중심
            Vector3 center = new Vector3(centerX, worldPosition.y, centerZ);
            // 시각화할 전체 영역 크기
            Vector3 size = new Vector3(
                columnCount * _gridSystem.CellWidth,
                0f,
                rowCount * _gridSystem.CellHeight);

            // Unity 기본 Bounds로 시각화 영역 구성
            bounds = new Bounds(center, size);
            // 같은 요청으로 현재 설치 가능 여부 계산
            canPlace = CanPlace(row, col, rowCount, columnCount);

            // 유효한 시각화 정보 생성 성공 반환
            return true;
        }

        // 첫 Cell 좌표를 Grid 기준 월드 위치로 변환
        internal Vector3 GetWorldPosition(int row, int col)
        {
            // Grid 원점과 Cell 크기로 계산한 월드 X 좌표
            float worldX = _gridSystem.Origin.x + col * _gridSystem.CellWidth;
            // Grid 원점과 Cell 크기로 계산한 월드 Z 좌표
            float worldZ = _gridSystem.Origin.z + row * _gridSystem.CellHeight;

            // Prefab 설치에 사용할 월드 위치 반환
            return new Vector3(worldX, _gridSystem.Origin.y, worldZ);
        }

        // 요청 영역이 초기화된 Grid 범위 안인지 확인
        private bool IsValidArea(int row, int col, int rowCount, int columnCount)
        {
            // 초기화 전 Grid와 잘못된 시작 좌표와 크기 차단
            if (!_gridSystem.IsInitialized
                || row < 0
                || col < 0
                || rowCount <= 0
                || columnCount <= 0
                || rowCount > _gridSystem.RowCount
                || columnCount > _gridSystem.ColumnCount)
            {
                // 사용할 수 없는 영역 결과 반환
                return false;
            }

            // 덧셈 overflow 없이 마지막 Cell이 Grid 범위 안인지 반환
            return row <= _gridSystem.RowCount - rowCount
                && col <= _gridSystem.ColumnCount - columnCount;
        }
    }
}
