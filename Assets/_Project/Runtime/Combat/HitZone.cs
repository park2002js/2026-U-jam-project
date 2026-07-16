using System;

namespace UJam.Runtime.Combat
{
    public readonly struct HitZone
    {
        public HitZone(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Id);
    }
}
