using System;

namespace UJam.Runtime.Combat
{
    public readonly struct Faction
    {
        public Faction(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Id);
    }
}
