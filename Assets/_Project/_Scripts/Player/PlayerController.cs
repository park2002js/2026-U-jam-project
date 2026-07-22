using UnityEngine;
using UJam.Runtime.Defense;

namespace UJam.Runtime.Player
{
    public sealed class PlayerController : MonoBehaviour
    {
        // Player 입력 담당 Component
        [SerializeField] private PlayerInput _input;

        // Player 사격 담당 Component
        [SerializeField] private PlayerShooter _shooter;

        // Player 스킬 담당 Component
        [SerializeField] private PlayerSkills _skills;

        // Player 아이템 담당 Component
        [SerializeField] private PlayerInventory _inventory;

        // Player 설치 담당 Component
        [SerializeField] private PlayerPlacement _placement;

        // Player 스탯과 행동 권한 담당 Component
        [SerializeField] private PlayerStatus _status;

        // 외부 기능이 접근할 Player 아이템 보관소
        public PlayerInventory Inventory
        {
            get
            {
                // 현재 연결된 아이템 보관소 반환
                return _inventory;
            }
        }

        // 같은 GameObject의 Player Component 연결 보완
        private void Awake()
        {
            // 누락된 입력 Component 확인
            if (_input == null)
            {
                _input = GetComponent<PlayerInput>();
            }

            // 누락된 사격 Component 확인
            if (_shooter == null)
            {
                _shooter = GetComponent<PlayerShooter>();
            }

            // 누락된 스킬 Component 확인
            if (_skills == null)
            {
                _skills = GetComponent<PlayerSkills>();
            }

            // 누락된 아이템 Component 확인
            if (_inventory == null)
            {
                _inventory = GetComponent<PlayerInventory>();
            }

            // 누락된 설치 Component 확인
            if (_placement == null)
            {
                _placement = GetComponent<PlayerPlacement>();
            }

            // 누락된 상태 Component 확인
            if (_status == null)
            {
                _status = GetComponent<PlayerStatus>();
            }
        }

        // 입력과 상태 이벤트 연결
        private void OnEnable()
        {
            // 입력 요청 callback 연결
            if (_input != null)
            {
                _input.AttackRequested += HandleAttackRequested;
                _input.SkillRequested += HandleSkillRequested;
                _input.PlacementConfirmRequested += HandlePlacementConfirmRequested;
                _input.PlacementCancelRequested += HandlePlacementCancelRequested;
            }

            // Phase 기반 권한 변경 callback 연결
            if (_status != null)
            {
                _status.InputAvailabilityChanged += RefreshInputAvailability;
            }

            // 현재 Phase 권한 즉시 반영
            RefreshInputAvailability();
        }

        // 입력과 상태 이벤트 해제
        private void OnDisable()
        {
            // 입력 요청 callback 해제
            if (_input != null)
            {
                _input.AttackRequested -= HandleAttackRequested;
                _input.SkillRequested -= HandleSkillRequested;
                _input.PlacementConfirmRequested -= HandlePlacementConfirmRequested;
                _input.PlacementCancelRequested -= HandlePlacementCancelRequested;
            }

            // Phase 기반 권한 변경 callback 해제
            if (_status != null)
            {
                _status.InputAvailabilityChanged -= RefreshInputAvailability;
            }
        }

        // 상점이나 UI에서 설치 모드 시작 요청
        public bool TryBeginPlacement(DefenseBase prefab, int rowCount, int columnCount)
        {
            // 정비 Phase와 설치 Component 준비 여부 확인
            if (_status == null || !_status.CanPlace || _placement == null)
            {
                // 설치 시작 실패 반환
                return false;
            }

            // 실제 설치 담당 Component에 시작 요청 전달
            return _placement.TryBegin(prefab, rowCount, columnCount);
        }

        // 외부 Pointer 코드에서 현재 설치 대상 Cell 전달
        public bool SetPlacementTargetCell(int row, int col)
        {
            // 설치 Component 준비 여부 확인
            if (_placement == null)
            {
                // Cell 전달 실패 반환
                return false;
            }

            // 실제 설치 담당 Component에 Cell 전달
            return _placement.SetTargetCell(row, col);
        }

        // Phase에 맞춰 실제 Input Action 상태 변경
        private void RefreshInputAvailability()
        {
            // 입력 Component 누락 시 변경 없이 종료
            if (_input == null)
            {
                // 입력 상태 변경 없이 종료
                return;
            }

            // 현재 상태에서 전투 입력 가능 여부 계산
            bool combatEnabled = _status != null && _status.CanAttack;
            // 현재 상태에서 설치 입력 가능 여부 계산
            bool placementEnabled = _status != null && _status.CanPlace;
            _input.SetCombatInputEnabled(combatEnabled);
            _input.SetPlacementInputEnabled(placementEnabled);

            // 설치 도중 정비 Phase가 끝나면 시점과 설치 상태 복구
            if (!placementEnabled && _placement != null && _placement.IsPlacing)
            {
                _placement.Cancel();
            }
        }

        // 발사 요청을 Shooter에 전달
        private void HandleAttackRequested()
        {
            // 공격 권한과 필수 Component 확인
            if (_status == null || !_status.CanAttack || _shooter == null)
            {
                // 발사 전달 없이 종료
                return;
            }

            // 현재 Player 공격력으로 한 발 발사
            _shooter.TryShoot(_status.AttackDamage);
        }

        // 스킬 요청을 슬롯 담당에 전달
        private void HandleSkillRequested(int slot)
        {
            // 스킬 권한과 필수 Component 확인
            if (_status == null || !_status.CanUseSkills || _skills == null)
            {
                // 스킬 전달 없이 종료
                return;
            }

            // 요청한 슬롯의 스킬 사용 시도
            _skills.TryUse(slot);
        }

        // 설치 확정 요청을 Placement에 전달
        private void HandlePlacementConfirmRequested()
        {
            // 설치 권한과 진행 상태 확인
            if (_status == null || !_status.CanPlace || _placement == null)
            {
                // 설치 확정 전달 없이 종료
                return;
            }

            // 현재 Cell에 설치 확정 시도
            _placement.TryConfirm();
        }

        // 설치 취소 요청을 Placement에 전달
        private void HandlePlacementCancelRequested()
        {
            // 설치 Component가 있을 때만 취소 전달
            if (_placement != null)
            {
                _placement.Cancel();
            }
        }
    }
}
