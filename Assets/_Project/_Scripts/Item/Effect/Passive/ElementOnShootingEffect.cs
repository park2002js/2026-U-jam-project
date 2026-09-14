using UJam.Runtime.Systems;

namespace Ujam.Runtime.Item
{
    // 등록되지 않은 제작 예제: 패시브 제작 시 Shooting 트리거와 함께 new로 할당한다.
    // 별도 ID를 등록하지 않으므로 현재 상점에는 액티브 31/33만 등장한다.
    public sealed class ElementOnShootingEffect : ItemEffect
    {
        private readonly UJam.Runtime.Systems.ElementType element;
        public ElementOnShootingEffect(UJam.Runtime.Systems.ElementType element) => this.element = element;
        public override bool CanExecute(ItemUseContext context) => context.Enemies != null;
        public override void Execute(ItemUseContext context) => ElementSystem.Instance.Apply(context.Enemies, element);
    }
}
