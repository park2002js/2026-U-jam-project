using System;
using UnityEngine;

namespace UJam.Runtime.Enemy.Vfx
{
    public sealed class SniperBeam : MonoBehaviour
    {
        [SerializeField] private LineRenderer _shell;
        [SerializeField] private LineRenderer _halo;
        [SerializeField] private LineRenderer[] _currents;
        [SerializeField] private LineRenderer[] _offshoots;
        [SerializeField] private GameObject _targetEnergy;
        [SerializeField] private GameObject _muzzleFlash;
        [SerializeField] private GameObject _targetImpact;

        private const float BeamHold = .08f;
        private const float BeamDuration = .18f;
        private const float EffectDuration = .42f;
        private readonly Vector3[] points = new Vector3[25];
        private readonly Vector3[][] currentPoints = { new Vector3[25], new Vector3[25], new Vector3[25] };
        private readonly Vector3[][] branchPoints = { new Vector3[5], new Vector3[5], new Vector3[5], new Vector3[5] };
        private EnemyBase owner;
        private bool playing, hasOwner;
        private float started;
        private Vector3 source, destination;
        private LineRenderer[] lines;
        private float[] authoredWidths;
        private Renderer[] energyRenderers;
        private MaterialPropertyBlock pulse;
        private static readonly int Pulse = Shader.PropertyToID("_Pulse");

        private void Awake()
        {
            lines = GetComponentsInChildren<LineRenderer>(true);
            authoredWidths = new float[lines.Length];
            for (int i = 0; i < lines.Length; i++) authoredWidths[i] = lines[i].widthMultiplier;
            energyRenderers = _targetEnergy != null ? _targetEnergy.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            pulse = new MaterialPropertyBlock();
        }

        public void Play(Vector3 start, Vector3 target, EnemyBase sourceOwner = null)
        {
            if (!Finite(start) || !Finite(target))
            {
                Destroy(gameObject);
                return;
            }
            bool configured = _shell != null && _halo != null && _currents != null && _currents.Length == 3
                && Array.TrueForAll(_currents, line => line != null) && _offshoots != null && _offshoots.Length == 4
                && Array.TrueForAll(_offshoots, line => line != null) && _targetEnergy != null;
            Debug.Assert(configured,
                "Sniper lightning requires a main trunk, three separate currents, four offshoots and target energy.", this);
            if (!configured)
            {
                Destroy(gameObject);
                return;
            }

            source = start;
            destination = target;
            owner = sourceOwner;
            hasOwner = sourceOwner != null;
            started = Time.time;
            playing = true;
            UpdateGeometry(0f);
            UpdateEnergy(0f);
            _targetEnergy.transform.position = target;
            _targetEnergy.SetActive(true);
            Vector3 direction = target - start;
            Quaternion rotation = direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            if (_muzzleFlash != null) Instantiate(_muzzleFlash, start, rotation, transform);
            if (_targetImpact != null) Instantiate(_targetImpact, target, rotation, transform);
        }

        private void Start()
        {
            if (!playing) Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (!playing) return;
            if (hasOwner && (owner == null || !owner.isActiveAndEnabled || !owner.IsAttackPlaying ||
                owner.Status == null || owner.Status.HP <= 0f))
            {
                Destroy(gameObject);
                return;
            }

            float elapsed = Time.time - started;
            UpdateGeometry(elapsed);
            UpdateEnergy(elapsed);
            if (elapsed >= EffectDuration) Destroy(gameObject);
        }

        private void UpdateGeometry(float elapsed)
        {
            float visibility = WidthAt(elapsed);
            if (visibility > 0f)
            {
                BuildPoints(source, destination, points, Mathf.Floor(elapsed * 29f));
                _shell.SetPositions(points);
                _halo.SetPositions(points);
                for (int i = 0; i < _currents.Length; i++)
                {
                    BuildCurrent(source, destination, currentPoints[i], i, Mathf.Floor(elapsed * (37f + i * 9f)));
                    _currents[i].SetPositions(currentPoints[i]);
                }
                for (int i = 0; i < _offshoots.Length; i++)
                {
                    BuildOffshoot(currentPoints[i % currentPoints.Length], i, branchPoints[i], Mathf.Floor(elapsed * (41f + i * 7f)));
                    _offshoots[i].SetPositions(branchPoints[i]);
                }
            }
            for (int i = 0; i < lines.Length; i++)
            {
                LineRenderer line = lines[i];
                line.enabled = visibility > 0f && source != destination;
                line.widthMultiplier = authoredWidths[i] * Mathf.Lerp(.88f, 1f, visibility);
                float flicker = line == _shell || line == _halo ? 1f : .64f + .36f * Mathf.Abs(Mathf.Sin(elapsed * (91f + i * 13f) + i * 2.3f));
                pulse.SetFloat(Pulse, visibility * flicker);
                line.SetPropertyBlock(pulse);
            }
        }

        private void UpdateEnergy(float elapsed)
        {
            pulse.SetFloat(Pulse, EnergyAt(elapsed) * (.9f + .1f * Mathf.Sin(elapsed * 83f)));
            foreach (Renderer renderer in energyRenderers) renderer.SetPropertyBlock(pulse);
            if (_targetEnergy != null) _targetEnergy.transform.localScale = Vector3.one * Mathf.Lerp(.8f, 1.1f, Mathf.Clamp01(elapsed / .1f));
        }

        public static float WidthAt(float elapsed) => 1f - Mathf.InverseLerp(BeamHold, BeamDuration, elapsed);
        public static float EnergyAt(float elapsed) => 1f - Mathf.InverseLerp(.1f, .4f, elapsed);

        // All full-length currents meet the exact shot endpoints; geometry never selects a damage target.
        public static void BuildPoints(Vector3 start, Vector3 target, Vector3[] output, float phase = 0f)
        {
            ValidateOutput(output);
            Basis(start, target, out Vector3 side, out Vector3 rise, out float distance);
            float amplitude = Mathf.Min(.045f, distance * .009f);
            output[0] = start;
            output[output.Length - 1] = target;
            for (int i = 1; i < output.Length - 1; i++)
            {
                float t = i / (float)(output.Length - 1);
                output[i] = Vector3.Lerp(start, target, t) + amplitude * Mathf.Sin(t * Mathf.PI)
                    * (side * Jitter(i, phase) + rise * Jitter(i + 17, phase) * .35f);
            }
        }

        public static void BuildCurrent(Vector3 start, Vector3 target, Vector3[] output, int current, float phase)
        {
            ValidateOutput(output);
            Basis(start, target, out Vector3 side, out Vector3 rise, out float distance);
            float radius = Mathf.Min(.22f, distance * .045f);
            output[0] = start;
            output[output.Length - 1] = target;
            for (int i = 1; i < output.Length - 1; i++)
            {
                float t = i / (float)(output.Length - 1);
                float angle = t * (6.1f + current * .85f) + current * 2.1f + phase * .3f;
                float envelope = Mathf.Sin(t * Mathf.PI) * radius * (.82f + .18f * Mathf.Sin(t * 13f + current));
                output[i] = Vector3.Lerp(start, target, t) + envelope
                    * (side * (Mathf.Cos(angle) + Jitter(i + current * 7, phase) * .18f)
                    + rise * (Mathf.Sin(angle) * .8f + Jitter(i + 31, phase) * .18f));
            }
        }

        public static void BuildOffshoot(Vector3[] current, int branch, Vector3[] output, float phase)
        {
            ValidateOutput(current);
            ValidateOutput(output);
            Vector3 start = current[Mathf.Clamp(Mathf.RoundToInt((current.Length - 1) * (.22f + branch * .14f)), 0, current.Length - 1)];
            Vector3 delta = current[current.Length - 1] - current[0];
            Basis(current[0], current[current.Length - 1], out Vector3 side, out Vector3 rise, out float distance);
            float length = Mathf.Min(.65f, distance * .12f);
            Vector3 direction = distance > .0001f ? delta / distance : Vector3.forward;
            Vector3 tip = start + length * (direction * .5f + side * (branch % 2 == 0 ? .8f : -.8f)
                + rise * Mathf.Cos(branch * 1.7f) * .35f);
            output[0] = start;
            output[output.Length - 1] = tip;
            for (int i = 1; i < output.Length - 1; i++)
            {
                float t = i / (float)(output.Length - 1);
                output[i] = Vector3.Lerp(start, tip, t) + rise * (length * .2f * Mathf.Sin(t * Mathf.PI) * Jitter(i + branch * 11, phase));
            }
        }

        private static void Basis(Vector3 start, Vector3 target, out Vector3 side, out Vector3 rise, out float distance)
        {
            Vector3 delta = target - start;
            distance = delta.magnitude;
            Vector3 direction = distance > .0001f ? delta / distance : Vector3.forward;
            side = Vector3.Cross(direction, Mathf.Abs(direction.y) > .98f ? Vector3.forward : Vector3.up).normalized;
            rise = Vector3.Cross(side, direction).normalized;
        }

        private static float Jitter(int index, float phase) => Mathf.Repeat(Mathf.Sin(index * 12.9898f + phase * 7.17f) * 43758.5453f, 1f) * 2f - 1f;

        private static void ValidateOutput(Vector3[] output)
        {
            if (output == null || output.Length < 2)
                throw new ArgumentException("A sniper current needs at least two points.", nameof(output));
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
