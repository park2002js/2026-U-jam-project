using UnityEngine;

namespace UJam.Runtime.Combat
{
    public sealed class DefenseDamageModifier : DamageModifier
    {
        // 피해량에서 차감할 방어력
        [SerializeField, Min(0f)] private float _defense;

        // 외부에서 읽는 방어력 값
        public float Defense
        {
            get
            {
                // Inspector 방어력 반환
                return _defense;
            }
        }

        // IgnoreDefense가 없을 때 방어력을 피해량에서 차감
        public override DamageModification Modify(DamageInfo info, float currentDamage)
        {
            // IgnoreDefense 플래그가 있으면 현재 피해량을 그대로 전달
            if ((info.Flags & DamageFlags.IgnoreDefense) != 0)
            {
                // 방어력을 무시한 보정 결과 반환
                return new DamageModification(currentDamage, false);
            }

            // 방어력 차감 후 0 미만 피해를 차단
            float adjustedDamage = Mathf.Max(0f, currentDamage - _defense);

            // 양의 피해가 방어력으로 0이 되었는지 확인
            bool blocked = currentDamage > 0f && adjustedDamage <= 0f;

            // 방어력 적용 결과 반환
            return new DamageModification(adjustedDamage, blocked);
        }
    }
}
