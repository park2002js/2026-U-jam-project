namespace UJam.Runtime.Combat
{
    // Immutable input contract for target-independent damage handling.
    public readonly struct DamageInfo
    {
        public DamageInfo(
            IAttackSource source,
            float rawDamage,
            DamageType damageType,
            ElementPayload? element,
            HitContext hitContext,
            DamageFlags flags)
        {
            Source = source;
            RawDamage = rawDamage < 0f ? 0f : rawDamage;
            DamageType = damageType;
            Element = element;
            HitContext = hitContext;
            Flags = flags;
        }

        public IAttackSource Source { get; }

        public float RawDamage { get; }

        public DamageType DamageType { get; }

        public ElementPayload? Element { get; }

        public HitContext HitContext { get; }

        public DamageFlags Flags { get; }
    }
}
