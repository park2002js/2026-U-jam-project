using System;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Grid
{
    // 전체 Cell 정보와 Grid 설정을 한 곳에서 제공하는 최소 정보 허브
    public class GridSystem : MonoBehaviour
    {
        #region Singleton

        // 유일한 하나의 객체 생성
        public static GridSystem Instance { get; private set; }

        // Inspector 설정을 읽을 수 있는 Awake에서 싱글톤과 내부 Cell을 준비한다.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            IsInitialized = false;
        }

        #endregion

        #region Grid Information

        // row와 col tuple로 Cell을 찾기 위한 Dictionary
        // Row, Col로 좌표를 지정하면 이에 저장된 Cell 객체를 반환하는 빈 Dictionary를 보유함
        private Dictionary<(int Row, int Col), Cell> _cells;

        // 각 Cell의 가로 길이
        [SerializeField] public float CellWidth;

        // 각 Cell의 세로 길이
        [SerializeField] public float CellHeight;

        // 전체 Grid의 세로 Cell 수
        [SerializeField] public int RowCount;

        // 전체 Grid의 가로 Cell 수
        [SerializeField] public int ColumnCount;

        // Grid의 시작 월드 좌표
        [SerializeField] public Vector3 Origin;

        // 거점이 위치한 Row 칸 수
        [SerializeField] public int BaseCoreRow;

        // 유효한 Grid 정보가 준비됐는지 여부
        public bool IsInitialized { get; private set; }

        #endregion

        #region Initialization

        // Inspector 값은 그대로 두고 런타임 Cell과 초기화 상태만 생성한다.
        private void Initialize()
        {
            if (!IsPositiveFinite(CellWidth) || !IsPositiveFinite(CellHeight) || RowCount <= 0 || ColumnCount <= 0 || !IsFinite(Origin))
            {
                Debug.LogError("[GridSystem] Inspector의 Cell 크기, 개수, 원점 설정을 확인하세요.", this);
                return;
            }

            long cellCount = (long)RowCount * ColumnCount;
            if (cellCount > int.MaxValue)
            {
                Debug.LogError("[GridSystem] Cell 수가 Dictionary 용량을 초과합니다.", this);
                return;
            }

            _cells = new Dictionary<(int Row, int Col), Cell>((int)cellCount);
            for (int row = 0; row < RowCount; row++)
            {
                for (int col = 0; col < ColumnCount; col++) _cells.Add((row, col), new Cell(row, col, CellWidth, CellHeight));
            }

            IsInitialized = true;
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
