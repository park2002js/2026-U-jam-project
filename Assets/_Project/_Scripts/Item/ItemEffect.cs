using UnityEngine;

// Item의 효과를 구현할 코드들이 상속할 Base입니다.
// context로 전달된 객체를 활용해서 ItemEffect측에서 효과 발동을 할 수 있도록 합니다.
namespace UJam.Runtime.Item
{
    public abstract class ItemEffect : ScriptableObject
    {
        [Header("지속시간 설정")]
        [SerializeField, Min(0f)] private float duration = 0f;
        [SerializeField, Min(0f)] private float tickInterval = 1f;

        public float Duration => duration;
        public float TickInterval => tickInterval;

        public virtual void Apply(ItemUseContext context) {}
        public virtual void Tick(ItemUseContext context) {}
        public virtual void Remove(ItemUseContext context) {}
    }
}   