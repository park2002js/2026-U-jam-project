using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public sealed class WitchDeathVfx : MonoBehaviour
    {
        [SerializeField] private Renderer[] _bodyRenderers = new Renderer[0];
        [SerializeField] private Renderer[] _staffRenderers = new Renderer[0];
        [SerializeField] private ParticleSystem[] _bodyDust = new ParticleSystem[0];
        [SerializeField] private ParticleSystem[] _staffDust = new ParticleSystem[0];

        private static readonly int Dissolve = Shader.PropertyToID("_Dissolve");
        private static readonly int Origin = Shader.PropertyToID("_DissolveOrigin");
        private static readonly int Height = Shader.PropertyToID("_DissolveHeight");
        private static readonly int Death = Animator.StringToHash("Death");
        private MaterialPropertyBlock block;
        private bool[] bodyEnabled, staffEnabled;
        private Bounds bodyBounds, staffBounds;
        private Animator animator;
        private EnemyBase owner;
        private float started;
        private bool bodyDustStarted, staffDustStarted;

        public bool IsPlaying { get; private set; }
        public float BodyDissolve { get; private set; }
        public float StaffDissolve { get; private set; }
        public int BodyRendererCount => _bodyRenderers.Length;
        public int StaffRendererCount => _staffRenderers.Length;

        private void OnEnable() => ResetEffect();

        public void ResetEffect()
        {
            if (block == null) block = new MaterialPropertyBlock();
            if (bodyEnabled == null || bodyEnabled.Length != _bodyRenderers.Length)
                bodyEnabled = CaptureEnabled(_bodyRenderers);
            if (staffEnabled == null || staffEnabled.Length != _staffRenderers.Length)
                staffEnabled = CaptureEnabled(_staffRenderers);
            bodyBounds = GroupBounds(_bodyRenderers);
            staffBounds = GroupBounds(_staffRenderers);
            BodyDissolve = StaffDissolve = 0f;
            IsPlaying = bodyDustStarted = staffDustStarted = false;
            ApplyGroup(_bodyRenderers, bodyEnabled, bodyBounds, 0f);
            ApplyGroup(_staffRenderers, staffEnabled, staffBounds, 0f);
            StopDust(_bodyDust, true);
            StopDust(_staffDust, true);
        }

        public void Begin()
        {
            if (IsPlaying) return;
            ResetEffect();
            owner = GetComponent<EnemyBase>();
            animator = GetComponentInChildren<Animator>(true);
            started = Time.time;
            IsPlaying = true;
            ApplyTime(0f);
        }

        private void LateUpdate()
        {
            if (!IsPlaying) return;
            float seconds = Time.time - started;
            if (animator != null && owner != null && owner.Animation != null)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash == Death)
                    seconds = state.normalizedTime * owner.Animation.Duration("Death");
            }
            ApplyTime(seconds);
        }

        private void ApplyTime(float seconds)
        {
            BodyDissolve = Mathf.Clamp01(seconds / .7f);
            StaffDissolve = Mathf.InverseLerp(2.2f, 2.7f, seconds);
            if (!bodyDustStarted && seconds < .7f)
            {
                StartDust(_bodyDust);
                bodyDustStarted = true;
            }
            if (!staffDustStarted && seconds >= 2.2f && seconds < 2.7f)
            {
                // The staff has fallen by this point; its dissolve band follows the settled pose.
                staffBounds = GroupBounds(_staffRenderers);
                StartDust(_staffDust);
                staffDustStarted = true;
            }
            ApplyGroup(_bodyRenderers, bodyEnabled, bodyBounds, BodyDissolve);
            ApplyGroup(_staffRenderers, staffEnabled, staffBounds, StaffDissolve);
            if (BodyDissolve >= 1f) StopDust(_bodyDust, false);
            if (StaffDissolve >= 1f) StopDust(_staffDust, false);
            if (seconds >= 3f) IsPlaying = false;
        }

        private void ApplyGroup(Renderer[] renderers, bool[] originalEnabled, Bounds bounds, float amount)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                if (target == null) continue;
                target.GetPropertyBlock(block);
                block.SetFloat(Dissolve, amount);
                block.SetVector(Origin, bounds.min);
                block.SetFloat(Height, Mathf.Max(.01f, bounds.size.y));
                target.SetPropertyBlock(block);
                target.enabled = originalEnabled[i] && amount < 1f;
            }
        }

        private Bounds GroupBounds(Renderer[] renderers)
        {
            var bounds = new Bounds(transform.position, Vector3.zero);
            bool found = false;
            foreach (Renderer target in renderers)
            {
                if (target == null) continue;
                if (target is SkinnedMeshRenderer skin && skin.sharedMesh != null)
                {
                    // Imported renderer bounds cover multiple poses; use the actual falling staff pose.
                    var posed = new Mesh();
                    try
                    {
                        skin.BakeMesh(posed);
                        foreach (Vector3 vertex in posed.vertices)
                        {
                            Vector3 point = skin.transform.TransformPoint(vertex);
                            if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                            else bounds.Encapsulate(point);
                        }
                    }
                    finally
                    {
                        if (Application.isPlaying) Destroy(posed);
                        else DestroyImmediate(posed);
                    }
                }
                else if (!found) { bounds = target.bounds; found = true; }
                else bounds.Encapsulate(target.bounds);
            }
            return bounds;
        }

        private static bool[] CaptureEnabled(Renderer[] renderers)
        {
            var enabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                enabled[i] = renderers[i] != null && renderers[i].enabled;
            return enabled;
        }

        private static void StartDust(ParticleSystem[] systems)
        {
            foreach (ParticleSystem system in systems)
            {
                if (system == null) continue;
                var emission = system.emission;
                emission.enabled = true;
                system.Play(false);
            }
        }

        private static void StopDust(ParticleSystem[] systems, bool clear)
        {
            foreach (ParticleSystem system in systems)
            {
                if (system == null) continue;
                var emission = system.emission;
                emission.enabled = false;
                system.Stop(false, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnDisable()
        {
            IsPlaying = false;
            StopDust(_bodyDust, true);
            StopDust(_staffDust, true);
        }
    }
}
