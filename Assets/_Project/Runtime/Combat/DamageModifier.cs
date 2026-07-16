using UnityEngine;

namespace UJam.Runtime.Combat
{
    public abstract class DamageModifier : MonoBehaviour
    {
        // 같은 대상의 Modifier 실행 순서
        [SerializeField] private int _order;

        // Inspector에 설정된 실행 순서
        public int Order
        {
            get
            {
                // Modifier 정렬 기준 반환
                return _order;
            }
        }

        // 현재 피해량을 보정하거나 명시적으로 차단
        public abstract DamageModification Modify(DamageInfo info, float currentDamage);
    }
}
