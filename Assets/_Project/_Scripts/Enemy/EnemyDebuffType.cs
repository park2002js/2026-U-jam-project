namespace UJam.Runtime.Enemy
{
    /// <summary>적에게 걸리는 디버프 종류. 속성(ElementType)과 별개로 관리한다.</summary>
    public enum EnemyDebuffType
    {
        AttackDown,     // 약화(3)     — 공격력 감소
        DamageTakenUp,  // 안개(4)     — 받는 피해 증가
        SlowDown,       // 중력장(5)   — 이동속도 감소
        Marked,         // 집중 사격(1) — 표적 표시
    }
}