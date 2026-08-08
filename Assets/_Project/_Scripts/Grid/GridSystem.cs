using System;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Grid
{
    // 전체 Cell 정보와 Grid 설정을 한 곳에서 제공하는 최소 정보 허브
    public sealed class GridSystem
    {
        #region Singleton

        // 유일한 하나의 객체 생성
        private static readonly GridSystem _instance = new GridSystem();

        // 외부 생성을 막고 단일 인스턴스만 유지
        private GridSystem()
        {
            _cells = new Dictionary<(int Row, int Col), Cell>();
        }

        // 게임 내에서 GridSystem 인스턴스를 사용 가능
        public static GridSystem Instance  { get { return _instance; } }

        #endregion

        #region Grid Information

        // row와 col tuple로 Cell을 찾기 위한 Dictionary
        // Row, Col로 좌표를 지정하면 이에 저장된 Cell 객체를 반환하는 빈 Dictionary를 보유함
        private Dictionary<(int Row, int Col), Cell> _cells;

        // 각 Cell의 가로 길이
        public float CellWidth { get; private set; }

        // 각 Cell의 세로 길이
        public float CellHeight { get; private set; }

        // 전체 Grid의 세로 Cell 수
        public int RowCount { get; private set; }

        // 전체 Grid의 가로 Cell 수
        public int ColumnCount { get; private set; }

        // Grid의 시작 월드 좌표
        public Vector3 Origin { get; private set; }

        // 유효한 Grid 정보가 준비됐는지 여부
        public bool IsInitialized { get; private set; }

        // 거점이 위치하는 Row 줄 
        public int BaseCoreRow { get; private set; }

        #endregion

        #region Initialization

        // 외부 설정으로 전체 Grid와 기본 None Cell을 다시 생성
        public bool Initialize(
            float cellWidth,    // 단위 Cell 하나의 가로 길이
            float cellHeight,   // 단위 Cell 하나의 세로 길이
            int rowCount,       // 총 격자의 세로 길이
            int columnCount,    // 총 격자의 가로 길이
            Vector3 origin,     // 총 격자의 원점 World 좌표
            int baseCoreRow)    // 거점의 Row 줄 위치
        {
            // Cell 크기와 Grid 개수와 월드 좌표가 모두 유효한지 확인
            if (!IsPositiveFinite(cellWidth)
                || !IsPositiveFinite(cellHeight)
                || rowCount <= 0
                || columnCount <= 0
                || !IsFinite(origin))
            {
                // 기존 Grid를 건드리지 않은 초기화 실패 반환
                return false;
            }

            // Dictionary가 수용할 전체 Cell 개수
            long cellCount = (long)rowCount * columnCount;

            // Dictionary 용량을 넘는 Grid는 생성하지 않음
            if (cellCount > int.MaxValue)
            {
                // 지나치게 큰 Grid 초기화 실패 반환
                return false;
            }

            // 위에서 계산한 갯수만큼 빈 cell 저장 공간을 생성
            Dictionary<(int Row, int Col), Cell> initializedCells = new Dictionary<(int Row, int Col), Cell>((int)cellCount);

            // 전체 세로 Cell을 순서대로 생성
            for (int row = 0; row < rowCount; row += 1)
            {
                // 현재 row의 모든 가로 Cell을 생성
                for (int col = 0; col < columnCount; col += 1)
                {
                    // 현재 row와 col을 Dictionary key로 사용할 tuple 좌표
                    (int Row, int Col) key = (row, col);
                    // 현재 좌표의 기본 None Cell 정보
                    Cell cell = new Cell(row, col, cellWidth, cellHeight);

                    // cell을 생성해서 dictionary에 할당
                    initializedCells.Add(key, cell);
                }
            }

            CellWidth = cellWidth;
            CellHeight = cellHeight;
            RowCount = rowCount;
            ColumnCount = columnCount;
            Origin = origin;
            _cells = initializedCells; // GridSystem을 통해 외부에서 사용할 수 있도록 변수에 Dictionary 할당
            IsInitialized = true;

            // 전체 Cell 생성이 끝난 초기화 성공 반환
            return true;
        }

        #endregion

        #region Cell 관련 Public 함수들

        // 외부에서 Cell 상태를 업데이트 하기 위해 호출하는 함수
        public bool UpdateCellState(int row, int col, CellState state, GameObject obj = null)
        {
            // 정의되지 않은 상태 값은 Cell에 저장하지 않음
            if (!Enum.IsDefined(typeof(CellState), state))
            {
                // 잘못된 상태 갱신 실패 반환
                return false;
            }

            // row와 col에 해당하는 Cell 정보
            Cell cell;

            // 초기화되지 않았거나 범위 밖인 Cell은 갱신하지 않음
            if (!TryFindCell(row, col, out cell))
            {
                // 찾을 수 없는 Cell 갱신 실패 반환
                return false;
            }

            // Def 상태는 실제 방어 건물 객체와 함께 저장하도록 하는 방어 코드
            if (state == CellState.Def && obj == null)
            {
                // 연결할 방어 건물이 없는 Def 갱신 실패 반환
                return false;
            }

            // 검증된 Cell 상태와 방어 건물 연결을 반영
            cell.UpdateState(state, obj);

            // Cell 상태 갱신 성공 반환
            return true;
        }

        // row와 col로 특정 Cell 상태를 조회
        public bool TryGetCellState(int row, int col, out CellState state)
        {
            // 조회 실패 시 사용할 기본 None 상태
            state = CellState.None;
            // row와 col에 해당하는 Cell 정보
            Cell cell;

            // 초기화되지 않았거나 범위 밖인 Cell을 구분
            if (!TryFindCell(row, col, out cell))
            {
                // 찾을 수 없는 Cell 조회 실패 반환
                return false;
            }

            state = cell.State;

            // 저장된 Cell 상태 조회 성공 반환
            return true;
        }

        // row와 col로 연결된 방어 건물 객체를 조회
        public GameObject GetDefenseObject(int row, int col)
        {
            // row와 col에 해당하는 Cell 정보
            Cell cell;

            // 초기화되지 않았거나 범위 밖인 Cell에는 연결 객체가 없음
            if (!TryFindCell(row, col, out cell))
            {
                // 찾을 수 없는 Cell의 빈 객체 반환
                return null;
            }

            // Def 상태가 아니면 방어 건물 객체를 공개하지 않음
            if (cell.State != CellState.Def)
            {
                // 방어 건물이 없는 Cell의 빈 객체 반환
                return null;
            }

            // Cell에 연결된 방어 건물 객체 반환
            return cell.Obj;
        }

        // public vector2<int, int> GetGridCoordinates(int x, int z)
        // {

        // }

        #endregion

        #region 내부 Query 처리 함수들

        // row와 col을 Dictionary key로 바꿔 Cell을 조회
        private bool TryFindCell(int row, int col, out Cell cell)
        {
            // 조회 실패 시 사용할 빈 Cell 참조
            cell = null;

            // Grid 초기화 전에는 Dictionary를 조회하지 않음
            if (!IsInitialized)
            {
                // 초기화 전 조회 실패 반환
                return false;
            }

            // row와 col이 Grid 범위에 있는지 확인
            if (row < 0 || row >= RowCount || col < 0 || col >= ColumnCount)
            {
                // 범위 밖 조회 실패 반환
                return false;
            }

            // row와 col을 Dictionary tuple key로 변환
            (int Row, int Col) key = (row, col);

            // Dictionary의 Cell 조회 결과 반환
            return _cells.TryGetValue(key, out cell);
        }

        #endregion

        #region 유효성 validation

        // 값이 양수이며 NaN이나 Infinity가 아닌지 확인
        private static bool IsPositiveFinite(float value)
        {
            // 유효한 양수 여부 반환
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        // 월드 좌표의 모든 성분이 유한한지 확인
        private static bool IsFinite(Vector3 value)
        {
            // 세 좌표의 유한 여부 반환
            return !float.IsNaN(value.x)
                && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y)
                && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z)
                && !float.IsInfinity(value.z);
        }

        #endregion
    }
}
