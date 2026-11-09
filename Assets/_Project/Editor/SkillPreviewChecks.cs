using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ujam.Runtime.Item;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    public static class SkillPreviewChecks
    {
        private const string Folder = "Assets/_Project/Prefabs/Player/Skill/Preview/";

        [MenuItem("Tools/Skills/Check Skill Previews and Capture")]
        public static void Run()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Run Preview checks in Edit mode.");
            CheckMetadata();
            GameObject circle = CheckPrefab("Circle");
            GameObject rectangle = CheckPrefab("Rectangle");
            CheckPlacement(circle, rectangle);
            CheckManager(circle, rectangle);
            CheckSceneReferences(circle, rectangle);
            Capture(circle, rectangle);
            Debug.Log("[SkillPreviewChecks] Item metadata, shape switching, instant casting, missing-prefab guard, scale, heading, surface placement and scene references passed.");
        }

        private static void CheckMetadata()
        {
            string[] aimingItems = { "1", "2", "3", "4", "5", "7", "10", "11", "12" };
            Require(ItemCatalog.IDs.Count() == 30, "Catalog must retain all 30 item definitions.");
            foreach (string id in ItemCatalog.IDs)
            {
                ItemMeta meta = ItemCatalog.GetMeta(id);
                ItemPreviewType expected = id == "10" ? ItemPreviewType.Rectangle :
                    aimingItems.Contains(id) ? ItemPreviewType.Circle : ItemPreviewType.None;
                Require(meta.Preview.Type == expected && Near(meta.Preview.Direction, Vector3.forward),
                    "Catalog Preview migration changed the cast shape or default direction for item " + id);
            }
            Require(Near(ItemCatalog.GetMeta("11").Preview.Scale, new Vector2(8, 8)) &&
                    Near(ItemCatalog.GetMeta("10").Preview.Scale, new Vector2(10, 2)),
                "Catalog scale migration must preserve current circle and rectangle footprints.");
            var uniform = new ItemPreview(ItemPreviewType.Circle, 3);
            var rectangle = new ItemPreview(ItemPreviewType.Rectangle, 2, 5, new Vector3(20, 7, 0));
            Require(Near(uniform.Scale, new Vector2(3, 3)) && Near(uniform.Direction, Vector3.forward),
                "One scale must apply to X/Z and omitted direction must be +Z.");
            Require(Near(rectangle.Scale, new Vector2(2, 5)) && Near(rectangle.Direction, Vector3.right),
                "Two scales must map to X/Z and direction must normalize in the XZ plane.");
            Require(Meta(default).CastType == ItemCastType.Instant && Meta(uniform).CastType == ItemCastType.Normal &&
                    Meta(rectangle).CastType == ItemCastType.Normal && Near(Meta(default).Preview.Direction, Vector3.forward),
                "None must determine Instant, other shapes Normal, and a default Meta must retain +Z direction.");
            Require(Near(new ItemPreview(ItemPreviewType.Rectangle, 1, Vector3.back * float.MaxValue).Direction, Vector3.back),
                "A large finite direction must normalize without overflow.");
            Reject(() => new ItemPreview((ItemPreviewType)99));
            Reject(() => new ItemPreview(ItemPreviewType.Circle, 0));
            Reject(() => new ItemPreview(ItemPreviewType.Circle, -1));
            Reject(() => new ItemPreview(ItemPreviewType.Rectangle, float.NaN, 1));
            Reject(() => new ItemPreview(ItemPreviewType.Rectangle, 1, float.PositiveInfinity));
            Reject(() => new ItemPreview(ItemPreviewType.Circle, direction: Vector3.zero));
            Reject(() => new ItemPreview(ItemPreviewType.Circle, direction: Vector3.up));
            Reject(() => new ItemPreview(ItemPreviewType.Circle, direction: new Vector3(float.NaN, 0, 1)));
        }

        private static ItemMeta Meta(ItemPreview preview) => new ItemMeta("Preview check", "", 0, 0, 1,
            ItemKind.Active, ItemTarget.Player, preview: preview);

        private static GameObject CheckPrefab(string shape)
        {
            string texturePath = Folder + "Img/SkillPreview" + shape + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            Require(texture != null && importer != null, shape + " texture is missing.");
            Require(importer.textureType == TextureImporterType.Default && importer.sRGBTexture &&
                    importer.alphaSource == TextureImporterAlphaSource.FromInput && importer.alphaIsTransparency &&
                    importer.wrapMode == TextureWrapMode.Clamp,
                shape + " texture must use sRGB, source transparency and clamped edges.");

            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Materials/SkillPreview" + shape + ".mat");
            Require(material != null && material.shader != null && material.shader.name == "UJam/SkillPreview" &&
                    !ShaderUtil.ShaderHasError(material.shader), shape + " indicator shader is missing or has compile errors.");
            Require(material.GetTexture("_BaseMap") == texture, shape + " material does not reference its texture.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "SkillPreview" + shape + ".prefab");
            Require(prefab != null && prefab.GetComponent<PlayerSkillPreview>() != null,
                shape + " prefab must have PlayerSkillPreview on its root.");
            Require(Near(prefab.transform.localScale, shape == "Circle" ? Vector3.one : new Vector3(1, 1, 3)),
                shape + " prefab does not have its intended default dimensions.");
            Require(prefab.GetComponentsInChildren<Collider>(true).Length == 0, shape + " indicator must not intercept aiming raycasts.");
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Require(renderers.Length == 1 && renderers[0].sharedMaterial == material &&
                    renderers[0].shadowCastingMode == ShadowCastingMode.Off && !renderers[0].receiveShadows,
                shape + " renderer must use its indicator material without shadows.");
            return prefab;
        }

        private static void CheckPlacement(GameObject circlePrefab, GameObject rectanglePrefab)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var circle = (GameObject)PrefabUtility.InstantiatePrefab(circlePrefab, scene);
                var rectangle = (GameObject)PrefabUtility.InstantiatePrefab(rectanglePrefab, scene);
                PlayerSkillPreview preview = circle.GetComponent<PlayerSkillPreview>();
                circle.transform.localScale = new Vector3(1, 2, 1);
                preview.Hide();
                Require(!circle.activeSelf, "Hide did not deactivate the preview.");
                preview.Show(new Vector2(3.5f, 3.5f), Vector3.right);
                Require(circle.activeSelf && Near(circle.transform.localScale, new Vector3(3.5f, 2, 3.5f)),
                    "Preview must apply the supplied X/Z scales directly while preserving Y scale.");
                Vector3 point = new Vector3(4, 7, -2);
                Vector3 normal = new Vector3(0, 4, 3).normalized;
                preview.SetPosition(point, normal * 5);
                Require(Near(circle.transform.position, point + normal * .03f) && Near(circle.transform.up, normal) &&
                        Near(circle.transform.forward, Quaternion.FromToRotation(Vector3.up, normal) * Vector3.right),
                    "Preview must align with the normalized surface and sit 0.03 units above it.");
                preview.SetPosition(point, Vector3.zero);
                Require(Near(circle.transform.position, point + Vector3.up * .03f) && Near(circle.transform.up, Vector3.up) &&
                        Near(circle.transform.forward, Vector3.right),
                    "A zero surface normal must fall back to an upward offset.");

                PlayerSkillPreview rectangularPreview = rectangle.GetComponent<PlayerSkillPreview>();
                rectangle.transform.localScale = new Vector3(1, 2, 3);
                rectangularPreview.Hide();
                rectangularPreview.Show(new Vector2(1.8f, 5), Vector3.forward);
                Require(rectangle.activeSelf && Near(rectangle.transform.localScale, new Vector3(1.8f, 2, 5)),
                    "Rectangle must overwrite its default scale using metadata X/Z scales.");
                rectangularPreview.Show(Vector2.one, Vector3.left);
                rectangularPreview.SetPosition(point, Vector3.up);
                rectangularPreview.Show(Vector2.one, Vector3.forward);
                rectangularPreview.SetPosition(point, Vector3.up);
                Require(Near(rectangle.transform.forward, Vector3.forward), "Reused Preview retained the previous item's heading.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void CheckManager(GameObject circlePrefab, GameObject rectanglePrefab)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var host = (GameObject)PrefabUtility.InstantiatePrefab(circlePrefab, scene);
            host.name = "SkillPreviewChecks";
            var player = host.AddComponent<PlayerStatus>();
            typeof(PlayerStatus).GetField("_currentHealth", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 100f);
            var manager = host.AddComponent<PlayerSkillManager>();
            var circle = (GameObject)PrefabUtility.InstantiatePrefab(circlePrefab, scene);
            var rectangle = (GameObject)PrefabUtility.InstantiatePrefab(rectanglePrefab, scene);
            var circleView = circle.GetComponent<PlayerSkillPreview>();
            var rectangleView = rectangle.GetComponent<PlayerSkillPreview>();
            circleView.Hide(); rectangleView.Hide();
            Field(manager, "circlePreview").SetValue(manager, circleView);
            Field(manager, "rectanglePreview").SetValue(manager, rectangleView);
            var circleItem = Owned("preview-circle", new ItemPreview(ItemPreviewType.Circle, 3, Vector3.right), player);
            var rectangleItem = Owned("preview-rectangle", new ItemPreview(ItemPreviewType.Rectangle, 2, 5), player);
            var instantItem = Owned("preview-instant", default, player);
            Action<ItemEvent> execute = signal => { if (signal.Player == player) signal.SkillItem.Runtime.Execute(signal); };
            EventManager.Instance.Subscribe(ItemTrigger.SkillUse, execute);
            try
            {
                Require(manager.Equip(0, circleItem) && manager.Equip(1, rectangleItem), "Test items could not enter the skill slots.");
                manager.TryUse(0);
                Require(manager.IsAiming && circle.activeSelf && !rectangle.activeSelf &&
                        Near(circle.transform.localScale, new Vector3(3, 1, 3)) && Near(circle.transform.forward, Vector3.right),
                    "Circle metadata did not reach the active Preview.");
                manager.TryUse(1);
                Require(manager.IsAiming && !circle.activeSelf && rectangle.activeSelf &&
                        Near(rectangle.transform.localScale, new Vector3(2, 1, 5)) && Near(rectangle.transform.forward, Vector3.forward),
                    "Switching items must hide the previous shape and apply the new scale/heading.");
                Require(((CheckEffect)rectangleItem.Runtime.Effect).Calls == 0, "A configured Preview cast before confirmation.");
                manager.UnEquip(0);
                Require(manager.Equip(0, instantItem), "None test item could not enter its slot.");
                manager.TryUse(0);
                Require(!manager.IsAiming && !circle.activeSelf && !rectangle.activeSelf &&
                        ((CheckEffect)instantItem.Runtime.Effect).Calls == 1,
                    "None must hide an existing Preview and execute immediately.");

                Field(manager, "rectanglePreview").SetValue(manager, null);
                var expected = new MissingPreviewLog { Original = Debug.unityLogger.logHandler };
                Debug.unityLogger.logHandler = expected;
                try { manager.TryUse(1); }
                finally { Debug.unityLogger.logHandler = expected.Original; }
                Require(expected.Seen && !manager.IsAiming && ((CheckEffect)rectangleItem.Runtime.Effect).Calls == 0,
                    "A missing shape prefab must report an error and must not silently cast instantly.");
            }
            finally
            {
                EventManager.Instance.Unsubscribe(ItemTrigger.SkillUse, execute);
                Object.DestroyImmediate(circle);
                Object.DestroyImmediate(rectangle);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // Editor 검사에서는 전역 RuntimeTimer 생성 없이 이 검사 소유자의 이벤트만 연결한다.
        private static Item Owned(string id, ItemPreview preview, PlayerStatus player)
        {
            var item = new Item(id, Meta(preview), new ItemRuntime(ItemTrigger.SkillUse, new CheckEffect()));
            typeof(ItemRuntime).GetProperty(nameof(ItemRuntime.Owner)).SetValue(item.Runtime, player);
            return item;
        }

        private sealed class CheckEffect : ItemEffect
        {
            public int Calls;
            public override void Execute(ItemUseContext context) => Calls++;
        }

        private sealed class MissingPreviewLog : ILogHandler
        {
            public ILogHandler Original;
            public bool Seen;
            public void LogFormat(LogType type, Object context, string format, params object[] args)
            {
                if (type == LogType.Error && string.Format(format, args).Contains("Preview 프리팹이 연결되지 않았습니다")) Seen = true;
                else Original.LogFormat(type, context, format, args);
            }
            public void LogException(Exception exception, Object context) => Original.LogException(exception, context);
        }

        private static FieldInfo Field(PlayerSkillManager manager, string name) =>
            manager.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        private static void CheckSceneReferences(GameObject circle, GameObject rectangle)
        {
            foreach (string name in new[] { "_MainScene 1", "_MainScene 2", "LCH_Scene", "PJS_Scene" })
            {
                var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/" + name + ".unity");
                try
                {
                    var managers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerCombatManager>(true)).ToArray();
                    Require(managers.Length > 0 && managers.All(manager => manager.PlayerSkillPreviewPrefab == circle),
                        name + " must reference the existing circle indicator prefab.");
                    var skills = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerSkillManager>(true)).ToArray();
                    Require(skills.Length > 0 && skills.All(skill =>
                        new SerializedObject(skill).FindProperty("rectanglePreviewPrefab").objectReferenceValue == rectangle),
                        name + " must reference the rectangle indicator prefab.");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }

        private static void Capture(GameObject circlePrefab, GameObject rectanglePrefab)
        {
            var preview = new PreviewRenderUtility();
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/LCH_Scene.unity");
            Texture2D frame = null;
            try
            {
                Transform map = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(transform => transform.name == "Map");
                Require(map != null, "LCH Map is missing.");
                Collider sourceGround = map.GetComponentsInChildren<Collider>()
                    .FirstOrDefault(candidate => candidate.enabled && candidate.gameObject.layer == 3 && candidate.GetComponent<Renderer>() != null);
                Require(sourceGround != null, "LCH Map must have a visible Ground collider.");
                GameObject ground = Object.Instantiate(sourceGround.gameObject, preview.camera.transform, true);
                ground.transform.SetParent(null, true);
                preview.AddSingleGO(ground);
                var collider = ground.GetComponent<Collider>();
                Physics.SyncTransforms();
                Vector3 center = collider.bounds.center;
                Require(collider.Raycast(new Ray(center + new Vector3(-2.5f, 10, 0), Vector3.down), out RaycastHit circleHit, 30),
                    "Circle preview did not hit LCH Ground.");
                Require(collider.Raycast(new Ray(center + new Vector3(2, 10, 0), Vector3.down), out RaycastHit rectangleHit, 30),
                    "Rectangle preview did not hit LCH Ground.");
                var circle = (GameObject)PrefabUtility.InstantiatePrefab(circlePrefab, preview.camera.scene);
                preview.AddSingleGO(circle);
                PlayerSkillPreview circleIndicator = circle.GetComponent<PlayerSkillPreview>();
                var circleSettings = new ItemPreview(ItemPreviewType.Circle, 3.5f);
                circleIndicator.Show(circleSettings.Scale, circleSettings.Direction);
                circleIndicator.SetPosition(circleHit.point, circleHit.normal);
                var rectangle = (GameObject)PrefabUtility.InstantiatePrefab(rectanglePrefab, preview.camera.scene);
                preview.AddSingleGO(rectangle);
                PlayerSkillPreview rectangleIndicator = rectangle.GetComponent<PlayerSkillPreview>();
                var rectangleSettings = new ItemPreview(ItemPreviewType.Rectangle, 1.8f, 5, Vector3.right);
                rectangleIndicator.Show(rectangleSettings.Scale, rectangleSettings.Direction);
                rectangleIndicator.SetPosition(rectangleHit.point, rectangleHit.normal);
                Debug.Log("[SkillPreviewChecks] LCH Ground " + sourceGround.name + ", material " +
                    ground.GetComponent<Renderer>().sharedMaterial.name + ", circle surface " + circleHit.point +
                    ", rectangle surface " + rectangleHit.point + ". Original scene preserved.");

                preview.camera.orthographic = true;
                preview.camera.orthographicSize = 4.1f;
                preview.camera.nearClipPlane = .01f;
                preview.camera.farClipPlane = 50;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(.04f, .06f, .05f);
                preview.camera.transform.position = center + new Vector3(0, 10, -7);
                preview.camera.transform.LookAt(center);
                preview.lights[0].intensity = 1.2f;
                preview.lights[0].transform.rotation = Quaternion.Euler(55, -30, 0);
                preview.lights[1].intensity = .5f;
                preview.ambientColor = new Color(.5f, .55f, .5f);
                preview.BeginStaticPreview(new Rect(0, 0, 1280, 800));
                preview.Render(true);
                frame = preview.EndStaticPreview();
                File.WriteAllBytes("Library/SkillPreviewCapture.png", frame.EncodeToPNG());
            }
            finally
            {
                if (frame != null) Object.DestroyImmediate(frame);
                preview.Cleanup();
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static bool Near(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < .000001f;
        private static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < .000001f;
        private static void Reject(Action action)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            throw new InvalidOperationException("Invalid Preview metadata was accepted.");
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
