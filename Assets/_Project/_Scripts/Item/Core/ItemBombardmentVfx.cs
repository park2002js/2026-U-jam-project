using System;
using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Player;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    public readonly struct ItemBombardmentArea
    {
        public Vector3 Center { get; }
        public Vector3 Size { get; }
        public Quaternion Rotation { get; }
        public ItemBombardmentArea(Vector3 center, Vector3 size, Quaternion rotation)
        { Center = center; Size = size; Rotation = rotation; }
    }

    /// <summary>폭격 한 번의 교체 가능한 폭탄·폭발 프리팹. 판정과 재생 시각은 Effect가 관리한다.</summary>
    public sealed class ItemBombardmentVfx : MonoBehaviour
    {
        [SerializeField] private GameObject bombPrefab, impactPrefab;
        [SerializeField, Min(.01f)] private float impactReferenceDiameter = 2f;
        [SerializeField, Min(.1f)] private float bombSpacing = 3f;
        private readonly List<GameObject> bombs = new(), impacts = new();
        private readonly List<Vector3> targets = new();
        private readonly List<float> starts = new(), diameters = new();
        private Coroutine incoming;
        private Camera viewCamera;
        public IReadOnlyList<GameObject> Bombs => bombs;
        public IReadOnlyList<GameObject> Impacts => impacts;
        public ItemBombardmentArea[] Areas { get; private set; }
        public int ImpactFrame { get; private set; } = -1;

        public static IEnumerator Create(ItemUseContext context, Action<ItemBombardmentVfx> loaded)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (loaded == null) throw new ArgumentNullException(nameof(loaded));
            string path = context.Item.Meta.VfxPath;
            if (!context.Item.IsEquipped || string.IsNullOrWhiteSpace(path)) { loaded(null); yield break; }
            GameObject prefab = null;
            yield return ItemAssets.LoadAsync(path, value => prefab = value);
            if (prefab == null || !context.Item.IsEquipped) { loaded(null); yield break; }
            var instance = Instantiate(prefab, context.Position, Quaternion.identity);
            context.Item.Runtime.Track(instance);
            var visual = instance.GetComponent<ItemBombardmentVfx>();
            if (visual == null || visual.bombPrefab == null || visual.impactPrefab == null ||
                !float.IsFinite(visual.bombSpacing) || visual.bombSpacing <= 0 ||
                !float.IsFinite(visual.impactReferenceDiameter) || visual.impactReferenceDiameter <= 0)
            {
                Debug.LogError($"[ItemBombardmentVfx] {path} 루트의 폭탄·폭발 프리팹과 기준 크기를 확인하세요.");
                Destroy(instance); loaded(null); yield break;
            }
            var combat = context.Player != null ? context.Player.GetComponentInParent<PlayerCombatManager>() : null;
            visual.viewCamera = combat != null && combat.AimCamera != null ? combat.AimCamera : Camera.main;
            loaded(visual);
        }

        public void Begin(ItemBombardmentArea[] areas, float spawnHeight, float fallDuration, float sweepDuration, float incomingSpeed)
        {
            if (areas == null || areas.Length == 0) throw new ArgumentException(nameof(areas));
            Positive(spawnHeight, nameof(spawnHeight)); Positive(fallDuration, nameof(fallDuration));
            Positive(incomingSpeed, nameof(incomingSpeed));
            if (!float.IsFinite(sweepDuration) || sweepDuration < 0) throw new ArgumentOutOfRangeException(nameof(sweepDuration));
            Areas = areas;
            for (int a = 0; a < areas.Length; a++)
            {
                var area = areas[a];
                Positive(area.Size.x, nameof(areas)); Positive(area.Size.z, nameof(areas));
                int columns = Mathf.Max(1, Mathf.CeilToInt(area.Size.x / bombSpacing));
                int rows = Mathf.Max(1, Mathf.CeilToInt(area.Size.z / bombSpacing));
                bool reverseX = viewCamera != null && Vector3.Dot(area.Rotation * Vector3.right, viewCamera.transform.right) < -.001f;
                bool reverseZ = viewCamera != null && Vector3.Dot(area.Rotation * Vector3.forward, viewCamera.transform.up) < -.001f;
                for (int z = 0; z < rows; z++)
                    for (int x = 0; x < columns; x++)
                    {
                        var offset = new Vector3(((x + .5f) / columns - .5f) * area.Size.x, .08f,
                            ((z + .5f) / rows - .5f) * area.Size.z);
                        targets.Add(area.Center + area.Rotation * offset);
                        // 카메라가 반대편을 보더라도 화면의 좌→우·위→아래 순서를 유지한다.
                        float progress = a == 0 ? (columns == 1 ? 0 : (float)x / (columns - 1)) :
                            (rows == 1 ? 0 : (float)z / (rows - 1));
                        if (a == 0 ? columns > 1 && reverseX : rows > 1 && !reverseZ) progress = 1 - progress;
                        starts.Add(progress * sweepDuration);
                        diameters.Add(Mathf.Max(area.Size.x / columns, area.Size.z / rows) * 1.1f);
                        bombs.Add(null);
                    }
            }
            incoming = StartCoroutine(Drop(spawnHeight, fallDuration, incomingSpeed));
        }

        private IEnumerator Drop(float height, float duration, float speed)
        {
            for (float elapsed = 0; ; elapsed += Time.deltaTime)
            {
                bool done = true;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (elapsed < starts[i]) { done = false; continue; }
                    if (bombs[i] == null)
                    {
                        bombs[i] = Instantiate(bombPrefab, targets[i] + Vector3.up * height, Quaternion.identity, transform);
                        Play(bombs[i], speed);
                    }
                    float t = Mathf.Clamp01((elapsed - starts[i]) / duration);
                    bombs[i].transform.position = targets[i] + Vector3.up * (height * (1 - t * t));
                    if (t < 1) done = false;
                }
                if (done) yield break;
                yield return null;
            }
        }

        public void Impact(float playbackSpeed, float lifetime)
        {
            Positive(playbackSpeed, nameof(playbackSpeed)); Positive(lifetime, nameof(lifetime));
            if (ImpactFrame >= 0) return;
            ImpactFrame = Time.frameCount;
            if (incoming != null) StopCoroutine(incoming);
            foreach (var bomb in bombs) if (bomb != null) { bomb.SetActive(false); Destroy(bomb); }
            for (int i = 0; i < targets.Count; i++)
            {
                var blast = Instantiate(impactPrefab, targets[i], Quaternion.identity, transform);
                blast.transform.localScale *= diameters[i] / impactReferenceDiameter;
                Play(blast, playbackSpeed);
                impacts.Add(blast);
            }
            Destroy(gameObject, lifetime);
        }

        private static void Play(GameObject root, float speed)
        {
            foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particle.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.simulationSpeed *= speed;
            }
            foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true))
                if (particle.transform.parent == null || particle.transform.parent.GetComponentInParent<ParticleSystem>() == null)
                    particle.Play(true);
        }

        private static void Positive(float value, string name)
        {
            if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
        }
    }
}
