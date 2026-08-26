// 아이템 효과를 발동시킨 쪽에서 해당 구조체를 정의하여 Apply(context)로 호출합니다.
using UnityEngine;
using UJam.Runtime.Combat;
namespace UJam.Runtime.Item
{
    public readonly struct ItemUseContext
    {
        public GameObject User { get; }
        public GameObject Target { get; }
        public DamageInfo DamageInfo { get; }
        public Vector3 HitPoint { get; }
        public bool IsShootingHit { get; }

        public ItemUseContext(GameObject user, GameObject target = null)
        {
            User = user;
            Target = target;
            DamageInfo = default;
            HitPoint = default;
            IsShootingHit = false;
        }

        public ItemUseContext(GameObject user, GameObject target, DamageInfo damageInfo, Vector3 hitPoint)
        {
            User = user;
            Target = target;
            DamageInfo = damageInfo;
            HitPoint = hitPoint;
            IsShootingHit = true;
        }
    }
}
