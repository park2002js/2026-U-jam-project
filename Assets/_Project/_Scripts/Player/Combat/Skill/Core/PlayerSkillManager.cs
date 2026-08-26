using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// Skill을 슬롯 형태로 다루며, 장착과 해제, Skill 시전을 위한 함수 호출을 제공한다.
    /// 
    /// Skill의 일반 시전이냐 즉시 시전이냐에 따라 달라지는 Preview 설정도 처리한다.
    /// </summary>
    public class PlayerSkillManager : MonoBehaviour
    {

        [Header("디버깅")]
        [SerializeField] [Tooltip("테스트를 위해 사용할 기본 스킬")]
        private PlayerSkill defulatPlayerSkill;

        private PlayerCombatManager _combatManager;

        // 스킬 슬롯의 역할을 할 리스트
        private List<PlayerSkill> _slots = new List<PlayerSkill> { null, null };

        // 쿨타임 동안 스킬 사용을 막기 위한 별도의 배열
        private readonly float[] _cooldownEndTimes = new float[2];

        // 장착 변경 시 슬롯 번호와 스킬을 전달한다. 해제된 슬롯은 null을 전달한다.
        public event Action<int, PlayerSkill> OnSkillChanged;

        // 실제 사용한 슬롯 번호와 스킬을 전달한다. 구독자는 Skill.CoolTime으로 쿨타임을 표시한다.
        public event Action<int, PlayerSkill> OnSkillUsed;

        /// <summary>
        /// UISkillSlot이 담당 슬롯에 장착된 스킬과 아이콘을 조회합니다.
        /// </summary>
        public PlayerSkill GetSkill(int slot) => (slot >= 0 && slot < _slots.Count ? _slots[slot] : null);

        /// <summary>
        /// UISkillSlot이 다시 활성화될 때도 표시를 복원하도록 마지막 시전의 쿨타임 종료 시각을 제공합니다.
        /// </summary>
        public float GetCooldownEndTime(int slot) => (slot >= 0 && slot < _cooldownEndTimes.Length ? _cooldownEndTimes[slot] : 0f);

        /// <summary>
        /// PlayerCombatManager에 의해 초기화 됨과 동시에 PlayerCombatManager의 객체를 받아 
        /// </summary>
        public void Init(PlayerCombatManager combatManager)
        {
            _combatManager = combatManager;

            // 테스팅을 위한 임시 할당
            if(defulatPlayerSkill != null) Equip(0, defulatPlayerSkill);
        }

        // 현재 시전 중인 스킬을 구분하기 위해 사용하는 변수
        private PlayerSkill currentSkill;
        private int currentSkillSlot = -1;

        // 원본 Prefab이 아닌, 현재 시전을 위해 생성한 Preview 인스턴스
        private PlayerSkillPreview currentSkillPreview;

        // 일반 스킬이 현재 시전중인지 여부를 나타내는 bool 변수
        private bool isNormalSkillCasting;

        // 현재 추적중인 마우스 위치를 저장하는 변수
        private Vector3 currentTargetPosition;

        
        /// <summary>
        /// 스킬의 사용을 외부에서 명령할 수 있도록 하는 함수
        /// 슬롯 번호를 지정하면, 그 슬롯에 할당된 Skill을 사용한다.
        /// 쿨타임 중인 슬롯의 요청은 기존 스킬 프리뷰를 변경하지 않고 무시한다.
        /// 
        /// 스킬 시전시 그 스킬의 시전 타입에 맞춰 동작을 달리한다.
        /// </summary>
        public void TryUse(int slot)
        {
            if (slot < 0 || slot >= _slots.Count || _slots[slot] == null)
            {
                Debug.LogError("[PlayerSkillManager] 스킬 시전을 위해 지정한 슬롯이 유효하지 않습니다.");
                return;
            }
            // 해당 슬롯의 쿨타임이 아직 끝나지 않았으면 스킬 시전 취소
            if (Time.time < _cooldownEndTimes[slot]) return;

            var skill = _slots[slot];
            var _skill = currentSkill; // 스킬 시전 종료시 이전 스킬 정보가 사라지므로 미리 저장

            // 기존에 실행중인 스킬 (아직 마우스 우클릭으로 실행을 결정하지 않은 일반 스킬)이 있다면 시전 종료시키기
            CancelCurrentSkill();

            // 만약 시전하려던 스킬이 이전의 스킬과 동일하다면, 현재 스킬 보이기를 종료시키고 여기서 함수 종료.
            if(_skill == skill) return; 
            
            // 스킬이 즉시시전이면, 즉시 시전함
            if(skill.CastType == SkillCastType.Instant)
            {
                skill.Excute(Vector3.zero); // 어짜피 즉시시전은 마우스의 위치를 필요로 하지 않으므로 쓸모없는 값을 할당
                NotifySkillExecuted(slot, skill);
                return;
            }
            
            // 스킬이 일반 시전이면, Preview를 먼저 띄우고 Update 내에서 마우스의 움직임을 추적하도록 함
            if(skill.CastType == SkillCastType.Normal)
            {
                // 다른 스킬 시전이 들어왔는지 확인하기 위해서 현재의 스킬을 임시로 저장함
                currentSkill = skill;
                currentSkillSlot = slot;

                // isNormalSkillCasting이 활성화 되었으므로 Update 내부에서 마우스 커서의 위치를 추적하기 시작함
                isNormalSkillCasting = true;

                // Prefab Asset은 절대 직접 조작하지 않고, 생성한 인스턴스만 조작한다.
                GameObject previewPrefab = _combatManager.PlayerSkillPreviewPrefab;
                if (previewPrefab == null)
                {
                    Debug.LogError("[PlayerSkillManager] PlayerSkillPreviewPrefab이 할당되지 않았습니다.");
                    CancelCurrentSkill();
                    return;
                }

                GameObject previewInstance = Instantiate(previewPrefab);
                currentSkillPreview = previewInstance.GetComponent<PlayerSkillPreview>();
                if (currentSkillPreview == null)
                {
                    Debug.LogError("[PlayerSkillManager] Preview Prefab에 PlayerSkillPreview 컴포넌트가 없습니다.", previewInstance);
                    Destroy(previewInstance);
                    CancelCurrentSkill();
                    return;
                }

                // 유효한 Ground 위치를 찾을 때까지는 표시하지 않는다.
                currentSkillPreview.Hide();
            }
        }

        /// <summary>
        /// 일반 스킬 시전시, 마우스 커서 위치를 계속 추적하며 Preview를 갱신한다.
        /// 일반 스킬 시전 중에 마우스 좌클릭을 할 경우, 그 위치에 Skill을 수행하도록 한다.
        /// </summary>
        private void Update()
        {
            // 일반 스킬 시전 중이 아니면 굳이 추적할 필요가 없으므로 종료
            if(!isNormalSkillCasting) return;

            // CombatManager에 할당되어 있는 카메라 (없으면 메인 카메라)를 이용해서 마우스 커서 위치로 Ray를 쏨
            // 마우스 커서가 위치한 Ground의 좌표를 얻어내어 Preview의 좌표를 계속해서 수정함
            Camera cam = (_combatManager.AimCamera != null ? _combatManager.AimCamera : Camera.main);
            if (cam == null || Mouse.current == null)
            {
                Debug.LogError("[PlayerSkillManager] 플레이어의 카메라와 마우스를 찾을 수가 없습니다.");
                currentSkillPreview.Hide();
                return;
            }

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, _combatManager.GroundMask))
            {
                Debug.Log("[PlayerSkillManager] 마우스가 Ground 위에 존재하지 않습니다.");
                currentSkillPreview.Hide();
                return;
            }
            currentTargetPosition = hit.point; // 마우스가 맞춘 지점을 그대로 스킬 시전 위치로 사용
            
            currentSkillPreview.SetPosition(currentTargetPosition, hit.normal); // Preview의 좌표 수정
            currentSkillPreview.Show(currentSkill.EffectRadius);    // 위의 조건문에 의해 hide가 된 경우일 수도 있으므로 Show로 모습을 띄움

            // Preview가 정상적으로 나오므로 마우스 좌클릭을 감시하여, 실제로 클릭하였다면 스킬을 실행하라고 명령
            if(Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlayerSkill skill = currentSkill;
                int slot = currentSkillSlot;
                skill.Excute(currentTargetPosition);
                NotifySkillExecuted(slot, skill);
                
                CancelCurrentSkill(); // Preview 보이기와, 변수 초기화
            }

        }

        /// <summary>
        /// 일반 스킬의 Preview를 종료시키고, 변수들을 초기화 한다.
        /// </summary>
        private void CancelCurrentSkill()
        {
            // 일반 스킬 시전 중이 아니면 Preview를 취소할 필요가 없으므로 종료
            if(!isNormalSkillCasting) return;
            
            // 프리뷰를 종료시키고, 나머지 변수들도 초기화
            if (currentSkillPreview != null)
            {
                currentSkillPreview.Hide();
                Destroy(currentSkillPreview.gameObject);
                currentSkillPreview = null;
            }

            currentSkill = null;
            currentSkillSlot = -1;
            isNormalSkillCasting = false;
        }

        /// <summary>
        /// 실제 시전한 Skill.CoolTime으로 재사용을 차단하고, OnSkillUsed로 UISkillSlot에 사용한 스킬을 전달합니다.
        /// </summary>
        private void NotifySkillExecuted(int slot, PlayerSkill skill)
        {
            if (slot < 0 || slot >= _slots.Count || skill == null || _slots[slot] != skill) return;

            float coolTime = skill.CoolTime;
            if (float.IsNaN(coolTime) || float.IsInfinity(coolTime)) coolTime = 0f;
            _cooldownEndTimes[slot] = Time.time + Mathf.Max(0f, coolTime);

            // 스킬 사용을 알림
            OnSkillUsed?.Invoke(slot, skill);
        }

        /// <summary>
        /// PlayerSkillManager가 비활성화되면 진행 중인 스킬 프리뷰를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            CancelCurrentSkill();
        }


        /// <summary>
        /// 스킬 장착을 명령하는 함수.
        /// 장착시에는 그 스킬을 초기화 한 뒤에 장착한다.
        /// 만약 새로 장착하려는 스킬 칸에 이미 다른 스킬이 있는 경우, 장착 해제를 한 뒤에 장착한다.
        /// 
        /// <para> 스킬 장착시의 이벤트 알림을 정의하여, 스킬 슬롯의 이미지를 변경하는 등의 처리를 하도록 한다. </para>
        /// </summary>
        public void Equip(int slot, PlayerSkill skill)
        {
            // 만약 슬롯이 비어있지 않다면, 장착 해제를 먼저 한 뒤에 장착
            if(_slots[slot] != null) UnEquip(slot);

            // 초기화 후 장착
            skill.Init(_combatManager);
            _slots[slot] = skill;
            _cooldownEndTimes[slot] = 0f;

            // UI에 상태 변경을 하도록 전달
            OnSkillChanged?.Invoke(slot, skill);
        }

        /// <summary>
        /// 스킬 장착을 해제하고 OnSkillChanged에 null을 전달하여 UISkillSlot의 아이콘과 쿨타임 표시를 정리합니다.
        /// </summary>
        public void UnEquip(int slot)
        {
            if(_slots[slot] == null) Debug.Log("[PlayerSkillSlot] 스킬이 없는데 장착 해제를 호출함");
            _slots[slot] = null;
            _cooldownEndTimes[slot] = 0f;

            // UI에 상태 변경을 하도록 전달
            OnSkillChanged?.Invoke(slot, null);
        }
    }
}
