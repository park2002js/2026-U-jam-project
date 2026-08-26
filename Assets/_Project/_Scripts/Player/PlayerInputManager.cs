using UnityEngine;
using UJam.Runtime.Phase;

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

            // 마우스 '우'클릭 시 Shooting 발동
            if (Input.GetMouseButtonDown(1))
            {
                _playerCombatSystem.DefaultAttack();
            }

            // 키보드 'D' 키를 누를 시 1번 슬롯의 스킬 발동
            if (Input.GetKeyDown(KeyCode.D))
            {
                _playerCombatSystem.Skill1();
            }

            // 키보드 'F' 키를 누를 시 2번 슬롯의 스킬 발동
            if (Input.GetKeyDown(KeyCode.F))
            {
                _playerCombatSystem.Skill2();
            }
        }
    }
}
