using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    public static class PolygonParticleMaterialChecks
    {
        const string ShaderName = "PolygonParticles/Legacy URP";
        const string Root = "Assets/POLYGON Package/PolygonParticles";
        static readonly string[] Modes = { "Alpha Blended", "Additive", "Additive (Soft)", "Multiply", "Multiply (Double)" };
        static readonly BlendMode[] Sources = { BlendMode.SrcAlpha, BlendMode.SrcAlpha, BlendMode.One, BlendMode.Zero, BlendMode.DstColor };
        static readonly BlendMode[] Destinations = { BlendMode.OneMinusSrcAlpha, BlendMode.One, BlendMode.OneMinusSrcColor, BlendMode.SrcColor, BlendMode.SrcColor };

        [MenuItem("Tools/Polygon Particles/Run Material Checks")]
        public static void Run()
        {
            var materials = AssetDatabase.FindAssets("t:Material", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();
            Require(materials.Length == 60, "Expected the 60 Polygon Particles materials, including the two restored assets.");
            foreach (var material in materials)
            {
                CheckShader(material.shader);
                Require(material.shader.name == ShaderName || material.shader.name == "Universal Render Pipeline/Lit",
                    material.name + " still uses an incompatible shader.");
                if (material.shader.name != ShaderName)
                {
                    if (material.name.StartsWith("PolygonParticle")) CheckStandardMaterial(material);
                    continue;
                }
                int mode = (int)material.GetFloat("_LegacyMode");
                Require(mode >= 0 && mode < Modes.Length, material.name + " has an invalid legacy mode.");
                Require(material.GetFloat("_SrcBlend") == (float)Sources[mode] &&
                        material.GetFloat("_DstBlend") == (float)Destinations[mode] &&
                        material.GetFloat("_ColorMask") == (mode == 3 ? 15 : 14),
                    material.name + " has incorrect legacy blending.");
                Require(material.renderQueue == 3000, material.name + " must render with transparent particles.");
            }
            Require(materials.Count(material => material.shader.name == ShaderName) == 44,
                "Expected 44 legacy particles and 16 URP Lit materials.");
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab t:Scene", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Match reference in Regex.Matches(File.ReadAllText(path), @"\{fileID: 2100000, guid: ([0-9a-f]{32}), type: 2\}"))
                    Require(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(reference.Groups[1].Value)) != null,
                        path + " references missing material " + reference.Groups[1].Value);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                foreach (var renderer in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    Require(renderer.renderMode == ParticleSystemRenderMode.None || renderer.sharedMaterial != null,
                        path + "/" + renderer.name + " has no particle material.");
                    Require(!renderer.GetComponent<ParticleSystem>().trails.enabled || renderer.trailMaterial != null,
                        path + "/" + renderer.name + " has no trail material.");
                    foreach (var material in renderer.sharedMaterials.Where(material => material != null)) CheckShader(material.shader);
                }
            }
            CheckPixels();
            Debug.Log("[PolygonParticleMaterialChecks] All 60 materials, prefab/scene references and five legacy blend modes passed.");
        }

        static void CheckStandardMaterial(Material material)
        {
            string name = material.name;
            bool fire = name == "PolygonParticleFX_Emissive_Fire";
            bool transparent = fire || name == "PolygonParticleFX_Emission" || name == "PolygonParticleFX_Smoke_Mat_03";
            bool emission = transparent || name == "PolygonParticleFX_Ice_01" || name == "PolygonParticleFX_Ice_02";
            Require(material.IsKeywordEnabled("_EMISSION") == emission, name + " changed its emission enabled state.");
            Require(material.GetFloat("_Surface") == (transparent ? 1 : 0), name + " changed its surface type.");
            if (transparent) Require(material.GetFloat("_Blend") == 0 && material.GetFloat("_ZWrite") == 0 &&
                material.GetFloat("_BlendModePreserveSpecular") == (fire ? 1 : 0) &&
                material.GetFloat("_SrcBlend") == (float)(fire ? BlendMode.One : BlendMode.SrcAlpha) &&
                material.GetFloat("_DstBlend") == (float)BlendMode.OneMinusSrcAlpha,
                name + " changed its transparent blend mode.");
        }

        static void CheckPixels()
        {
            var shader = Shader.Find(ShaderName);
            CheckShader(shader);
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1) },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            var texture = new Texture2D(2, 2, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            texture.SetPixels(new[] { new Color(.22f, .81f, .56f, .63f), new Color(.69f, .18f, .47f, .91f),
                new Color(.83f, .48f, .12f, .27f), new Color(.31f, .62f, .79f, .74f) });
            texture.Apply();
            var target = new RenderTexture(8, 8, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(8, 8, TextureFormat.RGBAFloat, false, true);
            var previousTarget = RenderTexture.active;
            string[] keywords = { "FOG_LINEAR", "FOG_EXP", "FOG_EXP2", "SOFTPARTICLES_ON" };
            bool[] enabled = keywords.Select(Shader.IsKeywordEnabled).ToArray();
            bool previousAsyncCompilation = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                foreach (string keyword in keywords) Shader.DisableKeyword(keyword);
                target.Create();
                for (int mode = 0; mode < Modes.Length; mode++)
                {
                    var original = new Material(Shader.Find("Legacy Shaders/Particles/" + Modes[mode]));
                    var converted = new Material(shader);
                    try
                    {
                        converted.SetFloat("_LegacyMode", mode);
                        converted.SetFloat("_SrcBlend", (float)Sources[mode]);
                        converted.SetFloat("_DstBlend", (float)Destinations[mode]);
                        converted.SetFloat("_ColorMask", mode == 3 ? 15 : 14);
                        converted.DisableKeyword("_SOFTPARTICLES_ON");
                        foreach (var material in new[] { original, converted })
                        {
                            material.SetTexture("_MainTex", texture);
                            material.SetTextureScale("_MainTex", new Vector2(.65f, .77f));
                            material.SetTextureOffset("_MainTex", new Vector2(.13f, .09f));
                            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", new Color(.73f, .31f, .58f, .64f));
                        }
                        foreach (float alpha in new[] { 0f, .35f, 1f })
                        {
                            mesh.colors = Enumerable.Repeat(new Color(.27f, .76f, .49f, alpha), 4).ToArray();
                            Color[] expected = Render(original, mesh, target, readback);
                            Color[] actual = Render(converted, mesh, target, readback);
                            Require(!expected.Concat(actual).Any(pixel => pixel.r > .99f && pixel.g < .01f && pixel.b > .99f),
                                Modes[mode] + " rendered the magenta error shader.");
                            if (alpha == 0f) Require(actual.All(pixel => Mathf.Abs(pixel.r - .17f) < .003f &&
                                Mathf.Abs(pixel.g - .29f) < .003f && Mathf.Abs(pixel.b - .43f) < .003f &&
                                Mathf.Abs(pixel.a - .61f) < .003f), Modes[mode] + " must preserve the background at zero alpha.");
                            float error = expected.Zip(actual, (a, b) => Mathf.Max(Mathf.Abs(a.r - b.r),
                                Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b), Mathf.Abs(a.a - b.a))).Max();
                            Require(error < .003f, Modes[mode] + " changed pixel output at alpha " + alpha + ": " + error);
                            if (alpha == 1f) Require(actual.Any(pixel => Mathf.Abs(pixel.r - .17f) > .01f),
                                Modes[mode] + " did not draw the test quad.");
                        }
                    }
                    finally { Object.DestroyImmediate(original); Object.DestroyImmediate(converted); }
                }
                CheckShader(shader);
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = previousAsyncCompilation;
                for (int i = 0; i < keywords.Length; i++)
                    if (enabled[i]) Shader.EnableKeyword(keywords[i]); else Shader.DisableKeyword(keywords[i]);
                RenderTexture.active = previousTarget;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(texture);
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
            Require(shader != null && shader.isSupported, "A Polygon Particles shader is missing or unsupported.");
            Require(!ShaderUtil.ShaderHasError(shader), shader.name + ": " +
                string.Join("\n", ShaderUtil.GetShaderMessages(shader).Select(message => message.message)));
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
