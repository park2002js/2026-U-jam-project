namespace UJam.Runtime.Combat
{
    // 버프/디버프로 스탯을 증감시킬 수 있는 대상 (적·아군 공통)
    public interface IStatModifiable
    {
        // 이동 속도 증감 (+면 가속, -면 슬로우)
        void ModifySpeed(float delta);

        // 공격 속도 증감
        void ModifyAttackSpeed(float delta);

        // 공격력 증감
        void ModifyAttackDamage(float delta);
    }
}