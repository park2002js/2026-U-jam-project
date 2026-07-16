namespace UJam.Runtime.Combat
{
    public interface IDamageable
    {
        DamageResult TakeDamage(DamageInfo info);
    }
}
