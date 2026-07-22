namespace UJam.Runtime.Combat
{
    public interface IDamageable
    {
        // 피해 정보를 받아 실제로 감소한 체력 반환
        float TakeDamage(DamageInfo info);
    }
}
