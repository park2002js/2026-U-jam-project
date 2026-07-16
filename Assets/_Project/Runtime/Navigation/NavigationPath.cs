using System;
using System.Collections.Generic;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Navigation
{
    public readonly struct NavigationPath
    {
        // 경로 시작 Cell 저장
        private readonly GridCell _startCell;
        // 경로 목적지 Cell 저장
        private readonly GridCell _destination;
        // 외부 변경을 막은 경로 Cell 목록 저장
        private readonly IReadOnlyList<GridCell> _cells;

        // 시작점과 목적지와 경로 Cell을 불변 값으로 복사
        public NavigationPath(GridCell startCell, GridCell destination, IReadOnlyList<GridCell> cells)
        {
            // 경로 Cell 목록 필수 여부 확인
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            // 외부 목록과 분리할 새 경로 배열 생성
            GridCell[] copiedCells = new GridCell[cells.Count];
            // 경로 Cell 복사 위치
            int index = 0;

            // 모든 경로 Cell을 값으로 복사
            while (index < cells.Count)
            {
                // 현재 경로 Cell 복사
                copiedCells[index] = cells[index];
                // 다음 경로 Cell 위치
                index += 1;
            }

            _startCell = startCell;
            _destination = destination;
            // 복사 배열을 읽기 전용 목록으로 감쌈
            _cells = Array.AsReadOnly(copiedCells);
        }

        // 경로 시작 Cell
        public GridCell StartCell
        {
            get
            {
                // 저장된 시작 Cell 반환
                return _startCell;
            }
        }

        // 경로 목적지 Cell
        public GridCell Destination
        {
            get
            {
                // 저장된 목적지 Cell 반환
                return _destination;
            }
        }

        // 외부에서 수정할 수 없는 경로 Cell 조회
        public IReadOnlyList<GridCell> Cells
        {
            get
            {
                // 복사된 경로 배열을 읽기 전용으로 반환
                return _cells;
            }
        }
    }
}
