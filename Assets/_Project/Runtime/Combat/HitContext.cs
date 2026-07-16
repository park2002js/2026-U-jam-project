namespace UJam.Runtime.Combat
{
    public readonly struct HitContext
    {
        public HitContext(HitZone zone)
        {
            Zone = zone;
        }

        public HitZone Zone { get; }
    }
}
