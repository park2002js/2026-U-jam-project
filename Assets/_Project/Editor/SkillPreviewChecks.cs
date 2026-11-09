using System;
using System.IO;
using System.Linq;
using UJam.Runtime.Player;
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
            GameObject circle = CheckPrefab("Circle");
            GameObject rectangle = CheckPrefab("Rectangle");
            CheckPlacement(circle, rectangle);
            CheckSceneReferences(circle);
            Capture(circle, rectangle);
            Debug.Log("[SkillPreviewChecks] Texture import, materials, sizing, surface placement, scene references and preview passed.");
        }

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
                preview.Show(1.75f);
                Require(circle.activeSelf && Near(circle.transform.localScale, new Vector3(3.5f, 2, 3.5f)),
                    "Circle radius must become its diameter while preserving Y scale.");
                Vector3 point = new Vector3(4, 7, -2);
                Vector3 normal = new Vector3(0, 4, 3).normalized;
                preview.SetPosition(point, normal * 5);
                Require(Near(circle.transform.position, point + normal * .03f) && Near(circle.transform.up, normal),
                    "Preview must align with the normalized surface and sit 0.03 units above it.");
                preview.SetPosition(point, Vector3.zero);
                Require(Near(circle.transform.position, point + Vector3.up * .03f) && Near(circle.transform.up, Vector3.up),
                    "A zero surface normal must fall back to an upward offset.");

                PlayerSkillPreview rectangularPreview = rectangle.GetComponent<PlayerSkillPreview>();
                rectangle.transform.localScale = new Vector3(1, 2, 3);
                rectangularPreview.Hide();
                rectangularPreview.ShowRectangle(1.8f, 5);
                Require(rectangle.activeSelf && Near(rectangle.transform.localScale, new Vector3(1.8f, 2, 5)),
                    "Rectangle must use the requested width and length while preserving Y scale.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void CheckSceneReferences(GameObject circle)
        {
            foreach (string name in new[] { "_MainScene 1", "LCH_Scene" })
            {
                var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/" + name + ".unity");
                try
                {
                    var managers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerCombatManager>(true)).ToArray();
                    Require(managers.Length > 0 && managers.All(manager => manager.PlayerSkillPreviewPrefab == circle),
                        name + " must reference the existing circle indicator prefab.");
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
                circleIndicator.Show(1.75f);
                circleIndicator.SetPosition(circleHit.point, circleHit.normal);
                var rectangle = (GameObject)PrefabUtility.InstantiatePrefab(rectanglePrefab, preview.camera.scene);
                preview.AddSingleGO(rectangle);
                PlayerSkillPreview rectangleIndicator = rectangle.GetComponent<PlayerSkillPreview>();
                rectangleIndicator.ShowRectangle(1.8f, 5);
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

        private static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < .000001f;
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
