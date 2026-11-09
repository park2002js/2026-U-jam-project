using System;
using System.Linq;
using UJam.Runtime.Enemy.Vfx;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    public static class CrystalGlowAuthoring
    {
        public static void Configure(GameObject root, Renderer[] crystals, Color color, string assetPrefix)
        {
            Require(root != null && crystals != null && crystals.Length > 0, "Crystal glow requires explicit source renderers.");
            Require(!string.IsNullOrWhiteSpace(assetPrefix) && assetPrefix.IndexOfAny(new[] { '/', '\\', '.' }) < 0,
                "Crystal glow requires a local enemy asset prefix.");
            Require(crystals.Distinct().Count() == crystals.Length && crystals.All(source => source != null
                && source.transform.IsChildOf(root.transform) && source.name.IndexOf("Crystal", StringComparison.OrdinalIgnoreCase) >= 0
                && source.GetComponent<CrystalGlow>() == null && MeshOf(source) != null),
                "Crystal glow sources must be distinct physical crystal mesh renderers.");
            Shader shader = Shader.Find("UJam/Enemy/Crystal Glow");
            Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Crystal glow shader is missing or has compiler errors.");
            Material surface = Material(assetPrefix + "/CrystalSurfaceGlow", shader, color, false);
            Material halo = Material(assetPrefix + "/CrystalHaloGlow", shader, color, true);
            Transform container = root.transform.Find("VFX_CrystalGlow");
            if (container == null)
            {
                container = new GameObject("VFX_CrystalGlow").transform;
                container.SetParent(root.transform, false);
            }
            Transform previous = container.Find(assetPrefix);
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
            var group = new GameObject(assetPrefix).transform;
            group.SetParent(container, false);
            Renderer[] largest = crystals.OrderByDescending(source => MeshOf(source).bounds.size.sqrMagnitude)
                .Take(Mathf.Min(4, crystals.Length)).ToArray();

            foreach (Renderer source in crystals)
            {
                Mesh mesh = MeshOf(source);
                Require(mesh != null && AssetDatabase.Contains(mesh), "Crystal glow must reuse a persistent source mesh.");
                var visual = new GameObject(source.name + "_Glow", typeof(MeshFilter), typeof(MeshRenderer));
                visual.layer = source.gameObject.layer;
                visual.transform.SetParent(group, false);
                visual.GetComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer overlay = visual.GetComponent<MeshRenderer>();
                SetupRenderer(overlay, surface);
                overlay.sharedMaterials = Enumerable.Repeat(surface, mesh.subMeshCount).ToArray();
                CrystalGlow glow = visual.AddComponent<CrystalGlow>();
                glow.Source = source;
                glow.Surface = overlay;
                if (source is SkinnedMeshRenderer skin)
                {
                    BoneWeight[] weights = mesh.boneWeights;
                    Require(weights.Length > 0, source.name + " has no rigid skin weights.");
                    int index = weights[0].boneIndex0;
                    Require(weights.All(weight => weight.boneIndex0 == index && weight.weight0 > .999f
                        && weight.weight1 < .001f && weight.weight2 < .001f && weight.weight3 < .001f),
                        source.name + " glow requires one owning rigid bone.");
                    Require(index >= 0 && index < skin.bones.Length && index < mesh.bindposes.Length && skin.bones[index] != null,
                        source.name + " has an invalid owning bone or bindpose.");
                    glow.Bone = skin.bones[index];
                    glow.BindPose = mesh.bindposes[index];
                }
                if (largest.Contains(source))
                {
                    var light = new GameObject("SoftHalo", typeof(MeshFilter), typeof(MeshRenderer));
                    light.layer = source.gameObject.layer;
                    light.transform.SetParent(visual.transform, false);
                    light.transform.localPosition = mesh.bounds.center;
                    light.transform.localScale = Vector3.one * Mathf.Max(.025f, mesh.bounds.extents.magnitude * 1.12f);
                    light.GetComponent<MeshFilter>().sharedMesh = Quad();
                    glow.Halo = light.GetComponent<MeshRenderer>();
                    SetupRenderer(glow.Halo, halo);
                }
                glow.Refresh();
                Matrix4x4 expected = glow.Bone != null ? glow.Bone.localToWorldMatrix * glow.BindPose : source.transform.localToWorldMatrix;
                Require((visual.transform.TransformPoint(mesh.bounds.center) - expected.MultiplyPoint3x4(mesh.bounds.center)).sqrMagnitude < .000001f,
                    source.name + " glow did not follow its posed rigid skin.");
                Transform model = root.transform.Find("Model");
                Require(model == null || !visual.transform.IsChildOf(model), "Crystal glow renderers must remain outside Model.");
                ValidateDissolve(glow);
            }
            Require(group.GetComponentsInChildren<CrystalGlow>(true).Length == crystals.Length,
                "Crystal glow source scope changed or duplicated.");
        }

        private static void ValidateDissolve(CrystalGlow glow)
        {
            var original = new MaterialPropertyBlock();
            var probe = new MaterialPropertyBlock();
            glow.Source.GetPropertyBlock(original);
            glow.Source.GetPropertyBlock(probe);
            bool enabled = glow.Source.enabled;
            try
            {
                glow.Source.enabled = true;
                probe.SetFloat("_Dissolve", .5f);
                glow.Source.SetPropertyBlock(probe);
                glow.Refresh();
                Require(!glow.Source.gameObject.activeInHierarchy || Mathf.Abs(glow.Visibility - .5f) < .0001f,
                    "Crystal glow must fade with its source dissolve.");
                probe.SetFloat("_Dissolve", 1);
                glow.Source.SetPropertyBlock(probe);
                glow.Refresh();
                Require(!glow.Surface.enabled && (glow.Halo == null || !glow.Halo.enabled),
                    "A fully dissolved crystal must leave no glow.");
                glow.Source.enabled = false;
                probe.SetFloat("_Dissolve", 0);
                glow.Source.SetPropertyBlock(probe);
                glow.Refresh();
                Require(!glow.Surface.enabled && (glow.Halo == null || !glow.Halo.enabled),
                    "A disabled source crystal must leave no glow.");
            }
            finally
            {
                glow.Source.SetPropertyBlock(original);
                glow.Source.enabled = enabled;
                glow.Refresh();
            }
        }

        private static Mesh MeshOf(Renderer source) => source is SkinnedMeshRenderer skin
            ? skin.sharedMesh : source.GetComponent<MeshFilter>()?.sharedMesh;

        private static void SetupRenderer(MeshRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Material Material(string id, Shader shader, Color color, bool billboard)
        {
            string path = VfxAuthoring.Directory + id + ".mat";
            VfxAuthoring.Ensure(path);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_GlowColor", color);
            material.SetFloat("_Billboard", billboard ? 1 : 0);
            material.SetFloat("_Visibility", 1);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh Quad()
        {
            const string path = VfxAuthoring.Directory + "Shared/CrystalGlowQuad.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            VfxAuthoring.Ensure(path);
            mesh = new Mesh { name = "Crystal glow billboard", vertices = new[] {
                new Vector3(-1,-1,0), new Vector3(1,-1,0), new Vector3(1,1,0), new Vector3(-1,1,0) },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                triangles = new[] { 0, 1, 2, 0, 2, 3 } };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
