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

        [Header("시각 이펙트")]
        [SerializeField] private GameObject visualPrefab;              // 대상 몸에 붙일 이펙트
        [SerializeField] private Vector3 visualOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private bool fitToTargetSize = true;          // 대상 크기에 맞춰 스케일

        public float Duration => duration;
        public float TickInterval => tickInterval;
        protected GameObject VisualPrefab => visualPrefab;

        // 명중 효과는 Apply에서 즉시 처리하며 지속 효과의 슬롯을 점유하지 않는다.
        public virtual bool IsShootingHitEffect => false;

        public virtual void Apply(ItemUseContext context) {}
        public virtual void Tick(ItemUseContext context) {}
        public virtual void Remove(ItemUseContext context) {}

        // 대상에게 이펙트를 붙이고 인스턴스를 반환 (ActiveEffect가 보관)
        public GameObject SpawnVisual(ItemUseContext context)
        {
            if (visualPrefab == null || context.Target == null) return null;

            Vector3 pos = context.Target.transform.position + visualOffset;
            GameObject fx = Object.Instantiate(visualPrefab, pos, Quaternion.identity);

            // 대상을 따라다니도록 자식으로 붙임 (ElementReceiver와 동일)
            fx.transform.SetParent(context.Target.transform);

            // 대상 콜라이더 크기에 맞춰 스케일 (AdjustEffectSize와 동일)
            if (fitToTargetSize)
            {
                Collider col = context.Target.GetComponent<Collider>();
                if (col != null)
                {
                    float size = Mathf.Max(col.bounds.size.x, col.bounds.size.y, col.bounds.size.z);
                    fx.transform.localScale = new Vector3(size, size, size);
                }
            }

            return fx;
        }
    }
}
