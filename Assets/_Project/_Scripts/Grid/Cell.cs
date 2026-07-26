using UnityEngine;

namespace UJam.Runtime.Grid
{
    // Cell에 저장할 현재 사용 목적
    public enum CellState
    {
        None,   // 비어 있는 Cell
        Def,    // 방어 건물이 존재하는 Cell
        Env     // 환경 요소가 존재하는 Cell
    }

    // 하나의 Grid 단위가 제공할 최소 정보
    public sealed class Cell
    {
        #region Cell Info (격자 한칸 정보)

        public int Row { get; } // 전체 Grid 안에서의 세로 위치

        public int Col { get; } // 전체 Grid 안에서의 가로 위치

        public float Width { get; } // Cell의 가로 길이

        public float Height { get; }// Cell의 세로 길이

        public CellState State { get; private set; } // Cell의 현재 사용 상태

        public GameObject Obj { get; private set; } // Cell에 저장할 건물 객체 (조회 목적)

        #endregion


        // 좌표와 크기로 기본 None Cell을 생성
        internal Cell(int row, int col, float width, float height)
        {
            Row = row;
            Col = col;
            Width = width;
            Height = height;
            State = CellState.None;
            Obj = null;
        }

        // GridSystem이 검증한 상태와 방어 건물 연결을 저장
        internal void UpdateState(CellState state, GameObject defenseObject)
        {
            State = state;

            // Def 상태에서만 전달된 방어 건물 객체를 유지
            if (state == CellState.Def)
            {
                Obj = defenseObject;
            }
            // None과 Env 상태에서는 이전 방어 건물 연결을 제거
            else
            {
                Obj = null;
            }
        }
    }
}
