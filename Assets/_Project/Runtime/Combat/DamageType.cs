using System;

namespace UJam.Runtime.Combat
{
    public readonly struct DamageType
    {
        public DamageType(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Id);
    }
}
