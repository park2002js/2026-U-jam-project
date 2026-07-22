using UnityEngine;
using UJam.Runtime.Defense;
using UJam.Runtime.Placement;

namespace UJam.Runtime.Player
{
    public sealed class PlayerPlacement : MonoBehaviour
    {
        // 일반 플레이에 사용할 1인칭 카메라
        [SerializeField] private Camera _firstPersonCamera;

        // 건물 설치에 사용할 Top View 카메라
        [SerializeField] private Camera _topViewCamera;

        // GameManager에서 주입할 설치 시스템
        private PlacementSystem _placementSystem;

        // 현재 설치할 Defense Prefab
        private DefenseBase _selectedPrefab;

        // 현재 Prefab의 세로 Cell 수
        private int _rowCount;

        // 현재 Prefab의 가로 Cell 수
        private int _columnCount;

        // 현재 Pointer가 가리키는 세로 Cell
        private int _targetRow;

        // 현재 Pointer가 가리키는 가로 Cell
        private int _targetCol;

        // 유효한 대상 Cell을 받은 상태
        private bool _hasTargetCell;

        // 현재 설치 모드 여부
        public bool IsPlacing
        {
            get
            {
                // 현재 Prefab 선택 상태 반환
                return _selectedPrefab != null;
            }
        }

        // 런타임 설치 시스템 주입
        public void Configure(PlacementSystem placementSystem)
        {
            // GameManager가 만든 단일 설치 시스템 저장
            _placementSystem = placementSystem;
        }

        // Defense Prefab 선택과 Top View 전환 시작
        public bool TryBegin(DefenseBase prefab, int rowCount, int columnCount)
        {
            // 필수 시스템과 Prefab과 크기와 중복 시작 확인
            if (_placementSystem == null
                || prefab == null
                || rowCount <= 0
                || columnCount <= 0
                || IsPlacing)
            {
                // 설치 시작 실패 반환
                return false;
            }

            // 선택한 설치 정보 저장
            _selectedPrefab = prefab;
            _rowCount = rowCount;
            _columnCount = columnCount;
            _hasTargetCell = false;

            // 설치용 Top View로 전환
            SetPlacementView(true);

            // 설치 시작 성공 반환
            return true;
        }

        // Pointer가 가리키는 Grid Cell 갱신
        public bool SetTargetCell(int row, int col)
        {
            // 설치 중인 상태와 음수가 아닌 좌표 확인
            if (!IsPlacing || row < 0 || col < 0)
            {
                // 대상 Cell 갱신 실패 반환
                return false;
            }

            // 현재 설치 대상 Cell 저장
            _targetRow = row;
            _targetCol = col;
            _hasTargetCell = true;

            // 대상 Cell 갱신 성공 반환
            return true;
        }

        // 현재 Cell에 Defense 설치 확정
        public bool TryConfirm()
        {
            // 설치 결과 식별자 보관
            long placementId;
            // 생성된 Defense 보관
            DefenseBase instance;

            // 선택과 대상 Cell과 시스템 준비 여부 확인
            if (!IsPlacing || !_hasTargetCell || _placementSystem == null)
            {
                // 설치 확정 실패 반환
                return false;
            }

            // 기존 PlacementSystem에 실제 Defense 설치 요청
            if (!_placementSystem.TryPlaceDefense(
                _selectedPrefab,
                _targetRow,
                _targetCol,
                _rowCount,
                _columnCount,
                out placementId,
                out instance))
            {
                // 설치 가능 상태를 유지한 실패 반환
                return false;
            }

            // 성공한 설치 뒤 선택과 시점 정리
            FinishPlacement();

            // 설치 확정 성공 반환
            return true;
        }

        // 현재 설치를 취소하고 1인칭 시점 복귀
        public void Cancel()
        {
            // 설치 중이 아니면 변경 없이 종료
            if (!IsPlacing)
            {
                // 취소할 설치 없이 종료
                return;
            }

            // 선택과 시점 정리
            FinishPlacement();
        }

        // Component 비활성화에서 남은 설치 상태 정리
        private void OnDisable()
        {
            // 진행 중인 설치와 시점 복구
            Cancel();
        }

        // 설치 선택 정보와 카메라 상태 초기화
        private void FinishPlacement()
        {
            // 현재 설치 정보 비우기
            _selectedPrefab = null;
            _rowCount = 0;
            _columnCount = 0;
            _hasTargetCell = false;

            // 일반 1인칭 시점으로 복귀
            SetPlacementView(false);
        }

        // 설치 여부에 따라 두 카메라와 Cursor 상태 변경
        private void SetPlacementView(bool placing)
        {
            // 1인칭 카메라가 있을 때 반대 상태 적용
            if (_firstPersonCamera != null)
            {
                _firstPersonCamera.gameObject.SetActive(!placing);
            }

            // Top View 카메라가 있을 때 설치 상태 적용
            if (_topViewCamera != null)
            {
                _topViewCamera.gameObject.SetActive(placing);
            }

            // 설치 중 Cursor 표시 여부 적용
            Cursor.visible = placing;
            // 설치 중 Cursor 잠금 해제 적용
            Cursor.lockState = placing ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
