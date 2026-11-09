using System;
using UJam.Runtime.Enemy;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    /// <summary>씬의 Managers에 배치하는 버프/속성 갱신 및 코루틴 호스트. 없는 씬에서는 자동 생성한다.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-1000)]
    public class RuntimeTimer : MonoBehaviour
    {
        [Serializable]
        private class ElementVisual
        {
            public ElementType type;
            public GameObject prefab;
            [Min(0.001f)] public float scalePerHeight = 1;
        }

        [SerializeField] private ElementVisual[] elementVfx = Array.Empty<ElementVisual>();
        public static RuntimeTimer Instance { get; private set; }
        /// <summary>지속적으로 갱신할 시스템에서 구독한다. 해제 시 반드시 구독을 제거한다.</summary>
        public event Action<float> Tick;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 이전 씬에서 자동 생성된 호스트가 살아 있어도 현재 씬의 VFX 설정을 사용한다.
                if (elementVfx.Length > 0) Instance.elementVfx = elementVfx;
                enabled = false; Destroy(this); return;
            }
            Instance = this;
        }
        /// <summary>코루틴 실행 전 호출한다. 존재하지 않을 때만 중앙 호스트를 만든다.</summary>
        public static RuntimeTimer Ensure()
        {
            if (Instance == null)
            {
                Instance = FindFirstObjectByType<RuntimeTimer>();
                if (Instance == null)
                {
                    var host = new GameObject("RuntimeTimer");
                    Instance = host.AddComponent<RuntimeTimer>();
                    DontDestroyOnLoad(host);
                }
            }
            return Instance;
        }

        /// <summary>속성의 수명에 종속된 표시를 적의 몸 중심에 붙인다. 바람에는 표시를 만들지 않는다.</summary>
        internal GameObject CreateElementVfx(EnemyBase enemy, ElementType type)
        {
            if (type == ElementType.Wind) return null;
            var visual = Array.Find(elementVfx, entry => entry != null && entry.type == type);
            if (visual == null || visual.prefab == null) return null;

            Collider collider = enemy.GetComponentInChildren<Collider>();
            Bounds bounds = collider != null ? collider.bounds : new Bounds(enemy.transform.position + Vector3.up * .5f, Vector3.one);
            foreach (Renderer renderer in enemy.GetComponentsInChildren<Renderer>())
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer) bounds.Encapsulate(renderer.bounds);
            float height = Mathf.Max(.1f, bounds.size.y);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.05f;
            Vector3 position = bounds.center;
            if (type == ElementType.Burn) position.y -= height * .25f;
            GameObject instance = Instantiate(visual.prefab, position, Quaternion.identity, enemy.transform);
            float size = height * visual.scalePerHeight;
            Vector3 worldScale = type == ElementType.Shock
                ? new Vector3(Mathf.Max(.1f, bounds.size.x), height * .6f, Mathf.Max(.1f, bounds.size.z)) * visual.scalePerHeight
                : Vector3.one * size;
            Vector3 parentScale = enemy.transform.lossyScale;
            instance.transform.localScale = new Vector3(
                worldScale.x / Mathf.Max(.0001f, Mathf.Abs(parentScale.x)),
                worldScale.y / Mathf.Max(.0001f, Mathf.Abs(parentScale.y)),
                worldScale.z / Mathf.Max(.0001f, Mathf.Abs(parentScale.z)));
            foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var shape = particle.shape;
                // 불꽃은 몸 전체의 표면, 번개는 프리팹의 반지름 0.5인 몸통 타원체를 사용한다.
                if (type == ElementType.Burn && shape.enabled &&
                    (shape.shapeType == ParticleSystemShapeType.Sphere || shape.shapeType == ParticleSystemShapeType.Circle))
                {
                    shape.radius = radius / Mathf.Max(.001f, size);
                    shape.radiusThickness = 0;
                }
                particle.Play();
            }
            return instance;
        }
        private void Update()
        {
            BuffManager.Instance.Update();
            ElementManager.Instance.Update();

            Tick?.Invoke(Time.deltaTime);
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        // Unity가 실행 시작 시 첫 씬을 불러오기 전에 이 static 메서드를 자동 호출한다.
        // SubsystemRegistration 시점에 정적 상태를 초기화하여 Domain Reload를 꺼도 이전 플레이의 상태가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = null;
    }
}
