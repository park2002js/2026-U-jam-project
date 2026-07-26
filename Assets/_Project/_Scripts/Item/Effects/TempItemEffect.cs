using System.Diagnostics;

namespace UJam.Runtime.Item
{
    [CreateAssetMenu(
        fileName = "NewHealEffect",
        menuName = "Game/Items/Effects/Templete"
    )]
    public sealed class HealEffect : ItemEffect
    {
        public override void Apply(ItemUseContext context)
        {
            // 여기에 세부 능력을 정의
            Debug.Log("효과 사용됨");
            // ex: context.Player.health.plus(100000);
        }

        public override void Remove(ItemUseContext context)
        {
            // 여기에 세부 능력을 정의
            Debug.Log("아이템 제거됨");
        }
    }
}