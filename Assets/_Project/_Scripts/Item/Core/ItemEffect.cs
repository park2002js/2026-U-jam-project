namespace Ujam.Runtime.Item
{
    /// <summary>
    /// 아이템별 동작을 담는 일반 C# 클래스. 효과 하나당 클래스 하나를 만들고 Catalog에 등록한다.
    /// 생성자에는 조정 수치만 받는다. 보유자/카운터/적중 목록을 필드에 저장하지 않는다.
    /// Effect의 설정은 공유하므로 카운터는 context.Player, 작업 수명은 context.Item.Runtime에 맡긴다.
    /// </summary>
    public abstract class ItemEffect
    {
        /// <summary>이벤트를 받은 순간의 조건을 검사한다. 기본값은 항상 발동이며 조건은 최대 두 개로 유지한다.</summary>
        public virtual bool CanExecute(ItemUseContext context) => true;
        /// <summary>해당 아이템의 동작을 구현한다. 지연/지속 동작은 context.Run(코루틴)으로 실행한다.</summary>
        public abstract void Execute(ItemUseContext context);
        /// <summary>장착 시 추가 준비가 필요할 때만 재정의한다. 이벤트 구독은 Runtime이 담당한다.</summary>
        public virtual void OnEquip(ItemUseContext context) { }
        /// <summary>장착 해제 시 효과 고유의 정리가 필요한 경우만 재정의한다. 버프/오브젝트는 공통으로 정리된다.</summary>
        public virtual void OnUnequip(ItemUseContext context) { }
    }
}
