using System;
using System.Collections;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    public sealed class ItemPreviewDefinition
    {
        public string PrefabPath { get; }
        public Vector3 Scale { get; }
        public Action<GameObject> Modify { get; }
        // Prefab의 기본 지름이 1인 원형 프리뷰라면 Scale.x/z = radius * 2.
        public ItemPreviewDefinition(string prefabPath, Vector3 scale, Action<GameObject> modify = null)
        {
            PrefabPath = prefabPath; Scale = scale; Modify = modify;
        }
    }

    public static class ItemPrefabs
    {
        public const string PreviewPath = "ItemPipeline/PreviewCircle";
        public const string AreaPath = "ItemPipeline/TriggerSphere";
        // 실제 위치: Assets/_Project/Prefabs/Resources/{path}.prefab
        public static IEnumerator LoadAsync(string path, Action<GameObject> loaded)
        {
            if (string.IsNullOrWhiteSpace(path)) { loaded(null); yield break; }
            var request = Resources.LoadAsync<GameObject>(path);
            yield return request;
            var prefab = request.asset as GameObject;
            if (prefab == null) Debug.LogError($"[ItemPrefabs] Resources/{path}.prefab을 불러오지 못했습니다.");
            loaded(prefab);
        }
    }

    public sealed class ItemPreview : IDisposable
    {
        private GameObject instance;
        private bool disposed;
        public bool IsReady => instance != null;
        public bool Failed { get; private set; }
        public ItemPreview(ItemPreviewDefinition definition)
        {
            RuntimeTimer.Ensure().StartCoroutine(ItemPrefabs.LoadAsync(definition.PrefabPath, prefab =>
            {
                if (disposed) return;
                if (prefab == null) { Failed = true; return; }
                instance = UnityEngine.Object.Instantiate(prefab);
                instance.SetActive(false);
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                instance.transform.localScale = definition.Scale;
                try { definition.Modify?.Invoke(instance); }
                catch (Exception exception) { Debug.LogException(exception); Failed = true; Dispose(); }
            }));
        }
        public void Show(Vector3 position, Vector3 normal)
        {
            if (instance == null) return;
            instance.transform.position = position + normal * 0.02f;
            instance.SetActive(true);
        }
        public void Hide() { if (instance != null) instance.SetActive(false); }
        public void Dispose()
        {
            disposed = true;
            if (instance != null) { instance.SetActive(false); UnityEngine.Object.Destroy(instance); }
            instance = null;
        }
    }
}
