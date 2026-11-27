using UnityEngine;

namespace UJam.Runtime.Player
{
    public class PlayerInputManager : MonoBehaviour
    {
        // [SerializeField] private PhaseSystem _phaseSystem;
        // [SerializeField] private PhaseState _inputPhase = PhaseState.Combat;
        [SerializeField] private PlayerCombatManager _playerCombatSystem;

        /// <summary>
        /// 매 프레임마다 Player의 입력을 추적하여, 그 입력과 연결된 기능을 호출하는 Update 부분
        /// 
        /// Canvas와의 충돌 여부를 감시해야 하며, 이후 New Input Action으로 개선할 방법도 고려해야 함
        /// </summary>
        private void Update()
        {
            // if (_phaseSystem == null || _playerCombatSystem == null || _phaseSystem.CurrentState != _inputPhase)
            // {
            //     return;
            // }

            // 설정창이 열려 게임이 멈춘 동안에는 사격·스킬 입력을 받지 않는다.
            if (Time.timeScale <= 0f || UJam.Runtime.UI.UISettingsPanel.AnyOpen) return;

            // 마우스 '우'클릭 시 Shooting 발동
            if (Input.GetMouseButtonDown(1))
            {
                // 마우스 '우'클릭 시 Shooting 발동 (스킬 조준 중에는 취소 입력으로만 쓰인다)
                if (Input.GetMouseButtonDown(1))
                {
                    if (_playerCombatSystem.SkillManager == null || !_playerCombatSystem.SkillManager.IsAiming)
                    {
                        _playerCombatSystem.DefaultAttack();
                    }
                }
            }

            // 숫자키 1~5(키패드 포함)를 누르면 각각 1~5번 스킬칸의 스킬 발동
            for (int i = 0; i < SkillKeys.Length; i++)
            {
                if (Input.GetKeyDown(SkillKeys[i]) || Input.GetKeyDown(SkillKeypadKeys[i]))
                {
                    _playerCombatSystem.UseSkill(i);
                }
            }
        }

        private static readonly KeyCode[] SkillKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };
        private static readonly KeyCode[] SkillKeypadKeys = { KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5 };
    }
}
