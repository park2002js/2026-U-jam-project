using System;
using System.Collections.Generic;
using UnityEngine;
using UJam.Runtime.Defense;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Placement
{
    internal sealed class PlacementLifecycle
    {
        // 설치와 해제로 Cell 상태를 변경할 Grid 정보 허브
        private readonly GridSystem _gridSystem;

        // 설치 가능 여부와 월드 위치를 제공할 조회 객체
        private readonly PlacementQuery _query;

        // 설치 식별자로 점유 영역을 찾을 장부
        private readonly Dictionary<long, PlacementRecord> _placements = new Dictionary<long, PlacementRecord>();

        // 설치된 Defense와 설치 식별자를 연결할 장부
        private readonly Dictionary<DefenseBase, long> _defensePlacements = new Dictionary<DefenseBase, long>();

        // 다음 설치에 부여할 양수 식별자
        private long _nextPlacementId = 1L;

        // 상태 변경에 필요한 Grid와 읽기 전용 조회 객체 저장
        internal PlacementLifecycle(GridSystem gridSystem, PlacementQuery query)
        {
            // 누락된 Grid 정보 허브를 거부
            if (gridSystem == null)
            {
                // 잘못된 Grid 생성 요청을 호출자에게 알림
                throw new ArgumentNullException(nameof(gridSystem));
            }

            // 누락된 설치 조회 객체를 거부
            if (query == null)
            {
                // 잘못된 조회 객체 생성 요청을 호출자에게 알림
                throw new ArgumentNullException(nameof(query));
            }

            // Cell 상태 변경에 사용할 Grid 저장
            _gridSystem = gridSystem;
            // 설치 판단과 위치 계산에 사용할 조회 객체 저장
            _query = query;
        }

        // Prefab을 생성하고 요청 영역의 Cell을 설치 상태로 변경
        internal bool TryPlacePrefab(
            GameObject prefab,
            CellState state,
            int row,
            int col,
            int rowCount,
            int columnCount,
            out long placementId,
            out GameObject instance)
        {
            // 설치 실패에서 사용할 빈 식별자
            placementId = 0L;
            // 설치 실패에서 사용할 빈 인스턴스
            instance = null;

            // Prefab과 점유 상태와 영역을 설치 전에 검증
            if (prefab == null
                || !IsOccupiedState(state)
                || !_query.CanPlace(row, col, rowCount, columnCount)
                || _nextPlacementId == long.MaxValue)
            {
                // 잘못된 설치 요청 실패 반환
                return false;
            }

            // Prefab을 생성할 첫 Cell의 월드 위치
            Vector3 worldPosition = _query.GetWorldPosition(row, col);
            // Grid 위치에 생성한 Prefab 인스턴스
            GameObject createdInstance = UnityEngine.Object.Instantiate(prefab, worldPosition, Quaternion.identity);

            // Unity 생성 실패를 차단
            if (createdInstance == null)
            {
                // Prefab 생성 실패 반환
                return false;
            }

            // 요청 상태와 영역을 설치 장부 형식으로 구성
            PlacementRecord record = new PlacementRecord(
                state,
                row,
                col,
                rowCount,
                columnCount,
                createdInstance);

            // 모든 Cell을 같은 설치 정보로 변경
            if (!TryClaimCells(record, createdInstance))
            {
                // Cell 변경에 실패한 인스턴스 제거
                UnityEngine.Object.Destroy(createdInstance);
                // Cell 변경 실패 반환
                return false;
            }

            // 성공한 설치에 부여할 식별자
            long createdPlacementId = _nextPlacementId;
            _nextPlacementId += 1L;
            _placements.Add(createdPlacementId, record);
            placementId = createdPlacementId;
            instance = createdInstance;

            // Prefab 생성과 Cell 변경 성공 반환
            return true;
        }

        // Defense Prefab을 생성하고 파괴 알림을 설치 해제와 연결
        internal bool TryPlaceDefense(
            DefenseBase prefab,
            int row,
            int col,
            int rowCount,
            int columnCount,
            out long placementId,
            out DefenseBase instance)
        {
            // 설치 실패에서 사용할 빈 식별자
            placementId = 0L;
            // 설치 실패에서 사용할 빈 Defense 참조
            instance = null;

            // Defense Prefab 누락 차단
            if (prefab == null)
            {
                // Prefab 누락 설치 실패 반환
                return false;
            }

            // 공통 Prefab 설치 결과를 받을 GameObject
            GameObject createdObject;

            // Defense 상태로 Prefab 생성과 Cell 점유 요청
            if (!TryPlacePrefab(
                prefab.gameObject,
                CellState.Def,
                row,
                col,
                rowCount,
                columnCount,
                out placementId,
                out createdObject))
            {
                // Defense 설치 실패 반환
                return false;
            }

            // 생성된 Prefab에서 구체 Defense Component 확인
            DefenseBase createdDefense = createdObject.GetComponent<DefenseBase>();

            // 잘못 구성된 Prefab은 설치와 인스턴스를 함께 정리
            if (createdDefense == null)
            {
                TryReleasePlacement(placementId);
                UnityEngine.Object.Destroy(createdObject);
                placementId = 0L;

                // Defense Component 누락 실패 반환
                return false;
            }

            _defensePlacements.Add(createdDefense, placementId);
            createdDefense.Destroyed += HandleDefenseDestroyed;
            instance = createdDefense;

            // Defense 생성과 파괴 알림 연결 성공 반환
            return true;
        }

        // Scene에 미리 존재하는 객체를 설치 장부와 Grid에 등록
        internal bool TryRegisterExistingObject(
            GameObject instance,
            CellState state,
            int row,
            int col,
            int rowCount,
            int columnCount,
            out long placementId)
        {
            // 등록 실패에서 사용할 빈 식별자
            placementId = 0L;

            // 객체와 점유 상태와 영역을 등록 전에 검증
            if (instance == null
                || !IsOccupiedState(state)
                || !_query.CanPlace(row, col, rowCount, columnCount)
                || _nextPlacementId == long.MaxValue)
            {
                // 잘못된 기존 객체 등록 실패 반환
                return false;
            }

            // 요청 상태와 영역을 설치 장부 형식으로 구성
            PlacementRecord record = new PlacementRecord(
                state,
                row,
                col,
                rowCount,
                columnCount,
                instance);

            // 모든 Cell을 같은 설치 정보로 변경
            if (!TryClaimCells(record, instance))
            {
                // 기존 객체 Cell 변경 실패 반환
                return false;
            }

            // 성공한 등록에 부여할 식별자
            long createdPlacementId = _nextPlacementId;
            _nextPlacementId += 1L;
            _placements.Add(createdPlacementId, record);
            placementId = createdPlacementId;

            // 기존 객체 등록과 Cell 변경 성공 반환
            return true;
        }

        // 명시적으로 전달된 설치 식별자의 Cell을 비움
        internal bool TryReleasePlacement(long placementId)
        {
            // 설치 식별자에 해당하는 점유 기록
            PlacementRecord record;

            // 등록되지 않은 설치 식별자 차단
            if (!_placements.TryGetValue(placementId, out record))
            {
                // 찾을 수 없는 설치 해제 실패 반환
                return false;
            }

            // 설치가 아직 소유한 Cell만 None으로 복구
            ClearCells(record);
            // 해제된 설치 기록 제거
            _placements.Remove(placementId);

            // Defense 설치였다면 파괴 알림 연결과 장부 제거
            DefenseBase defense = record.Instance != null
                ? record.Instance.GetComponent<DefenseBase>()
                : null;

            // 등록된 Defense만 파괴 알림에서 분리
            if (defense != null && _defensePlacements.Remove(defense))
            {
                defense.Destroyed -= HandleDefenseDestroyed;
            }

            // 명시적으로 요청된 설치 해제 성공 반환
            return true;
        }

        // Defense 파괴 알림으로 연결된 설치 점유 해제
        private void HandleDefenseDestroyed(DefenseBase defense)
        {
            // 등록되지 않은 Defense 파괴 알림 차단
            if (defense == null || !_defensePlacements.TryGetValue(defense, out long placementId))
            {
                // 해제할 설치가 없는 알림 종료
                return;
            }

            _defensePlacements.Remove(defense);
            defense.Destroyed -= HandleDefenseDestroyed;

            // 연결된 설치 식별자의 Cell 점유 해제
            TryReleasePlacement(placementId);
        }

        // None이 아닌 정의된 Cell 상태인지 확인
        private static bool IsOccupiedState(CellState state)
        {
            // 실제 설치에 사용할 수 있는 상태 결과 반환
            return state != CellState.None && Enum.IsDefined(typeof(CellState), state);
        }

        // 설치 영역의 모든 Cell을 요청 상태로 변경
        private bool TryClaimCells(PlacementRecord record, GameObject instance)
        {
            // 영역의 모든 세로 Cell 순회
            for (int rowOffset = 0; rowOffset < record.RowCount; rowOffset += 1)
            {
                // 현재 세로 위치의 모든 가로 Cell 순회
                for (int colOffset = 0; colOffset < record.ColumnCount; colOffset += 1)
                {
                    // 한 Cell이라도 바꾸지 못하면 이번 설치 전체 복구
                    if (!_gridSystem.UpdateCellState(
                        record.Row + rowOffset,
                        record.Col + colOffset,
                        record.State,
                        instance))
                    {
                        // 같은 설치가 이미 변경한 Cell만 None으로 복구
                        ClearCells(record);
                        // Cell 점유 실패 반환
                        return false;
                    }
                }
            }

            // 전체 Cell 점유 성공 반환
            return true;
        }

        // 설치 기록과 일치하는 Cell을 None으로 복구
        private void ClearCells(PlacementRecord record)
        {
            // 영역의 모든 세로 Cell 순회
            for (int rowOffset = 0; rowOffset < record.RowCount; rowOffset += 1)
            {
                // 현재 세로 위치의 모든 가로 Cell 순회
                for (int colOffset = 0; colOffset < record.ColumnCount; colOffset += 1)
                {
                    // 현재 Cell 상태를 받을 값
                    CellState state;
                    // 현재 비울 세로 좌표
                    int currentRow = record.Row + rowOffset;
                    // 현재 비울 가로 좌표
                    int currentCol = record.Col + colOffset;

                    // 같은 설치 상태가 아닌 Cell은 유지
                    if (!_gridSystem.TryGetCellState(currentRow, currentCol, out state)
                        || state != record.State
                        || record.State == CellState.Def
                        && _gridSystem.GetDefenseObject(currentRow, currentCol) != record.Instance)
                    {
                        // 다음 Cell 확인
                        continue;
                    }

                    // 이번 설치가 사용한 Cell을 빈 상태로 복구
                    _gridSystem.UpdateCellState(currentRow, currentCol, CellState.None);
                }
            }
        }

        private sealed class PlacementRecord
        {
            // Grid에 기록한 설치 상태
            public CellState State { get; }

            // 설치 영역의 첫 세로 좌표
            public int Row { get; }

            // 설치 영역의 첫 가로 좌표
            public int Col { get; }

            // 설치 영역의 세로 Cell 수
            public int RowCount { get; }

            // 설치 영역의 가로 Cell 수
            public int ColumnCount { get; }

            // Grid Cell과 연결된 설치 객체
            public GameObject Instance { get; }

            // Cell 상태와 점유 영역 기록 생성
            public PlacementRecord(
                CellState state,
                int row,
                int col,
                int rowCount,
                int columnCount,
                GameObject instance)
            {
                // Grid에 기록한 상태 저장
                State = state;
                // 설치 영역의 첫 세로 좌표 저장
                Row = row;
                // 설치 영역의 첫 가로 좌표 저장
                Col = col;
                // 설치 영역의 세로 Cell 수 저장
                RowCount = rowCount;
                // 설치 영역의 가로 Cell 수 저장
                ColumnCount = columnCount;
                // Grid Cell과 연결된 설치 객체 저장
                Instance = instance;
            }
        }
    }
}
