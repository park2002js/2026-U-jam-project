using System;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Grid
{
    // Cell의 통과 여부와 양의 유한 이동 비용
    public readonly struct GridCellState
    {
        // Cell의 기본 통과 가능 여부 저장
        private readonly bool _isPassable;
        // Cell의 양의 유한 이동 비용 저장
        private readonly float _movementCost;

        // Cell 상태와 이동 비용을 저장
        public GridCellState(bool isPassable, float movementCost)
        {
            // 이동 비용이 양의 유한 값인지 확인
            if (float.IsNaN(movementCost) || float.IsInfinity(movementCost) || movementCost <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(movementCost));
            }

            _isPassable = isPassable;
            _movementCost = movementCost;
        }

        // Cell의 기본 통과 가능 여부
        public bool IsPassable
        {
            get
            {
                // 저장된 통과 가능 여부 반환
                return _isPassable;
            }
        }

        // Cell의 양의 유한 이동 비용
        public float MovementCost
        {
            get
            {
                // 저장된 이동 비용 반환
                return _movementCost;
            }
        }

        // 기본 생성 상태가 유효한지 확인
        internal bool IsValid
        {
            get
            {
                // 기본 struct의 0 비용 상태를 무효로 판정
                return _movementCost > 0f
                    && !float.IsNaN(_movementCost)
                    && !float.IsInfinity(_movementCost);
            }
        }
    }

    // Grid 계약 네 개의 단일 계산과 상태를 소유
    public sealed class GridSystem : IGridMetrics, IGridNavigation, IGridOccupancy, IGridAreaQuery
    {
        // 생성 시 고정된 Cell 간격 저장
        private readonly float _cellSize;
        // GridCell(0, 0)의 대표 World 위치 저장
        private readonly Vector3 _origin;
        // Grid의 Cell 너비 저장
        private readonly int _width;
        // Grid의 Cell 높이 저장
        private readonly int _height;
        // 새 Cell에 적용할 기본 상태 저장
        private readonly GridCellState _defaultCellState;
        // 현재 Cell 상태 장부 저장
        private Dictionary<GridCell, GridCellState> _cellStates;
        // Cell별 점유 Handle 장부 저장
        private readonly Dictionary<GridCell, long> _occupiedCells;
        // Handle별 점유 Cell 목록 저장
        private readonly Dictionary<long, List<GridCell>> _handleCells;
        // 다음에 발급할 양의 Handle 저장
        private long _nextHandle;
        // Rebuild 횟수를 나타내는 version 저장
        private int _version;

        // 생성 시 고정된 Cell 간격
        public float CellSize
        {
            get
            {
                // Grid의 Cell 간격 반환
                return _cellSize;
            }
        }

        // GridCell(0, 0)의 대표 World 위치
        public Vector3 Origin
        {
            get
            {
                // Grid 원점 반환
                return _origin;
            }
        }

        // 현재 Grid version
        public int Version
        {
            get
            {
                // 현재 version 반환
                return _version;
            }
        }

        // Rebuild 완료 후 새 version 통지
        public event Action<int> VersionChanged;

        // 고정 Grid 설정과 기본 Cell 상태로 새 Grid 생성
        public GridSystem(
            float cellSize,
            Vector3 origin,
            int width,
            int height,
            bool defaultIsPassable = true,
            float defaultMovementCost = 1f)
        {
            // Cell 간격이 양의 유한 값인지 확인
            if (float.IsNaN(cellSize) || float.IsInfinity(cellSize) || cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            }

            // 원점의 세 좌표가 유한 값인지 확인
            if (!IsFinite(origin))
            {
                throw new ArgumentException("Origin must contain finite coordinates", nameof(origin));
            }

            // Grid 너비가 양수인지 확인
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            // Grid 높이가 양수인지 확인
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            // 기본 이동 비용이 양의 유한 값인지 확인
            if (float.IsNaN(defaultMovementCost)
                || float.IsInfinity(defaultMovementCost)
                || defaultMovementCost <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultMovementCost));
            }

            _cellSize = cellSize;
            _origin = origin;
            _width = width;
            _height = height;
            _defaultCellState = new GridCellState(defaultIsPassable, defaultMovementCost);
            _cellStates = new Dictionary<GridCell, GridCellState>();
            _occupiedCells = new Dictionary<GridCell, long>();
            _handleCells = new Dictionary<long, List<GridCell>>();
            _nextHandle = 1L;
            _version = 0;
            _cellStates = CreateDefaultCellStates();
        }

        // World X/Z를 가장 가까운 GridCell X/Y로 변환
        public GridCell WorldToCell(Vector3 worldPosition)
        {
            // World 좌표가 유한 값인지 확인
            if (!IsFinite(worldPosition))
            {
                throw new ArgumentException("World position must contain finite coordinates", nameof(worldPosition));
            }

            // 원점 기준 World X를 Cell X로 변환
            float relativeX = (worldPosition.x - _origin.x) / _cellSize;
            // 원점 기준 World Z를 Cell Y로 변환
            float relativeY = (worldPosition.z - _origin.z) / _cellSize;
            // Unity 기존 GridManager와 같은 nearest-cell 반올림
            int cellX = Mathf.RoundToInt(relativeX);
            // Unity 기존 GridManager와 같은 nearest-cell 반올림
            int cellY = Mathf.RoundToInt(relativeY);
            // 변환된 Cell 좌표 구성
            GridCell cell = new GridCell(cellX, cellY);

            // 가장 가까운 Cell 좌표 반환
            return cell;
        }

        // GridCell 대표 위치를 World X/Y/Z로 변환
        public Vector3 CellToWorld(GridCell cell)
        {
            // Cell X를 World X로 변환
            float worldX = _origin.x + cell.X * _cellSize;
            // Origin Y를 대표 위치의 높이로 유지
            float worldY = _origin.y;
            // Cell Y를 Unity World Z로 변환
            float worldZ = _origin.z + cell.Y * _cellSize;
            // Cell 대표 World 위치 구성
            Vector3 worldPosition = new Vector3(worldX, worldY, worldZ);

            // Cell 중심에 해당하는 대표 위치 반환
            return worldPosition;
        }

        // 기본 Cell 상태와 승인된 override로 Grid 상태 재구축
        public void Rebuild(IReadOnlyDictionary<GridCell, GridCellState> cellStates = null)
        {
            // 기존 상태를 건드리지 않는 새 기본 상태 장부
            Dictionary<GridCell, GridCellState> rebuiltStates = CreateDefaultCellStates();

            // 전달된 Cell 상태 override 전체 검증
            if (cellStates != null)
            {
                // 전달된 각 override를 범위와 비용 기준으로 확인
                foreach (KeyValuePair<GridCell, GridCellState> entry in cellStates)
                {
                    // override Cell이 Grid 범위 안인지 확인
                    if (!IsWithinBounds(entry.Key))
                    {
                        throw new ArgumentOutOfRangeException(nameof(cellStates));
                    }

                    // 기본 생성자가 아닌 유효한 Cell 상태인지 확인
                    if (!entry.Value.IsValid)
                    {
                        throw new ArgumentException("Cell state must have a positive finite movement cost", nameof(cellStates));
                    }

                    rebuiltStates[entry.Key] = entry.Value;
                }
            }

            _cellStates = rebuiltStates;
            _occupiedCells.Clear();
            _handleCells.Clear();

            // version overflow에서도 이전 값과 다른 version 유지
            if (_version == int.MaxValue)
            {
                _version = 0;
            }
            else
            {
                _version += 1;
            }

            // 완성된 새 상태를 본 뒤 event 대상 복사
            Action<int> versionChanged = VersionChanged;

            // 구독자가 있을 때 새 version을 한 번 통지
            if (versionChanged != null)
            {
                versionChanged(_version);
            }
        }

        // Cell이 범위 안이고 기본 통과 가능하며 미점유인지 확인
        public bool IsPassable(GridCell cell)
        {
            // 범위 밖 Cell은 통과 불가
            if (!IsWithinBounds(cell))
            {
                // 범위 밖 결과 반환
                return false;
            }

            // 기본 차단 Cell은 통과 불가
            if (!_cellStates[cell].IsPassable)
            {
                // 기본 차단 결과 반환
                return false;
            }

            // 점유 Cell은 통과 불가
            if (_occupiedCells.ContainsKey(cell))
            {
                // 점유 차단 결과 반환
                return false;
            }

            // 정상 통과 가능 결과 반환
            return true;
        }

        // 정상 Cell의 비용을 반환하고 실패는 무한대로 표시
        public float GetMovementCost(GridCell cell)
        {
            // 범위 밖 Cell은 이동 불가
            if (!IsWithinBounds(cell))
            {
                // 범위 밖 비용 반환
                return float.PositiveInfinity;
            }

            // 기본 차단 Cell은 이동 불가
            if (!_cellStates[cell].IsPassable)
            {
                // 기본 차단 비용 반환
                return float.PositiveInfinity;
            }

            // 점유 Cell은 이동 불가
            if (_occupiedCells.ContainsKey(cell))
            {
                // 점유 비용 반환
                return float.PositiveInfinity;
            }

            // 정상 Cell의 승인된 이동 비용 반환
            return _cellStates[cell].MovementCost;
        }

        // Footprint 전체를 원자적으로 점유하고 양의 Handle 발급
        public bool TryOccupy(GridCell origin, GridFootprint footprint, out long handle)
        {
            // 실패 시 Handle 0으로 초기화
            handle = 0L;

            // Footprint 전체의 범위와 통과 가능 상태 확인
            if (!IsAreaPassable(origin, footprint))
            {
                // 부분 점유 없이 실패 결과 반환
                return false;
            }

            // Handle overflow 직전의 새 점유 차단
            if (_nextHandle >= long.MaxValue)
            {
                // overflow를 피한 실패 결과 반환
                return false;
            }

            // 점유할 Cell 목록 생성
            List<GridCell> cells = GetFootprintCells(origin, footprint);
            // 현재 Handle 값 확보
            long newHandle = _nextHandle;
            // 다음 Handle을 단조 증가
            _nextHandle += 1L;

            // 점유 장부에 모든 Cell을 한 번에 반영
            foreach (GridCell cell in cells)
            {
                _occupiedCells[cell] = newHandle;
            }

            _handleCells[newHandle] = cells;
            handle = newHandle;

            // 새 Handle을 포함한 성공 결과 반환
            return true;
        }

        // 유효한 Handle의 모든 Cell을 점유 장부에서 해제
        public bool TryRelease(long handle)
        {
            // Handle이 양수인지 확인
            if (handle <= 0L)
            {
                // 알 수 없는 Handle 실패 결과 반환
                return false;
            }

            // Handle에 연결된 Cell 목록 조회
            List<GridCell> cells;
            // 등록된 Handle인지 확인
            if (!_handleCells.TryGetValue(handle, out cells))
            {
                // 알 수 없는 Handle 실패 결과 반환
                return false;
            }

            // Handle이 소유한 Cell만 점유 장부에서 제거
            foreach (GridCell cell in cells)
            {
                _occupiedCells.Remove(cell);
            }

            _handleCells.Remove(handle);

            // 정상 해제 결과 반환
            return true;
        }

        // Cell이 유효한 점유 상태인지 확인
        public bool IsOccupied(GridCell cell)
        {
            // 점유 장부에 있는 Cell 조회 결과 반환
            return _occupiedCells.ContainsKey(cell);
        }

        // Footprint 전체가 Grid 범위 안인지 확인
        public bool IsAreaWithinBounds(GridCell origin, GridFootprint footprint)
        {
            // 유효한 Footprint 크기와 회전인지 확인
            if (!TryGetRotatedSize(footprint, out int rotatedWidth, out int rotatedHeight))
            {
                // 잘못된 Footprint 범위 결과 반환
                return false;
            }

            // origin Cell이 범위 시작점으로 유효한지 확인
            if (!IsWithinBounds(origin))
            {
                // 시작점 범위 밖 결과 반환
                return false;
            }

            // Footprint 배타 범위 계산
            long maxX = (long)origin.X + rotatedWidth;
            // Footprint 배타 범위 계산
            long maxY = (long)origin.Y + rotatedHeight;
            // 배타 상한이 Grid 크기 안인지 확인
            bool withinBounds = maxX <= _width && maxY <= _height;

            // Footprint 범위 결과 반환
            return withinBounds;
        }

        // Footprint 전체가 범위 안이고 통과 가능한지 확인
        public bool IsAreaPassable(GridCell origin, GridFootprint footprint)
        {
            // Footprint 전체가 범위 안인지 확인
            if (!IsAreaWithinBounds(origin, footprint))
            {
                // 범위 밖 영역 결과 반환
                return false;
            }

            // Footprint Cell 목록 생성
            List<GridCell> cells = GetFootprintCells(origin, footprint);

            // 모든 Cell이 기본 통과 가능하고 미점유인지 확인
            foreach (GridCell cell in cells)
            {
                // 하나라도 통과 불가면 영역 실패
                if (!IsPassable(cell))
                {
                    // 부분 점유를 허용하지 않는 결과 반환
                    return false;
                }
            }

            // 전체 영역 통과 가능 결과 반환
            return true;
        }

        // Grid 전체를 기본 Cell 상태로 채운 새 장부 생성
        private Dictionary<GridCell, GridCellState> CreateDefaultCellStates()
        {
            // 기본 상태를 저장할 장부
            Dictionary<GridCell, GridCellState> states = new Dictionary<GridCell, GridCellState>();
            // Cell Y 순회 시작값
            int y = 0;

            // Grid 높이만큼 행 순회
            while (y < _height)
            {
                // Cell X 순회 시작값
                int x = 0;

                // Grid 너비만큼 열 순회
                while (x < _width)
                {
                    // 현재 행과 열의 Cell 구성
                    GridCell cell = new GridCell(x, y);
                    // 현재 Cell에 기본 상태 저장
                    states[cell] = _defaultCellState;
                    // 다음 열로 이동
                    x += 1;
                }

                // 다음 행으로 이동
                y += 1;
            }

            // 기본 상태 장부 반환
            return states;
        }

        // 회전에 따른 실제 Footprint 너비와 높이 확인
        private bool TryGetRotatedSize(GridFootprint footprint, out int width, out int height)
        {
            // 잘못된 기본 struct 입력에 대비한 출력 초기화
            width = 0;
            height = 0;

            // Footprint 크기가 양수인지 확인
            if (footprint.Width <= 0 || footprint.Height <= 0)
            {
                // 잘못된 크기 실패 결과 반환
                return false;
            }

            // quarter turn이 승인된 0부터 3인지 확인
            if (footprint.RotationQuarterTurns < 0 || footprint.RotationQuarterTurns > 3)
            {
                // 잘못된 회전 실패 결과 반환
                return false;
            }

            // 홀수 quarter turn에서 너비와 높이 교환
            if (footprint.RotationQuarterTurns % 2 == 0)
            {
                // 0도와 180도 실제 너비
                width = footprint.Width;
                // 0도와 180도 실제 높이
                height = footprint.Height;
            }
            else
            {
                // 90도와 270도 실제 너비
                width = footprint.Height;
                // 90도와 270도 실제 높이
                height = footprint.Width;
            }

            // 회전된 Footprint 크기 확인 성공
            return true;
        }

        // 유효한 Footprint의 모든 Cell을 양의 방향으로 생성
        private List<GridCell> GetFootprintCells(GridCell origin, GridFootprint footprint)
        {
            // 회전된 너비와 높이 확인
            if (!TryGetRotatedSize(footprint, out int rotatedWidth, out int rotatedHeight))
            {
                throw new ArgumentException("Footprint must have positive dimensions and a quarter-turn rotation", nameof(footprint));
            }

            // Footprint Cell을 담을 목록
            List<GridCell> cells = new List<GridCell>(rotatedWidth * rotatedHeight);
            // Cell Y 순회 시작값
            int y = 0;

            // 회전된 높이만큼 행 순회
            while (y < rotatedHeight)
            {
                // Cell X 순회 시작값
                int x = 0;

                // 회전된 너비만큼 열 순회
                while (x < rotatedWidth)
                {
                    // origin에서 양의 방향으로 이동한 Cell 구성
                    GridCell cell = new GridCell(origin.X + x, origin.Y + y);
                    // Footprint 목록에 Cell 추가
                    cells.Add(cell);
                    // 다음 열로 이동
                    x += 1;
                }

                // 다음 행으로 이동
                y += 1;
            }

            // Footprint Cell 목록 반환
            return cells;
        }

        // Cell이 0부터 width·height 배타 상한 안인지 확인
        private bool IsWithinBounds(GridCell cell)
        {
            // GridCell X가 유효한지 확인
            if (cell.X < 0 || cell.X >= _width)
            {
                // X 범위 밖 결과 반환
                return false;
            }

            // GridCell Y가 유효한지 확인
            if (cell.Y < 0 || cell.Y >= _height)
            {
                // Y 범위 밖 결과 반환
                return false;
            }

            // Grid 범위 안 결과 반환
            return true;
        }

        // Vector3의 모든 성분이 유한 값인지 확인
        private static bool IsFinite(Vector3 value)
        {
            // X 성분의 유한 여부 확인
            if (float.IsNaN(value.x) || float.IsInfinity(value.x))
            {
                // X 성분 무효 결과 반환
                return false;
            }

            // Y 성분의 유한 여부 확인
            if (float.IsNaN(value.y) || float.IsInfinity(value.y))
            {
                // Y 성분 무효 결과 반환
                return false;
            }

            // Z 성분의 유한 여부 확인
            if (float.IsNaN(value.z) || float.IsInfinity(value.z))
            {
                // Z 성분 무효 결과 반환
                return false;
            }

            // 모든 성분 유효 결과 반환
            return true;
        }
    }
}
