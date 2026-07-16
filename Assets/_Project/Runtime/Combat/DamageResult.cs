namespace UJam.Runtime.Combat
{
    public readonly struct DamageResult
    {
        public DamageResult(
            float actualDamage,
            bool blocked,
            bool killed,
            HitZone hitZone)
        {
            ActualDamage = actualDamage;
            Blocked = blocked;
            Killed = killed;
            HitZone = hitZone;
        }

        public float ActualDamage { get; }

        public bool Blocked { get; }

        public bool Killed { get; }

        public HitZone HitZone { get; }
    }
}
