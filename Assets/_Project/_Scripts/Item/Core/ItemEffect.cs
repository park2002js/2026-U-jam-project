namespace Ujam.Runtime.Item
{
    // 효과와 조건은 아이템마다 new로 생성하여 카운터 등 상태를 공유하지 않는다.
    public abstract class ItemEffect
    {
        public virtual bool CanExecute(ItemUseContext context) => true;
        public abstract void Execute(ItemUseContext context);
        public virtual void OnEquip(ItemRuntime runtime) { }
        public virtual void OnUnequip(ItemRuntime runtime) { }
    }
}
