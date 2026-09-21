using System.IO;
using UJam.Runtime.Systems;
using UnityEditor;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>아이템 지원 프리팹 재생성 도구. Preview를 만들지 않는다. 원본 낙뢰 아트는 복제만 한다.</summary>
    public static class ItemPipelinePrefabBuilder
    {
        private const string Folder = "Assets/_Project/Prefabs/Resources/ItemPipeline";

        /// <summary>지원 프리팹이 없거나 변경되었을 때 Tools/Ujam/Items 메뉴에서 호출한다.</summary>
        [MenuItem("Tools/Ujam/Items/Create support prefabs")]
        public static void CreatePrefabs()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var root = new GameObject("TriggerSphere");
            try
            {
                root.transform.localScale = Vector3.one;
                var sphere = root.AddComponent<SphereCollider>();
                sphere.radius = 0.5f; sphere.isTrigger = true;
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true; body.useGravity = false;
                root.AddComponent<AreaTrigger>();
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/TriggerSphere.prefab");
            }
            finally { Object.DestroyImmediate(root); }
            CopyVisual("Assets/_Project/_Scripts/Player/Combat/Skill/boltEffect.prefab", "LightningBolt");
            CopyVisual("Assets/_Project/_Scripts/Player/Combat/Skill/groundEffect.prefab", "LightningGround");
            AssetDatabase.SaveAssets();
        }
        private static void CopyVisual(string source, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if (prefab == null) throw new FileNotFoundException(source);
            var instance = Object.Instantiate(prefab);
            try
            {
                instance.name = name;
                // 원본 VFX에 남아 있는 삭제된 스크립트 참조만 제거한다. Renderer/ParticleSystem은 보존한다.
                foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                PrefabUtility.SaveAsPrefabAsset(instance, Folder + "/" + name + ".prefab");
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
