using System;

namespace UJam.Runtime.Combat
{
    public readonly struct AttackId
    {
        public AttackId(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Value);
    }
}
