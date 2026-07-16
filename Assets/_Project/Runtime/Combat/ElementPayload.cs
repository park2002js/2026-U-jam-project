using System;

namespace UJam.Runtime.Combat
{
    public readonly struct ElementPayload
    {
        public ElementPayload(string elementId, float magnitude)
        {
            ElementId = elementId;
            Magnitude = magnitude;
        }

        public string ElementId { get; }

        public float Magnitude { get; }

        public bool IsEmpty => string.IsNullOrEmpty(ElementId);
    }
}
