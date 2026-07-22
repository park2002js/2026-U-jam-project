using UnityEngine;
using Equipment;

namespace Equipment.Weapon
{
    // 무기 공통 클래스
    public abstract class Weapon : Equipment
    {
        [Header("Weapon Base Stats")]
        [Tooltip("무기 고유의 공격력")]
        public float baseAD;
        
        [Tooltip("무기 고유의 공격 속도")]
        public float baseAS;

        // PlayerStatManager 등의 영향을 받아 최종적으로 계산된 스탯 (캐싱용)
        protected float currentAD;
        protected float currentAS;

        // 쿨타임 제어용 변수
        protected float lastAttackTime = -999f;

        protected virtual void Start()
        {
            // 장착 시 최초 1회 스탯 갱신
            UpdateFinalStats();
        }

        // 매 프레임 계산하지 않고, 외부 요인(PlayerStat)이 바뀔 때만 호출되어 최종 수치 갱신
        public virtual void UpdateFinalStats()
        {
            // TODO: PlayerStatManager 연동 시 아래 주석 해제 및 수정
            // currentAD = baseAD + PlayerStatManager.Instance.GetBonusAD();
            // currentAS = baseAS * PlayerStatManager.Instance.GetAttackSpeedMultiplier();
            
            // 임시 세팅
            currentAD = baseAD;
            currentAS = baseAS;
        }

        // 외부(Player)에서 공격 입력을 받았을 때 호출할 공통 API
        public virtual void Attack()
        {
            
            // 공격속도(AS)를 기반으로 쿨타임 산출 (예: AS가 2면 1초에 2번 = 0.5초 쿨타임)
            float cooldown = 1f / Mathf.Max(currentAS, 0.1f); // 0 나누기 방지

            if (Time.time >= lastAttackTime + cooldown)
            {
                Debug.Log("Attack 호출됨");
                lastAttackTime = Time.time;
                PerformAttack();
            }
        }
        // 쿨타임 검증을 통과한 뒤 호출되는 실제 공격 추상 함수
        protected abstract void PerformAttack();
    }
}