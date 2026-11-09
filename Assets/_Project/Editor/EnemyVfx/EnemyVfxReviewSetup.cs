using System.Linq;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    public static class EnemyVfxReviewSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/EnemyVfxReview.unity";
        [MenuItem("UJam/Enemies/Open VFX Review")]
        public static void Open() => EditorSceneManager.OpenScene(ScenePath);

        public static void Build()
        {
            Scene previous = SceneManager.GetActiveScene();
            NewSceneMode mode = Application.isBatchMode || string.IsNullOrEmpty(previous.path) && !previous.isDirty
                ? NewSceneMode.Single : NewSceneMode.Additive;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            SceneManager.SetActiveScene(scene);
            var review = new GameObject("Enemy VFX Review").AddComponent<EnemyVfxReview>();
            review.Prefabs = EnemyVfxBuild.Names.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/" + name + ".prefab")).ToArray();
            var grid = new GameObject("Review Grid").AddComponent<GridSystem>();
            grid.CellWidth = grid.CellHeight = 2; grid.ColumnCount = 16; grid.RowCount = 100;
            grid.Origin = new Vector3(-16, 0, -20); grid.BaseCoreRow = 99;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            VfxAuthoring.Ensure("Assets/_Project/Art/EnemyVfx/Review/Floor.mat");
            Material floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/EnemyVfx/Review/Floor.mat");
            if (floor == null) { floor = new Material(lit); AssetDatabase.CreateAsset(floor, "Assets/_Project/Art/EnemyVfx/Review/Floor.mat"); }
            floor.color = new Color(.1f, .14f, .17f); floor.SetFloat("_Smoothness", .12f);
            GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stage.name = "Matte Review Stage"; stage.transform.position = new Vector3(0, -.1f, 2);
            stage.transform.localScale = new Vector3(30, .2f, 30); stage.GetComponent<Renderer>().sharedMaterial = floor;
            GameObject target = new GameObject("Damage Target");
            review.Target = target.AddComponent<EnemyViewerTarget>();
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Target Marker"; marker.transform.SetParent(target.transform, false);
            marker.transform.localPosition = Vector3.up * .55f; marker.transform.localScale = new Vector3(.65f, .55f, .65f);
            marker.GetComponent<Renderer>().sharedMaterial = VfxAuthoring.Material("Review/Target", new Color(.83f, .5f, .18f));
            var camera = new GameObject("Review Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(9, 11, 15);
            camera.transform.LookAt(new Vector3(-1, 1.4f, 2.3f));
            camera.orthographic = true; camera.orthographicSize = 6.7f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .052f, .07f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 150; camera.allowHDR = true;
            review.ReviewCamera = camera;
            var light = new GameObject("Key Light").AddComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(48, -30, 0); light.intensity = 1.4f;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.55f, .61f, .7f);
            RenderSettings.fog = false;
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (mode == NewSceneMode.Additive) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            AssetDatabase.SaveAssets();
        }
    }
}
