using UnityEngine;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// 스킬의 시전 타입을 설정한다.
    /// - 일반시전 : 스킬 키를 누르면, 그것이 적용될 범위를 Preview로 보여주고 
    /// </summary>
    public enum SkillCastType
    {
        Normal,     // 일반 시전
        Instant     // 즉시시전
    }

    public class PlayerSkill : MonoBehaviour
    {
        protected PlayerCombatManager _combatManager;

        public PlayerCombatManager CombatManager { get { return _combatManager; } }

        // 스킬이 어느 타입인지 정의한다.
        public SkillCastType CastType { get; set; }

        // 스킬이 적용될 범위를 정의한다.
        public float EffectRadius { get; set; }

        // 스킬의 재사용까지 걸리는 쿨 타임을 정의한다.
        public float CoolTime {get; set;}

        // UI에 띄울 스킬의 아이콘을 정의한다.
        public Sprite SkillIcon { get; set; }
        

        /// <summary>
        /// 스킬의 기본 속성들을 할당하고 초기화한다.
        /// </summary>
        public virtual void Init(PlayerCombatManager combatManager)
        {
            _combatManager = combatManager;
        }

        /// <summary>
        /// 외부에서 스킬을 발동시키기 위해 호출하는 함수이다.
        /// </summary>
        public virtual void Excute(Vector3 targetPosition)
        {
            Debug.Log("Skill 발동");
        }

        public virtual void TakeEffects(){}
    }
}
