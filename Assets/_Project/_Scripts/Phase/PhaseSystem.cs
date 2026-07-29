using System;
using UnityEngine;

namespace UJam.Runtime.Phase
{
    // 현재 게임 진행 구간
    public enum PhaseState
    {
        // 전투 전 정비 구간
        Preparation,

        // Enemy Wave가 진행되는 전투 구간
        Combat
    }

    public sealed class PhaseSystem : MonoBehaviour
    {
        // 정비 Phase에서만 표시할 UI 최상위 객체
        [SerializeField] private GameObject _preparationUi;

        // 전투 Phase에서만 표시할 UI 최상위 객체
        [SerializeField] private GameObject _combatUi;

        // 현재 Phase 상태
        private PhaseState _currentState = PhaseState.Preparation;

        // WaveController가 마지막으로 보고한 생존 Enemy 수
        private int _remainingEnemyCount;

        // GameManager가 연결한 Wave 제어기
        private WaveController _waveController;

        // GameManager 초기화 완료 여부
        private bool _isInitialized;

        // PlayerStatus와 외부 시스템에 전달할 Phase 변경 이벤트
        public event Action<PhaseState> PhaseChanged;

        // 외부 시스템이 조회할 현재 Phase
        public PhaseState CurrentState
        {
            get
            {
                // 현재 Phase 상태 반환
                return _currentState;
            }
        }

        // 남은 Enemy UI가 조회할 최신 수치
        public int RemainingEnemyCount
        {
            get
            {
                // WaveController가 보고한 남은 수 반환
                return _remainingEnemyCount;
            }
        }

        // GameManager가 Grid 준비 뒤 Wave 제어기와 최초 정비 Phase 연결
        public void Initialize(WaveController waveController)
        {
            // Wave 제어기 누락과 중복 초기화 차단
            if (waveController == null || _isInitialized)
            {
                // 잘못된 초기화 요청 종료
                return;
            }

            _waveController = waveController;
            _remainingEnemyCount = 0;
            _waveController.ConfigurePhaseSystem(this);

            // 게임 시작 Phase를 정비로 적용
            ChangePhase(PhaseState.Preparation);
            _isInitialized = true;
        }

        // Button 클릭으로 정비에서 전투로 전환
        public void StartCombatPhase()
        {
            // 초기화 완료와 정비 상태 확인
            if (!_isInitialized || _currentState != PhaseState.Preparation)
            {
                // 시작할 수 없는 전투 요청 종료
                return;
            }

            // 다음 Wave를 시작할 수 있는지 확인
            if (!_waveController.StartNextWave())
            {
                // 잘못된 Wave의 Phase 변경 차단
                return;
            }

            ChangePhase(PhaseState.Combat);
        }

        // WaveController가 전달한 남은 Enemy 수 저장
        internal void UpdateRemainingEnemyCount(int remainingEnemyCount)
        {
            _remainingEnemyCount = Mathf.Max(0, remainingEnemyCount);
        }

        // 현재 전투 완료 보고를 받아 바로 정비로 전환
        internal void CompleteCombatPhase()
        {
            // 전투 중 완료 보고인지 확인
            if (_currentState != PhaseState.Combat)
            {
                // 잘못된 완료 보고 종료
                return;
            }

            ChangePhase(PhaseState.Preparation);
        }

        // Phase 상태와 두 UI 루트 활성화 갱신
        private void ChangePhase(PhaseState nextState)
        {
            // 이벤트 중복 여부를 확인할 이전 Phase
            PhaseState previousState = _currentState;
            _currentState = nextState;

            // 정비 UI가 연결됐는지 확인
            if (_preparationUi != null)
            {
                _preparationUi.SetActive(nextState == PhaseState.Preparation);
            }

            // 전투 UI가 연결됐는지 확인
            if (_combatUi != null)
            {
                _combatUi.SetActive(nextState == PhaseState.Combat);
            }

            // 실제 Phase가 달라졌을 때만 변경 통지
            if (previousState != nextState)
            {
                PhaseChanged?.Invoke(nextState);
            }
        }
    }
}
