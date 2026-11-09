using System;
using System.Linq;
using UJam.Runtime.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UJam.Editor
{
    public static class SkeletonVfxSetup
    {
        private const string Id = "Skeleton/";
        private static readonly Color Dust = new Color(.44f, .40f, .33f, .5f);

        public static void Configure(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            Require(root.GetComponent<SkeletonEnemy>() != null, "Skeleton VFX requires SkeletonEnemy.");
            SkinnedMeshRenderer sword = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(renderer => renderer.name == "Sword");
            Require(sword != null && sword.sharedMesh != null, "Skeleton requires the authored Sword mesh.");
            int boneIndex = Array.FindIndex(sword.bones, bone => bone != null && bone.name == "Sword");
            Require(boneIndex >= 0 && boneIndex < sword.sharedMesh.bindposes.Length,
                "Skeleton sword trail must follow the animated Sword bone.");
            Transform bone = sword.bones[boneIndex];
            var blade = new GameObject("SkeletonSteelSlash");
            blade.layer = root.layer;
            blade.transform.SetParent(bone, false);
            // A skinned renderer's transform is static; the bind pose puts its mesh center on the animated blade.
            blade.transform.localPosition = sword.sharedMesh.bindposes[boneIndex].MultiplyPoint3x4(sword.sharedMesh.bounds.center);
            TrailRenderer trail = blade.AddComponent<TrailRenderer>();
            trail.sharedMaterial = VfxAuthoring.Material(Id + "SteelSlash", Color.white, true);
            trail.time = .09f;
            trail.minVertexDistance = .06f;
            trail.widthMultiplier = Mathf.Clamp(Vector3.Scale(sword.sharedMesh.bounds.size, sword.transform.lossyScale).magnitude * .6f, .12f, .65f);
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            trail.startColor = new Color(.8f, .9f, 1f, .65f);
            trail.endColor = new Color(.6f, .7f, .8f, 0);
            trail.alignment = LineAlignment.View;
            trail.numCapVertices = 0;
            trail.numCornerVertices = 0;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;

            var ground = new GameObject("SkeletonGroundVfx");
            ground.layer = root.layer;
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = Vector3.up * .035f;
            VfxAuthoring.Bind(root, "SpawnStart", GroundDust("SummonDust", 8, .11f, .3f, .5f), ground.transform);
            VfxAuthoring.Bind(root, "SpawnLand", GroundDust("LandingDust", 12, .13f, .65f, .4f), ground.transform);
            VfxAuthoring.Bind(root, "DeathImpact", GroundDust("CollapseDust", 10, .11f, .45f, .35f), ground.transform);
            GameObject hit = VfxAuthoring.Burst(Id + "SteelHit", new Color(.86f, .92f, 1f, .85f), 7, .055f, 1.25f, .18f);
            VfxAuthoring.Bind(root, "AttackImpact", hit, atTarget: true);
            EnemyVfx vfx = root.GetComponent<EnemyVfx>();
            vfx.Trails = new[] { trail };
            Require(vfx.Trails[0].transform.parent == bone && !trail.emitting,
                "Skeleton slash must start disabled on its animated sword.");
            Require(vfx.Bindings.Any(binding => binding.Signal == "AttackImpact" && binding.AtTarget && binding.Effect == hit),
                "Skeleton steel hit must use the actual validated attack target.");
        }

        private static GameObject GroundDust(string name, int count, float size, float speed, float lifetime)
        {
            GameObject prefab = VfxAuthoring.Burst(Id + name, Dust, count, size, speed, lifetime);
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ParticleSystem particles = contents.GetComponent<ParticleSystem>();
                particles.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                var main = particles.main;
                main.gravityModifier = 0;
                main.startSize3D = true;
                main.startSizeX = main.startSizeZ = new ParticleSystem.MinMaxCurve(size * .6f, size);
                main.startSizeY = new ParticleSystem.MinMaxCurve(size * .2f, size * .4f);
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 78;
                shape.radius = .3f;
                var sizes = particles.sizeOverLifetime;
                sizes.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .5f, 1, 1.35f));
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
