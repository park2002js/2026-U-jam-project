using UnityEngine;
using UJam.Integration.UI;
using UJam.Runtime.Grid;
using UJam.Runtime.Phase;
using UJam.Runtime.Placement;
using UJam.Runtime.Player;
using UJam.Runtime.Shop;
using UJam.Runtime.Item;

namespace UJam.Runtime.Composition
{
    public sealed class GameManager : MonoBehaviour
    {
        // Grid Cell 가로와 세로 크기
        [SerializeField, Min(0.0001f)] private float _gridCellSize = 1f;

        // Grid 시작 월드 좌표
        [SerializeField] private Vector3 _gridOrigin;

        // Grid 가로 Cell 수
        [SerializeField, Min(1)] private int _gridWidth = 10;

        // Grid 세로 Cell 수
        [SerializeField, Min(1)] private int _gridHeight = 10;

        // Player에 전달할 Phase 시스템
        [SerializeField] private PhaseSystem _phaseSystem;

        // Phase 상태를 받을 Player 상태
        [SerializeField] private PlayerStatus _playerStatus;

        // 설치 시스템을 받을 Player 설치 입력
        [SerializeField] private PlayerPlacement _playerPlacement;

        // UI에 전달할 단일 재화 저장소
        [SerializeField] private Wallet _wallet;

        // 선택적인 UI 읽기 경계
        [SerializeField] private RuntimeUiStateBridge _runtimeUiStateBridge;

        // 게임 시작에 필요한 최소 시스템 연결
        private void Awake()
        {
            // 프로젝트 단일 Grid 시스템
            GridSystem gridSystem = GridSystem.Instance;
            gridSystem.Initialize(
                _gridCellSize,
                _gridCellSize,
                _gridHeight,
                _gridWidth,
                _gridOrigin);

            // Player에 전달할 설치 시스템
            PlacementSystem placementSystem = new PlacementSystem(gridSystem);
            _playerStatus.ConfigurePhaseSystem(_phaseSystem);
            _playerPlacement.Configure(placementSystem);

            // UI가 연결된 경우에만 조회 대상 전달
            if (_runtimeUiStateBridge != null)
            {
                _runtimeUiStateBridge.Configure(_phaseSystem, _wallet, _playerStatus);
            }
        }
    }
}
