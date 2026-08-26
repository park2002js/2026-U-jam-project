using System;
using UnityEngine;

namespace UJam.Runtime.Phase
{
    // 현재 게임 진행 구간
    public enum PhaseState
    {
        // GameManager가 아직 최초 Phase를 시작하지 않은 상태
        None = -1,

        // 전투 전 정비 구간
        Preparation,

        // Enemy Wave가 진행되는 전투 구간
        Combat
    }

    public class PhaseSystem : MonoBehaviour
    {
        // 현재 Phase 상태
        private PhaseState _currentState = PhaseState.None;

        // WaveController가 마지막으로 보고한 생존 Enemy 수
        private int _remainingEnemyCount;

        // WaveController가 마지막으로 보고한 현재 Wave 사망 Enemy 수
        private int _deadEnemyCount;

        // Wave 제어기
        private WaveController _waveController;

        // 초기화와 적 사망으로 갱신된 남은 수를 UIRemainingEnemyCount에 전달한다.
        public event Action<int> OnRemainingEnemyCountChanged;

        // 외부 시스템이 조회할 현재 Phase
        public PhaseState CurrentState => _currentState;

        // 남은 Enemy UI가 조회할 최신 수치
        public int RemainingEnemyCount => _remainingEnemyCount;

        // 현재 Wave에서 죽은 Enemy 수
        public int DeadEnemyCount => _deadEnemyCount;

        /// <summary>
        /// GameManager가 WaveController를 연결합니다. 초기화 자체는 Phase를 시작하거나 이벤트를 보내지 않습니다.
        /// </summary>
        public bool Initialize(WaveController waveController)
        {
            if (waveController == null || _waveController != null)
            {
                Debug.LogWarning("[PhaseSystem] WaveController가 없거나 이미 초기화되었습니다.", this);
                return false;
            }

            _waveController = waveController;
            _waveController.ConfigurePhaseSystem(this);
            return true;
        }

        /// <summary>
        /// 최초 게임 준비 또는 전투 종료 후 정비 상태와 카운터를 설정하고 이벤트를 알립니다.
        /// </summary>
        internal void StartPreparationPhase()
        {
            if (_waveController == null || _currentState == PhaseState.Preparation || _remainingEnemyCount > 0) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            _deadEnemyCount = 0;
            UpdateRemainingEnemyCount(0);
            ChangePhase(PhaseState.Preparation);
        }

        /// <summary>
        /// UIManager → GameManager의 시작 요청을 검증하고 Wave 시작에 성공한 경우에만 전투로 전환합니다.
        /// </summary>
        public void StartCombatPhase()
        {
            if (_waveController == null || _currentState != PhaseState.Preparation) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            // 다음 Wave를 시작할 수 있는지 확인
            if (!_waveController.StartNextWave())
            {
                // WaveController 세부 오류 확인 안내
                Debug.LogWarning("[PhaseSystem] 전투 시작 실패: WaveController가 다음 Wave를 시작하지 못함", this);

                // 잘못된 Wave의 Phase 변경 차단
                return;
            }

            ChangePhase(PhaseState.Combat);
        }

        /// <summary>
        /// 초기화 또는 WaveController가 전달한 남은 적 수를 저장하고 매번 UIRemainingEnemyCount에 알립니다.
        /// </summary>
        internal void UpdateRemainingEnemyCount(int remainingEnemyCount)
        {
            _remainingEnemyCount = Mathf.Max(0, remainingEnemyCount);
            OnRemainingEnemyCountChanged?.Invoke(_remainingEnemyCount);
        }

        // WaveController가 전달한 현재 Wave 사망 Enemy 수 저장
        internal void UpdateDeadEnemyCount(int deadEnemyCount)
        {
            // 음수가 아닌 죽은 Enemy 수 저장
            _deadEnemyCount = Mathf.Max(0, deadEnemyCount);
        }

        /// <summary>
        /// WaveController의 완료 보고를 받아 생존 적이 없는 전투를 정비 Phase로 전환합니다.
        /// </summary>
        internal void CompleteCombatPhase()
        {
            if (_remainingEnemyCount > 0 || (GameManager.Instance != null && GameManager.Instance.IsGameOver)) return;
            if (_currentState == PhaseState.Combat) StartPreparationPhase();
        }

        /// <summary>
        /// Phase 상태를 확정하고 GameManager에 변경을 전달합니다. UI 표시 판단은 UIManager가 담당합니다.
        /// </summary>
        private void ChangePhase(PhaseState nextState)
        {
            // 똑같은 상태를 다시 요청했다면 반환
            if (_currentState == nextState) return;

            _currentState = nextState;

            GameManager.Instance?.HandlePhaseChanged(nextState);
        }
    }
}
