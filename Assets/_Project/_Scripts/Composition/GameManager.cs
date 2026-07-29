using UnityEngine;
using UJam.Integration.UI;
using UJam.Runtime.Defense;
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
        // 아래 Grid 설정값은 GridPreview에도 따로 존재함
        // 이 값을 변경하면 Scene 미리보기와 달라지므로 GridPreview의 같은 값도 함께 변경

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

        // Phase 시스템에 전달할 Wave 제어기
        [SerializeField] private WaveController _waveController;

        // 발표용 Enemy 기본 Target으로 주입할 거점
        [SerializeField] private BaseCore _baseCore;

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

            // 발표용 Enemy 기본 Target으로 거점 객체 연결
            if (_waveController != null && _baseCore != null)
            {
                _waveController.ConfigureDefaultTarget(_baseCore.gameObject);
            }

            // Grid 준비 뒤 Phase와 Wave 시스템 연결
            _phaseSystem.Initialize(_waveController);

            // Player에 전달할 설치 시스템
            PlacementSystem placementSystem = new PlacementSystem(gridSystem);

            // Player 상태가 연결된 경우에만 Phase 전달
            if (_playerStatus != null)
            {
                _playerStatus.ConfigurePhaseSystem(_phaseSystem);
            }

            // 설치 입력이 연결된 경우에만 설치 시스템 전달
            if (_playerPlacement != null)
            {
                _playerPlacement.Configure(placementSystem);
            }

            // UI가 연결된 경우에만 조회 대상 전달
            if (_runtimeUiStateBridge != null)
            {
                _runtimeUiStateBridge.Configure(_phaseSystem, _wallet, _playerStatus);
            }
        }
    }
}
