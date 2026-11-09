using System;
using System.IO;
using System.Linq;
using UJam.Runtime.Combat;
using UJam.Runtime.Defense;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    [InitializeOnLoad]
    public static class EnemyViewerSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PJS_Scene.unity";
        private const string Request = "Library/SetupEnemyViewer.request";
        private const string CameraRequest = "Library/MatchEnemyViewerCamera.request";
        private const string ReportPath = "Library/EnemyViewerSceneSetupReport.json";
        private static readonly string[] Names = { "Golem", "Big_Golem", "Eyebeast", "Sniper_Eyebeast", "Witch", "Skeleton", "Slime", "Rollbug" };

        [Serializable]
        private sealed class Report
        {
            public string result, error, scene;
            public int assignedEnemies;
            public Vector3 spawnPosition, targetPosition;
            public bool targetCanTakeDamage, modelInCamera, gridConfigured;
        }

        static EnemyViewerSceneSetup() => EditorApplication.update += Poll;

        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(CameraRequest))
            {
                File.Delete(CameraRequest);
                try { MatchGameplayCamera(); }
                catch (Exception error)
                {
                    File.WriteAllText("Library/EnemyViewerCameraMatchReport.json", JsonUtility.ToJson(new CameraMatchReport { result = "failed", error = error.ToString() }, true));
                    Debug.LogException(error);
                }
                return;
            }
            if (!File.Exists(Request)) return;
            try { File.Delete(Request); }
            catch (IOException) { return; }
            try { Apply(); }
            catch (Exception error) { Debug.LogException(error); }
        }

        [MenuItem("UJam/Enemies/Setup PJS FSM Viewer")]
        public static void Apply()
        {
            var report = new Report { result = "building", scene = ScenePath };
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play mode before updating the viewer scene.");
                for (int index = 0; index < SceneManager.sceneCount; index++)
                    Require(!SceneManager.GetSceneAt(index).isDirty, "Save open scenes before setting up the enemy viewer.");
                Scene scene = SceneManager.GetSceneByPath(ScenePath);
                if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                SceneManager.SetActiveScene(scene);
                EnemyAnimationViewer viewer = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyAnimationViewer>(true)).SingleOrDefault();
                if (viewer == null) viewer = new GameObject("Enemy Animation Viewer").AddComponent<EnemyAnimationViewer>();
                viewer.gameObject.SetActive(true);
                viewer.enabled = true;
                EnemyBase[] prefabs = Names.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/" + name + ".prefab")?.GetComponent<EnemyBase>()).ToArray();
                Require(prefabs.All(enemy => enemy != null), "All eight production enemy prefabs must exist before viewer setup.");

                Transform spawn = Child(viewer.transform, "Spawn Point");
                spawn.position = new Vector3(0, 1, 18);
                spawn.rotation = Quaternion.identity;
                Transform targetRoot = Child(viewer.transform, "Viewer Target");
                targetRoot.position = new Vector3(6, 1, 30);
                targetRoot.rotation = Quaternion.identity;
                targetRoot.localScale = Vector3.one;
                targetRoot.gameObject.layer = LayerMask.NameToLayer("Tower");
                EnemyViewerTarget target = targetRoot.GetComponent<EnemyViewerTarget>();
                if (target == null) target = targetRoot.gameObject.AddComponent<EnemyViewerTarget>();
                target.ResetTarget();
                float hp = target.HP;
                Require(target.TakeDamage(new DamageInfo(25)) == 25 && target.HP == hp - 25 && target.HitCount == 1,
                    "The viewer target did not record actual damage.");
                Require(target.TakeDamage(new DamageInfo(float.NaN)) == 0 && target.HitCount == 1,
                    "The viewer target accepted invalid damage.");
                target.ResetTarget();
                BoxCollider targetCollider = targetRoot.GetComponent<BoxCollider>();
                if (targetCollider == null) targetCollider = targetRoot.gameObject.AddComponent<BoxCollider>();
                targetCollider.center = Vector3.up * .8f;
                targetCollider.size = new Vector3(1.6f, 1.6f, 1.6f);

                Material floorMaterial = Material("EnemyViewerStage", new Color(.16f, .2f, .25f));
                Material targetMaterial = Material("EnemyViewerTarget", new Color(.9f, .56f, .18f));
                Transform marker = Primitive(targetRoot, "Target Marker", PrimitiveType.Cylinder, targetMaterial);
                marker.localPosition = Vector3.up * .7f;
                marker.localScale = new Vector3(1.35f, .7f, 1.35f);
                marker.gameObject.layer = targetRoot.gameObject.layer;
                Transform stage = Child(viewer.transform, "Preview Stage");
                Transform floor = Primitive(stage, "Floor", PrimitiveType.Cube, floorMaterial);
                floor.position = new Vector3(0, .94f, 24);
                floor.localScale = new Vector3(34, .1f, 28);
                floor.gameObject.layer = LayerMask.NameToLayer("Ground");
                foreach (int side in new[] { -1, 1 })
                {
                    Transform wall = Primitive(stage, side < 0 ? "Wall -16" : "Wall +16", PrimitiveType.Cube, floorMaterial);
                    wall.position = new Vector3(side * 16, 1.5f, 24);
                    wall.localScale = new Vector3(.12f, 1, 28);
                }

                Transform cameraRoot = Child(viewer.transform, "Preview Camera");
                Camera camera = cameraRoot.GetComponent<Camera>();
                if (camera == null) camera = cameraRoot.gameObject.AddComponent<Camera>();
                camera.orthographicSize = 12;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 200;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.055f, .07f, .09f);
                camera.depth = 100;
                camera.enabled = false;
                AlignGameplayCamera(viewer);
                Transform lightRoot = Child(viewer.transform, "Preview Light");
                lightRoot.rotation = Quaternion.Euler(48, -35, 0);
                Light light = lightRoot.GetComponent<Light>();
                if (light == null) light = lightRoot.gameObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.enabled = false;

                GridSystem grid = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GridSystem>(true)).FirstOrDefault();
                if (grid == null)
                {
                    grid = new GameObject("Viewer Grid").AddComponent<GridSystem>();
                    grid.CellWidth = grid.CellHeight = 2;
                    grid.ColumnCount = 16; grid.RowCount = 50;
                    grid.Origin = new Vector3(-15, 1, -100);
                    grid.BaseCoreRow = 49;
                }
                Require(grid.CellWidth > 0 && grid.CellHeight > 0 && grid.ColumnCount > 0 && grid.RowCount > 0, "The viewer requires a valid GridSystem.");
                var serialized = new SerializedObject(viewer);
                SerializedProperty enemies = serialized.FindProperty("_enemyPrefabs");
                enemies.arraySize = prefabs.Length;
                for (int index = 0; index < prefabs.Length; index++) enemies.GetArrayElementAtIndex(index).objectReferenceValue = prefabs[index];
                serialized.FindProperty("model").objectReferenceValue = prefabs[0].gameObject;
                serialized.FindProperty("_spawnPoint").objectReferenceValue = spawn;
                serialized.FindProperty("_target").objectReferenceValue = target.gameObject;
                serialized.FindProperty("_testMode").boolValue = true;
                serialized.FindProperty("_previewCamera").objectReferenceValue = camera;
                serialized.FindProperty("_previewLight").objectReferenceValue = light;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Vector3 viewport = camera.WorldToViewportPoint(spawn.position + Vector3.up * 2);
                Require(viewport.z > 0 && viewport.x > .25f && viewport.x < .95f && viewport.y > .1f && viewport.y < .9f, "The selected enemy is outside the preview camera.");
                foreach (Transform child in viewer.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "The viewer contains a missing script.");
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene), "Could not save PJS_Scene viewer setup.");
                AssetDatabase.SaveAssets();
                report.assignedEnemies = prefabs.Length;
                report.spawnPosition = spawn.position;
                report.targetPosition = targetRoot.position;
                report.targetCanTakeDamage = target.GetComponent<IDamageable>() != null;
                report.modelInCamera = report.gridConfigured = true;
                report.result = "passed";
                Selection.activeGameObject = viewer.gameObject;
                Debug.Log("[EnemyViewerSceneSetup] PJS_Scene now has eight FSM-controlled enemies, a damageable target and a runtime preview camera.");
            }
            catch (Exception error) { report.result = "failed"; report.error = error.ToString(); throw; }
            finally { File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true)); }
        }

        [MenuItem("UJam/Enemies/Match PJS Preview to Gameplay Camera")]
        public static void MatchGameplayCamera()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play mode before matching the preview camera.");
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                for (int index = 0; index < SceneManager.sceneCount; index++)
                    Require(!SceneManager.GetSceneAt(index).isDirty, "Save open scenes before opening PJS_Scene.");
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            EnemyAnimationViewer viewer = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnemyAnimationViewer>(true)).Single();
            File.Copy(ScenePath, "Library/PJS_Scene.BeforeCameraMatch.unity.txt", true);
            AlignGameplayCamera(viewer);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Could not save the matched preview camera.");
        }

        private static void AlignGameplayCamera(EnemyAnimationViewer viewer)
        {
            GameObject[] roots = viewer.gameObject.scene.GetRootGameObjects();
            Transform head = roots.Single(root => root.name == "PlayerTurret").transform.Find("CameraHead");
            BaseCore core = roots.Single(root => root.name == "Map").GetComponentsInChildren<BaseCore>(true).Single();
            Transform marker = viewer.transform.Find("Viewer Target/Target Marker");
            Camera preview = viewer.transform.Find("Preview Camera").GetComponent<Camera>();
            Require(head != null && marker != null && preview != null, "Gameplay camera, target marker and preview camera are required.");
            Camera gameplay = head.GetComponentInChildren<Camera>(true);
            BoxCollider coreCollider = core.GetComponent<BoxCollider>();
            MeshFilter markerMesh = marker.GetComponent<MeshFilter>();
            Require(gameplay != null && coreCollider != null && markerMesh != null && markerMesh.sharedMesh != null,
                "Matching requires the gameplay camera, BaseCore collider and target marker geometry.");
            Require(Vector3.Distance(gameplay.transform.position, head.position) < .0001f
                && Quaternion.Angle(gameplay.transform.rotation, head.rotation) < .001f,
                "The gameplay Camera must share its CameraHead pose.");
            Matrix4x4 unchangedMarker = marker.localToWorldMatrix;
            Undo.RecordObjects(new Object[] { preview.transform, preview }, "Match viewer camera to gameplay");
            Vector3 coreCenter = core.transform.TransformPoint(coreCollider.center);
            Vector3 markerCenter = marker.TransformPoint(markerMesh.sharedMesh.bounds.center);
            // Preserve the world-space camera-to-core offset; LookAt would change the gameplay angle.
            preview.transform.SetPositionAndRotation(markerCenter + head.position - coreCenter, head.rotation);
            preview.orthographic = gameplay.orthographic;
            preview.fieldOfView = gameplay.fieldOfView;
            float offsetError = Vector3.Distance(preview.transform.position - markerCenter, head.position - coreCenter);
            float viewportError = Vector3.Distance(preview.WorldToViewportPoint(markerCenter), gameplay.WorldToViewportPoint(coreCenter));
            Require(offsetError < .0001f && Quaternion.Angle(preview.transform.rotation, head.rotation) < .001f
                && viewportError < .0001f && marker.localToWorldMatrix == unchangedMarker,
                "The preview must match the gameplay camera relationship without moving the target marker.");
            File.WriteAllText("Library/EnemyViewerCameraMatchReport.json", JsonUtility.ToJson(new CameraMatchReport
            {
                result = "passed", cameraHeadPosition = head.position, baseCoreCenter = coreCenter,
                targetMarkerCenter = markerCenter, previewPosition = preview.transform.position,
                previewEuler = preview.transform.eulerAngles, offsetError = offsetError, viewportError = viewportError,
                markerUnchanged = true, perspective = !preview.orthographic, fieldOfView = preview.fieldOfView
            }, true));
            Debug.Log("[EnemyViewerSceneSetup] Preview camera matched CameraHead/BaseCore center; marker unchanged.");
        }

        [Serializable] private sealed class CameraMatchReport
        {
            public string result, error;
            public Vector3 cameraHeadPosition, baseCoreCenter, targetMarkerCenter, previewPosition, previewEuler;
            public float offsetError, viewportError, fieldOfView;
            public bool markerUnchanged, perspective;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Transform Primitive(Transform parent, string name, PrimitiveType type, Material material)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = GameObject.CreatePrimitive(type).transform;
                child.name = name;
                child.SetParent(parent, false);
            }
            Collider collider = child.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            child.GetComponent<Renderer>().sharedMaterial = material;
            return child;
        }

        private static Material Material(string name, Color color)
        {
            string path = "Assets/_Project/Art/EnemyChar/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
