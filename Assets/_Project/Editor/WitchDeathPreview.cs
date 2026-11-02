using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UJam.Runtime.Enemy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    [InitializeOnLoad]
    public static class WitchDeathPreview
    {
        private const string Request = "Library/PreviewWitchDeath.request";
        private const string Output = "Library/WitchDeathPreview";

        static WitchDeathPreview() { EditorApplication.update += RequestedCapture; }

        private static void RequestedCapture()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { File.Delete(Request); }
            catch (IOException) { return; }
            try { Capture(); }
            catch (Exception exception)
            {
                File.WriteAllText(Output + ".error.txt", exception.ToString());
                Debug.LogException(exception);
            }
        }

        [MenuItem("Tools/Enemies/Preview Witch Death")]
        public static void Capture()
        {
            const int width = 440, height = 480;
            float[] times = { 0f, .35f, .8f, 2.45f, 2.8f };
            var preview = new PreviewRenderUtility();
            var sheet = new Texture2D(width * times.Length, height, TextureFormat.RGB24, false);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/Witch.prefab");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.camera.scene);
                preview.AddSingleGO(root);
                Transform model = root.transform.Find("Model");
                var animator = model.GetComponent<Animator>();
                AnimationClip death = animator.runtimeAnimatorController.animationClips
                    .First(clip => clip.name == "Death" || clip.name.EndsWith("_Death", StringComparison.Ordinal));
                animator.enabled = false;
                var effect = root.GetComponent<WitchDeathVfx>();
                MethodInfo apply = typeof(WitchDeathVfx).GetMethod("ApplyTime", BindingFlags.Instance | BindingFlags.NonPublic);
                var particles = root.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].useAutoRandomSeed = false;
                    particles[i].randomSeed = (uint)(i + 123);
                }
                preview.camera.orthographic = true;
                preview.camera.orthographicSize = 2.55f;
                preview.camera.nearClipPlane = .01f;
                preview.camera.farClipPlane = 50f;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(.055f, .065f, .085f);
                preview.camera.transform.position = new Vector3(5f, 3.5f, 7f);
                preview.camera.transform.LookAt(new Vector3(0, 2.05f, 0));
                preview.lights[0].intensity = 1.3f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35, 215, 0);
                preview.lights[1].intensity = .8f;
                preview.lights[1].transform.rotation = Quaternion.Euler(20, 130, 0);
                preview.ambientColor = new Color(.5f, .5f, .55f);
                Directory.CreateDirectory(Output);
                for (int index = 0; index < times.Length; index++)
                {
                    death.SampleAnimation(model.gameObject, 0);
                    effect.ResetEffect();
                    apply.Invoke(effect, new object[] { 0f });
                    float elapsed = 0;
                    while (elapsed < times[index])
                    {
                        float delta = Mathf.Min(1f / 60f, times[index] - elapsed);
                        elapsed += delta;
                        death.SampleAnimation(model.gameObject, elapsed);
                        apply.Invoke(effect, new object[] { elapsed });
                        foreach (ParticleSystem particle in particles) particle.Simulate(delta, false, false, true);
                    }
                    // Multiple preview renders share one editor frame; bake each pose to avoid cached skinning.
                    var bakedObjects = new System.Collections.Generic.List<GameObject>();
                    var bakedMeshes = new System.Collections.Generic.List<Mesh>();
                    var enabledSkins = new System.Collections.Generic.List<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        if (!skin.enabled) continue;
                        var mesh = new Mesh();
                        skin.BakeMesh(mesh);
                        var baked = new GameObject("Preview Pose");
                        baked.transform.SetParent(skin.transform, false);
                        baked.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var renderer = baked.AddComponent<MeshRenderer>();
                        renderer.sharedMaterials = skin.sharedMaterials;
                        var block = new MaterialPropertyBlock();
                        skin.GetPropertyBlock(block);
                        renderer.SetPropertyBlock(block);
                        skin.enabled = false;
                        enabledSkins.Add(skin);
                        bakedObjects.Add(baked);
                        bakedMeshes.Add(mesh);
                    }
                    preview.BeginStaticPreview(new Rect(0, 0, width, height));
                    preview.camera.backgroundColor = new Color(.055f, .065f, .085f);
                    preview.Render(true);
                    Texture2D frame = preview.EndStaticPreview();
                    try
                    {
                        sheet.SetPixels(index * width, 0, width, height, frame.GetPixels());
                        File.WriteAllBytes(Output + "/Death_" + times[index].ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "s.png", frame.EncodeToPNG());
                    }
                    finally
                    {
                        Object.DestroyImmediate(frame);
                        foreach (SkinnedMeshRenderer skin in enabledSkins) skin.enabled = true;
                        foreach (GameObject baked in bakedObjects) Object.DestroyImmediate(baked);
                        foreach (Mesh mesh in bakedMeshes) Object.DestroyImmediate(mesh);
                    }
                }
                sheet.Apply();
                File.WriteAllBytes(Output + ".png", sheet.EncodeToPNG());
                Debug.Log("[WitchDeathPreview] Captured Death at0/0.35/0.8/2.45/2.8seconds.");
            }
            finally { Object.DestroyImmediate(sheet); preview.Cleanup(); }
        }
    }
}
