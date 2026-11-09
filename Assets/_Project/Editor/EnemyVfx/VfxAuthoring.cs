using System;
using System.IO;
using System.Linq;
using UJam.Runtime.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    public static class VfxAuthoring
    {
        public const string Directory = "Assets/_Project/Art/EnemyVfx/";

        public static Material Material(string id, Color color, bool additive = false)
        {
            string path = Directory + id + ".mat";
            Ensure(path);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", additive ? 2 : 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Mesh ShardMesh()
        {
            string path = Directory + "Shared/Shard.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            Ensure(path);
            Vector3[] points = { new Vector3(0, .65f, 0), new Vector3(-.45f, -.3f, -.3f), new Vector3(.45f, -.3f, -.3f), new Vector3(0, -.3f, .5f) };
            int[] faces = { 0, 2, 1, 0, 3, 2, 0, 1, 3, 1, 2, 3 };
            mesh = new Mesh { name = "Lowpoly tetrahedron" };
            mesh.vertices = faces.Select(index => points[index]).ToArray();
            mesh.triangles = Enumerable.Range(0, faces.Length).ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        public static GameObject Burst(string id, Color color, int count, float size, float speed, float lifetime)
        {
            if (count <= 0 || size <= 0 || lifetime <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            string path = Directory + id + ".prefab";
            Ensure(path);
            GameObject root = new GameObject(Path.GetFileName(id));
            try
            {
                ParticleSystem particles = root.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.duration = .2f; main.loop = false; main.playOnAwake = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * .65f, lifetime);
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .45f, speed);
                main.startSize = new ParticleSystem.MinMaxCurve(size * .5f, size);
                main.startColor = color; main.gravityModifier = .3f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = Mathf.Max(count * 2, 32);
                main.startRotation3D = true;
                main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
                main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
                var emission = particles.emission; emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
                var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .12f;
                var colors = particles.colorOverLifetime; colors.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                    new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .55f), new GradientAlphaKey(0, 1) });
                colors.color = gradient;
                var sizes = particles.sizeOverLifetime; sizes.enabled = true;
                sizes.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .6f), new Keyframe(.15f, 1), new Keyframe(1, 0)));
                ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = ShardMesh();
                renderer.sharedMaterial = Material(id + "_Particles", Color.white);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                root.AddComponent<LowpolyVfxLifetime>().Lifetime = lifetime + .45f;
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        public static Transform Find(GameObject root, string nameFragment) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name.Equals(nameFragment, StringComparison.OrdinalIgnoreCase))
            ?? root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0);

        public static void Bind(GameObject root, string signal, GameObject effect, Transform anchor = null, bool atTarget = false, bool follow = false)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            EnemyVfx vfx = root.GetComponent<EnemyVfx>();
            if (vfx == null) vfx = root.AddComponent<EnemyVfx>();
            vfx.Bindings = vfx.Bindings.Concat(new[] { new EnemyVfx.Binding { Signal = signal, Effect = effect, Anchor = anchor, AtTarget = atTarget, Follow = follow } }).ToArray();
        }

        public static void Ensure(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(directory)) return;
            string parent = Path.GetDirectoryName(directory).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) Ensure(parent + "/placeholder");
            AssetDatabase.CreateFolder(parent, Path.GetFileName(directory));
        }
    }
}
