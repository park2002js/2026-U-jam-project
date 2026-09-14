using System.IO;
using UJam.Runtime.Systems;
using UnityEditor;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    public static class ItemPipelinePrefabBuilder
    {
        private const string Folder = "Assets/_Project/Prefabs/Resources/ItemPipeline";
        private const string OriginalPreview = "Assets/_Project/Prefabs/Player/Skill/Preview/SkillPreviewCircle.prefab";

        [MenuItem("Tools/Ujam/Items/Create example prefabs")]
        public static void CreatePrefabs()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            Build("PreviewCircle", false);
            Build("TriggerSphere", true);
            AssetDatabase.SaveAssets();
            Debug.Log("[ItemPipeline] Resources 비동기 로드용 Preview/Trigger Prefab 생성 완료.");
        }

        private static void Build(string name, bool trigger)
        {
            string path = $"{Folder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(OriginalPreview);
            if (original == null) throw new FileNotFoundException(OriginalPreview);
            var root = Object.Instantiate(original);
            try
            {
                root.name = name;
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null) Object.DestroyImmediate(component);
                foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                root.transform.localScale = Vector3.one;
                if (trigger)
                {
                    var sphere = root.AddComponent<SphereCollider>();
                    sphere.radius = 0.5f; sphere.isTrigger = true;
                    var body = root.AddComponent<Rigidbody>();
                    body.useGravity = false; body.isKinematic = true;
                    root.AddComponent<AreaTrigger>();
                    // 장판의 지면 이펙트는 프리뷰보다 조금 띄워 겹침을 방지한다.
                    foreach (Transform child in root.transform) child.localPosition += Vector3.up * 0.01f;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
