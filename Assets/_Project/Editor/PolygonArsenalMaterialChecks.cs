using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    public static class PolygonArsenalMaterialChecks
    {
        const string Root = "Assets/Polygon Arsenal";

        [MenuItem("Tools/Polygon Arsenal/Run Material Checks")]
        public static void Run()
        {
            var materials = AssetDatabase.FindAssets("t:Material", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).Where(path => path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                .Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();
            Require(materials.Length == 104, "Expected the 104 Polygon Arsenal materials.");
            foreach (var material in materials)
                Require(material.GetTag("RenderPipeline", false) == "UniversalPipeline", material.name + " still uses a Built-in shader.");
            var shaders = new HashSet<Shader>(materials.Select(material => material.shader));
            var references = new HashSet<string>();
            var paths = AssetDatabase.FindAssets("t:Prefab t:Scene", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (string path in paths)
            {
                string yaml = File.ReadAllText(path);
                Require(!yaml.Contains("guid: 948f4100a11a5c24981795d21301da5c") && !yaml.Contains("guid: 8b9a305e18de0c04dbd257a21cd47087"),
                    path + " still references missing Built-in post-processing scripts.");
                foreach (Match reference in Regex.Matches(yaml, @"\{fileID: 2100000, guid: ([0-9a-f]{32}), type: 2\}"))
                {
                    string guid = reference.Groups[1].Value;
                    if (references.Add(guid)) Require(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)) != null,
                        path + " references missing material " + guid);
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is ParticleSystemRenderer particles)
                    {
                        Require(particles.renderMode == ParticleSystemRenderMode.None || particles.sharedMaterial != null,
                            path + "/" + renderer.name + " has no particle material.");
                        Require(!particles.GetComponent<ParticleSystem>().trails.enabled || particles.trailMaterial != null,
                            path + "/" + renderer.name + " has no trail material.");
                    }
                    foreach (var material in renderer.sharedMaterials.Where(material => material != null)) shaders.Add(material.shader);
                }
            }
            foreach (var shader in shaders) CheckShader(shader);
            CheckCameras();
            // Unity runtime callbacks and renderer.material must be checked in Play mode.
            if (Application.isPlaying) CheckBeamUV();
            CheckPixels(materials);
            Debug.Log("[PolygonArsenalMaterialChecks] " + materials.Length + " materials, " + paths.Length +
                " prefab/scene files, " + references.Count + " material references, five camera rigs, beam UV updates and GPU color passes passed.");
        }

        static void CheckCameras()
        {
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Demo/Demo Prefabs/Cameras" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Require(paths.Length == 5, "Expected the five Polygon Arsenal camera rigs.");
            int mainCameras = 0;
            foreach (string path in paths)
                foreach (var camera in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Camera>(true))
                {
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(camera.gameObject) == 0,
                        path + "/" + camera.name + " still has a missing camera script.");
                    var data = camera.GetComponent<UniversalAdditionalCameraData>();
                    Require(data != null && data.requiresDepthTexture, path + "/" + camera.name + " has no URP camera depth texture.");
                    if (camera.name != "Main Camera") continue;
                    mainCameras++;
                    Require(camera.CompareTag("MainCamera") && data.renderPostProcessing,
                        path + " lost its main camera tag or post-processing.");
                    var volume = camera.GetComponent<Volume>();
                    Require(volume != null && volume.sharedProfile != null && volume.sharedProfile.components.Count > 0 &&
                        volume.sharedProfile.components.All(component => component != null),
                        path + " has no valid URP volume profile.");
                }
            Require(mainCameras == 5, "Expected one tagged Main Camera per Polygon Arsenal camera rig.");
        }

        static void CheckBeamUV()
        {
            foreach (bool textured in new[] { true, false })
            {
                var root = new GameObject("PolygonBeamUVCheck") { hideFlags = HideFlags.HideAndDontSave };
                root.SetActive(false);
                var template = new GameObject("BeamLine") { hideFlags = HideFlags.HideAndDontSave };
                template.SetActive(false);
                template.transform.SetParent(root.transform);
                var material = textured ? new Material(Shader.Find("Universal Render Pipeline/Unlit")) :
                    new Material(AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Main/PolySolidGlow.mat"));
                template.AddComponent<LineRenderer>().sharedMaterial = material;
                var messages = new List<string>();
                Application.LogCallback collect = (message, stack, type) => { if (type != LogType.Log) messages.Add(message); };
                try
                {
                    Require(textured || (!material.HasProperty("_MainTex") && !material.HasProperty("_BaseMap")),
                        "The textureless beam fixture unexpectedly has a main texture.");
                    var beam = root.AddComponent<PolygonArsenal.PolygonBeamStatic>();
                    beam.alwaysOn = false;
                    beam.beamCollides = false;
                    beam.beamLength = 5;
                    beam.textureLengthScale = 2;
                    beam.beamLineRendererPrefab = template;
                    beam.SpawnBeam();
                    root.SetActive(true);
                    Application.logMessageReceived += collect;
                    try { beam.SendMessage("FixedUpdate"); }
                    finally { Application.logMessageReceived -= collect; }
                    var line = root.GetComponentsInChildren<LineRenderer>(true).Single(renderer => renderer.gameObject != template);
                    Require(messages.Count == 0, "Beam UV update logged: " + string.Join("\n", messages));
                    Require(Vector3.Distance(line.GetPosition(1), Vector3.forward * 5) < .0001f, "Beam UV guard changed the beam endpoint.");
                    if (textured) Require(line.sharedMaterial.mainTextureScale == new Vector2(2.5f, 1), "Textured beam UV scaling stopped working.");
                }
                finally
                {
                    Application.logMessageReceived -= collect;
                    var lines = root.GetComponentsInChildren<LineRenderer>(true);
                    var ownedMaterials = lines.Select(line => line.sharedMaterial).Where(value => value != null).Distinct().ToArray();
                    foreach (var line in lines) Object.DestroyImmediate(line.gameObject);
                    Object.DestroyImmediate(root);
                    foreach (var value in ownedMaterials) Object.DestroyImmediate(value);
                }
            }
        }

        static void CheckPixels(Material[] materials)
        {
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1) },
                normals = Enumerable.Repeat(Vector3.forward, 4).ToArray(),
                tangents = Enumerable.Repeat(new Vector4(1, 0, 0, 1), 4).ToArray(),
                colors = Enumerable.Repeat(new Color(.2f, .3f, .4f, 1), 4).ToArray(),
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 }
            };
            var target = new RenderTexture(8, 8, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(8, 8, TextureFormat.RGBAFloat, false, true);
            var previousTarget = RenderTexture.active;
            bool previousAsync = ShaderUtil.allowAsyncCompilation;
            string[] keywords = { "FOG_LINEAR", "FOG_EXP", "FOG_EXP2", "SOFTPARTICLES_ON" };
            bool[] enabled = keywords.Select(Shader.IsKeywordEnabled).ToArray();
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                foreach (string keyword in keywords) Shader.DisableKeyword(keyword);
                target.Create();
                foreach (var source in materials)
                {
                    var material = new Material(source) { enableInstancing = false };
                    try
                    {
                        // Scene depth and camera fading are unavailable in this isolated color-pass probe.
                        material.DisableKeyword("_SOFTPARTICLES_ON");
                        material.DisableKeyword("_FADING_ON");
                        // Neutral colors distinguish shader errors from intentionally magenta effects.
                        for (int i = 0; i < material.shader.GetPropertyCount(); i++)
                            if (material.shader.GetPropertyType(i) == ShaderPropertyType.Color)
                                material.SetColor(material.shader.GetPropertyName(i), new Color(.2f, .3f, .4f, 1));
                        Color[] pixels = Render(material, mesh, target, readback);
                        Require(!pixels.Any(pixel => pixel.r > .99f && pixel.g < .01f && pixel.b > .99f),
                            source.name + " rendered the magenta error shader.");
                        CheckShader(source.shader);
                    }
                    finally { Object.DestroyImmediate(material); }
                }
                var intense = new Material(AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Main/PolySolidIntense.mat"));
                try
                {
                    intense.SetTexture("_MainTex", Texture2D.whiteTexture);
                    intense.SetColor("_TintColor", Color.white);
                    intense.SetFloat("_Glow", .5f);
                    mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                    mesh.colors = Enumerable.Repeat(new Color(.2f, .3f, .4f, 0), 4).ToArray();
                    Color[] pixels = Render(intense, mesh, target, readback);
                    Require(pixels.All(pixel => Mathf.Abs(pixel.r - .37f) < .003f && Mathf.Abs(pixel.g - .59f) < .003f &&
                        Mathf.Abs(pixel.b - .83f) < .003f && Mathf.Abs(pixel.a - .61f) < .003f),
                        "PolyIntense must add RGB even when vertex alpha is zero.");
                }
                finally { Object.DestroyImmediate(intense); }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = previousAsync;
                for (int i = 0; i < keywords.Length; i++)
                    if (enabled[i]) Shader.EnableKeyword(keywords[i]); else Shader.DisableKeyword(keywords[i]);
                RenderTexture.active = previousTarget;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(mesh);
            }
        }

        static Color[] Render(Material material, Mesh mesh, RenderTexture target, Texture2D readback)
        {
            using (var commands = new CommandBuffer())
            {
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true, true, new Color(.17f, .29f, .43f, .61f));
                commands.SetViewProjectionMatrices(Matrix4x4.identity,
                    GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-1, 1, -1, 1, .1f, 10), true));
                commands.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
                Graphics.ExecuteCommandBuffer(commands);
            }
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, 8, 8), 0, 0);
            readback.Apply();
            return readback.GetPixels();
        }

        static void CheckShader(Shader shader)
        {
            Require(shader != null && shader.isSupported, "A Polygon Arsenal shader is missing or unsupported.");
            Require(!ShaderUtil.ShaderHasError(shader), shader.name + ": " +
                string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(message => message.message)));
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
