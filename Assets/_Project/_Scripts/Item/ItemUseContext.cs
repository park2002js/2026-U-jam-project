// 아이템 효과를 발동시킨 쪽에서 해당 구조체를 정의하여 Apply(context)로 호출합니다.
using UnityEngine;
namespace UJam.Runtime.Item
{
    public readonly struct ItemUseContext
    {
        public GameObject User { get; }
        public GameObject Target { get; }

        public ItemUseContext(GameObject user, GameObject target = null)
        {
            User = user;
            Target = target;
        }
    }
}