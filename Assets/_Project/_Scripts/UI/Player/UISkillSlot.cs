using UnityEngine;
using UnityEngine.UI;
using UJam.Runtime.Player;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// SkillSlotA/B에 부착하여 PlayerSkillManager의 해당 슬롯 아이콘과 쿨타임 표시를 관리합니다.
    /// </summary>
    public class UISkillSlot : MonoBehaviour
    {
        [SerializeField] private PlayerSkillManager _skillManager;
        [SerializeField, Min(0)] private int _slotIndex;
        [SerializeField] private Image _skillIcon;
        [SerializeField] private Color _cooldownColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private Color _normalColor = Color.white;
        private float _cooldownEndTime;
        private bool _isCoolingDown;

        /// <summary>
        /// 쿨타임이 끝났을 때 복원할 Skill_Icon의 원래 색을 보관합니다.
        /// </summary>
        private void Awake()
        {
            if (_skillIcon != null) _normalColor = _skillIcon.color;
        }

        /// <summary>
        /// PlayerSkillManager의 장착 변경과 사용 이벤트를 각각 구독하고 현재 슬롯 상태를 표시합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_skillManager != null)
            {
                _skillManager.OnSkillChanged += OnSkillChanged;
                _skillManager.OnSkillUsed += OnSkillUsed;
            }
            RefreshSlot();
        }

        /// <summary>
        /// PlayerCombatManager의 초기 스킬 장착이 완료된 뒤 최초 표시를 동기화합니다.
        /// </summary>
        private void Start()
        {
            RefreshSlot();
        }

        /// <summary>
        /// UI가 닫히거나 제거되면 PlayerSkillManager의 장착 변경과 사용 이벤트 구독을 모두 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (_skillManager != null)
            {
                _skillManager.OnSkillChanged -= OnSkillChanged;
                _skillManager.OnSkillUsed -= OnSkillUsed;
            }
        }

        /// <summary>
        /// 표시용 쿨타임이 끝나면 아이콘 색만 복원합니다. 실제 재사용 차단은 PlayerSkillManager가 담당합니다.
        /// </summary>
        private void Update()
        {
            if (!_isCoolingDown || Time.time < _cooldownEndTime) return;
            SetCooldown(0f);
        }

        /// <summary>
        /// 자신의 슬롯에 전달된 스킬의 이미지를 표시하며, null이면 장착 해제로 처리하여 아이콘을 비웁니다.
        /// </summary>
        private void OnSkillChanged(int slot, PlayerSkill skill)
        {
            if (slot != _slotIndex) return;
            if (_skillIcon != null)
            {
                _skillIcon.sprite = skill != null ? skill.SkillIcon : null;
                _skillIcon.enabled = _skillIcon.sprite != null;
            }
            SetCooldown(0f);
        }

        /// <summary>
        /// 사용한 스킬의 보정 완료된 CoolTime을 그대로 읽어 해당 시간 동안 자신의 슬롯 아이콘을 어둡게 표시합니다.
        /// </summary>
        private void OnSkillUsed(int slot, PlayerSkill skill)
        {
            if (slot != _slotIndex || skill == null) return;
            float coolTime = skill.CoolTime;
            if (float.IsNaN(coolTime) || float.IsInfinity(coolTime)) coolTime = 0f;
            SetCooldown(Time.time + Mathf.Max(0f, coolTime));
        }

        /// <summary>
        /// UI 최초 표시와 재활성화 때만 매니저를 조회하여 놓친 장착 변경과 남은 쿨타임을 복원합니다.
        /// </summary>
        private void RefreshSlot()
        {
            PlayerSkill skill = _skillManager != null ? _skillManager.GetSkill(_slotIndex) : null;
            OnSkillChanged(_slotIndex, skill);
            SetCooldown(skill != null ? _skillManager.GetCooldownEndTime(_slotIndex) : 0f);
        }

        /// <summary>
        /// 표시용 종료 시각과 색을 함께 갱신하며 PlayerSkillManager의 입력 제한 상태는 변경하지 않습니다.
        /// </summary>
        private void SetCooldown(float endTime)
        {
            _cooldownEndTime = endTime;
            _isCoolingDown = Time.time < _cooldownEndTime;
            if (_skillIcon != null) _skillIcon.color = _isCoolingDown ? _cooldownColor : _normalColor;
        }
    }
}
