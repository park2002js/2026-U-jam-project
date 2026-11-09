using System;
using System.Collections;
using UnityEngine;
using UJam.Runtime.Player;

namespace Ujam.Runtime.Item
{
    public enum ItemIncomingMotion { Fall, CameraProjectile, SkyLightning }

    /// <summary>아이템 하나의 낙하·착탄·잔류 모델 프리팹 참조. 재생 시점과 속도는 Effect가 지정한다.</summary>
    public sealed class ItemCastVfx : MonoBehaviour
    {
        [SerializeField] private GameObject incomingPrefab;
        [SerializeField] private GameObject impactPrefab;
        [SerializeField] private GameObject settledPrefab;
        [SerializeField] private bool incomingFacesDown = true;
        [SerializeField] private ItemIncomingMotion incomingMotion;
        [SerializeField, Min(.0001f)] private float incomingScale = 1f;
        [SerializeField, Min(.0001f)] private float impactReferenceDiameter = 1f;

        public GameObject Incoming { get; private set; }
        public GameObject ImpactInstance { get; private set; }
        public GameObject SettledInstance { get; private set; }
        public float ImpactReferenceDiameter => impactReferenceDiameter;
        public ItemIncomingMotion Motion => incomingMotion;
        private Coroutine falling;
        private Camera viewCamera;
        private Vector3 columnTop;
        private LineRenderer[] columnLines;
        private float[] columnWidths;
        private float columnPlaybackSpeed = 1;

        public static IEnumerator Create(ItemUseContext context, Action<ItemCastVfx> loaded)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (loaded == null) throw new ArgumentNullException(nameof(loaded));
            string path = context.Item.Meta.VfxPath;
            if (!context.Item.IsEquipped || string.IsNullOrWhiteSpace(path)) { loaded(null); yield break; }
            GameObject prefab = null;
            yield return ItemAssets.LoadAsync(path, value => prefab = value);
            if (prefab == null || !context.Item.IsEquipped) { loaded(null); yield break; }
            var instance = Instantiate(prefab, context.Position + Vector3.up * .03f,
                Quaternion.LookRotation(context.Item.Meta.Preview.Direction));
            context.Item.Runtime.Track(instance);
            var visual = instance.GetComponent<ItemCastVfx>();
            if (visual == null || visual.incomingPrefab == null || visual.impactPrefab == null)
            {
                Debug.LogError($"[ItemCastVfx] {path} 루트의 ItemCastVfx와 Incoming/Impact 프리팹을 확인하세요.");
                Destroy(instance);
                loaded(null);
                yield break;
            }
            var combat = context.Player != null ? context.Player.GetComponentInParent<PlayerCombatManager>() : null;
            visual.viewCamera = combat != null && combat.AimCamera != null ? combat.AimCamera : Camera.main;
            loaded(visual);
        }

        public void BeginIncoming(float height, float fallDuration, float playbackSpeed, float startDelay = 0)
        {
            Positive(height, nameof(height));
            Positive(fallDuration, nameof(fallDuration));
            Positive(playbackSpeed, nameof(playbackSpeed));
            Positive(incomingScale, nameof(incomingScale));
            if (!float.IsFinite(startDelay) || startDelay < 0) throw new ArgumentOutOfRangeException(nameof(startDelay));
            falling = StartCoroutine(Fall(height, fallDuration, playbackSpeed, startDelay));
        }

        private IEnumerator Fall(float height, float duration, float speed, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            Incoming = Instantiate(incomingPrefab, transform);
            Incoming.transform.localScale *= incomingScale;
            if (incomingMotion == ItemIncomingMotion.CameraProjectile && viewCamera != null)
            {
                yield return FlyFromCamera(height, duration, speed);
                yield break;
            }
            if (incomingMotion == ItemIncomingMotion.SkyLightning)
            {
                yield return StrikeFromSky(height, duration, speed);
                yield break;
            }
            Incoming.transform.localPosition = Vector3.up * height;
            Incoming.transform.localRotation = incomingFacesDown
                ? Quaternion.LookRotation(Vector3.down, Vector3.forward) : Quaternion.identity;
            PlayParticles(Incoming, speed);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                Incoming.transform.localPosition = Vector3.up * (height * (1 - elapsed / duration));
                yield return null;
            }
            Incoming.transform.localPosition = Vector3.zero;
        }

        private IEnumerator FlyFromCamera(float height, float duration, float speed)
        {
            Transform camera = viewCamera.transform;
            Vector3 target = transform.position;
            float depth = Mathf.Max(height, Vector3.Dot(target - camera.position, camera.forward));
            Vector3 start = camera.position - camera.forward * (height * .6f) - camera.right * (depth * .45f) + camera.up * (depth * .4f);
            Vector3 control = viewCamera.ViewportToWorldPoint(new Vector3(.08f, 1.2f, depth * .45f));
            Incoming.transform.position = start;
            FaceIncoming(control - start);
            PlayParticles(Incoming, speed);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Pow(Mathf.Clamp01(elapsed / duration), 1.2f);
                Incoming.transform.position = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * control + t * t * target;
                FaceIncoming(2 * (1 - t) * (control - start) + 2 * t * (target - control));
                yield return null;
            }
            Incoming.transform.position = target;
        }

        private void FaceIncoming(Vector3 direction)
        {
            if (!incomingFacesDown || direction.sqrMagnitude < .0001f) return;
            Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > .95f ? Vector3.forward : Vector3.up;
            Incoming.transform.rotation = Quaternion.LookRotation(direction, up);
        }

        private IEnumerator StrikeFromSky(float height, float duration, float speed)
        {
            columnPlaybackSpeed = speed;
            Incoming.transform.localPosition = Vector3.zero;
            Incoming.transform.localRotation = Quaternion.identity;
            columnLines = Incoming.GetComponentsInChildren<LineRenderer>(true);
            columnWidths = new float[columnLines.Length];
            for (int i = 0; i < columnLines.Length; i++) columnWidths[i] = columnLines[i].widthMultiplier;
            if (viewCamera != null)
            {
                Vector3 point = viewCamera.transform.InverseTransformPoint(transform.position);
                float upY = Vector3.Dot(viewCamera.transform.up, Vector3.up);
                if (viewCamera.orthographic)
                {
                    if (upY > .001f) height = Mathf.Max(height, (viewCamera.orthographicSize * 1.5f - point.y) / upY);
                }
                else
                {
                    float slope = 1.5f * Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad / 2);
                    float divisor = upY - slope * Vector3.Dot(viewCamera.transform.forward, Vector3.up);
                    if (divisor > .001f) height = Mathf.Max(height, (slope * point.z - point.y) / divisor);
                }
            }
            columnTop = transform.position + Vector3.up * height;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                DrawColumn(Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration)), 1);
                yield return null;
            }
            DrawColumn(1, 1);
        }

        private void DrawColumn(float progress, float strength)
        {
            const int points = 25;
            Vector3 right = viewCamera != null ? Vector3.ProjectOnPlane(viewCamera.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 side = Vector3.Cross(right, Vector3.up);
            float phase = Mathf.Floor(Time.time * 24 * columnPlaybackSpeed) * .37f;
            for (int lineIndex = 0; lineIndex < columnLines.Length; lineIndex++)
            {
                LineRenderer line = columnLines[lineIndex];
                line.positionCount = points;
                line.widthMultiplier = columnWidths[lineIndex] * strength * (1 + .12f * Mathf.Sin(Time.time * 90 * columnPlaybackSpeed));
                for (int i = 0; i < points; i++)
                {
                    float t = Mathf.Min((float)i / (points - 1), progress);
                    float jagged = Mathf.Sin(t * Mathf.PI) * .75f;
                    Vector3 point = Vector3.Lerp(columnTop, transform.position, t) + jagged *
                        (right * (Mathf.PerlinNoise(t * 32, phase) - .5f) + side * (Mathf.PerlinNoise(t * 29 + 50, phase) - .5f));
                    line.SetPosition(i, line.transform.InverseTransformPoint(point));
                }
            }
        }

        private IEnumerator FadeColumn(float duration)
        {
            for (float elapsed = 0; elapsed < duration && Incoming != null; elapsed += Time.deltaTime)
            {
                DrawColumn(1, 1.3f * (1 - elapsed / duration));
                yield return null;
            }
            if (Incoming != null) { Incoming.SetActive(false); Destroy(Incoming); }
        }

        public void Impact(float diameter, float playbackSpeed, float lifetime)
        {
            Positive(diameter, nameof(diameter));
            Positive(playbackSpeed, nameof(playbackSpeed));
            Positive(lifetime, nameof(lifetime));
            Positive(impactReferenceDiameter, nameof(impactReferenceDiameter));
            if (falling != null) StopCoroutine(falling);
            if (Incoming != null)
            {
                if (incomingMotion == ItemIncomingMotion.SkyLightning && columnLines != null)
                {
                    DrawColumn(1, 1.3f);
                    StartCoroutine(FadeColumn(Mathf.Min(.16f / columnPlaybackSpeed, lifetime)));
                }
                else { Incoming.SetActive(false); Destroy(Incoming); }
            }
            ImpactInstance = Instantiate(impactPrefab, transform);
            ImpactInstance.transform.localPosition = Vector3.zero;
            ImpactInstance.transform.localScale *= diameter / impactReferenceDiameter;
            PlayParticles(ImpactInstance, playbackSpeed);
            Destroy(gameObject, lifetime);
        }

        public GameObject CreateSettled(Transform parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (settledPrefab == null) return null;
            SettledInstance = Instantiate(settledPrefab, transform.position, transform.rotation);
            // 도발 Collider 크기가 허수아비 모델 크기에 곱해지지 않도록 월드 크기를 유지한다.
            SettledInstance.transform.SetParent(parent, true);
            return SettledInstance;
        }

        private static void PlayParticles(GameObject instance, float speed)
        {
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particle in particles)
            {
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particle.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.simulationSpeed *= speed;
            }
            foreach (var particle in particles)
            {
                var ancestor = particle.transform.parent != null ? particle.transform.parent.GetComponentInParent<ParticleSystem>() : null;
                if (ancestor == null || !ancestor.transform.IsChildOf(instance.transform)) particle.Play(true);
            }
        }

        private static void Positive(float value, string name)
        {
            if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "유한한 양수가 필요합니다.");
        }
    }
}
