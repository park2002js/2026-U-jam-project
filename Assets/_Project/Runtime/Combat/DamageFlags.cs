using System;

namespace UJam.Runtime.Combat
{
    [Flags]
    public enum DamageFlags
    {
        None = 0,
        Critical = 1 << 0,
        IgnoreDefense = 1 << 1
    }
}
