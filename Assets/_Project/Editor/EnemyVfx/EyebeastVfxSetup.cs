using System;
using UJam.Runtime.Enemy;
using UJam.Runtime.Enemy.Projectiles;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    public static class EyebeastVfxSetup
    {
        private const string Directory = "Assets/_Project/Art/EnemyVfx/Eyebeast/";
        private const float EyeDiameterRatio = 1f;
        private static readonly Color Purple = new Color(.68f, .12f, 1f);
        private enum FlameLayer { Veil, Rim, Flame, Trail }

        public static void Configure(GameObject root)
        {
            var ranged = root.GetComponent<RangedEnemy>();
            Require(ranged != null, "Eyebeast VFX requires RangedEnemy.");
            var serialized = new SerializedObject(ranged);
            var source = serialized.FindProperty("_projectilePrefab").objectReferenceValue as Projectile;
            Transform firePoint = serialized.FindProperty("_firePoint").objectReferenceValue as Transform;
            Require(source != null && firePoint != null, "Eyebeast projectile and FirePoint must exist before VFX setup.");

            float eyeDiameter = MeasureEyeDiameter(root);
            float diameter = eyeDiameter * EyeDiameterRatio;
            GameObject launch = VfxAuthoring.Burst("Eyebeast/LaunchFlash", new Color(.9f, .55f, 1f), 16, diameter * .3f, 1.2f, .2f);
            GameObject impact = VfxAuthoring.Burst("Eyebeast/FireballImpact", Purple, 30, diameter * .42f, 2.8f, .42f);
            GameObject dust = VfxAuthoring.Burst("Eyebeast/LandingDust", new Color(.46f, .38f, .43f, .65f), 10, .18f, .85f, .42f);
            Projectile projectile = BuildProjectile(source, impact, eyeDiameter);
            serialized.FindProperty("_projectilePrefab").objectReferenceValue = projectile;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EnemyVfx vfx = root.GetComponent<EnemyVfx>();
            if (vfx != null)
                vfx.Bindings = Array.FindAll(vfx.Bindings, binding => binding.Effect == null
                    || !AssetDatabase.GetAssetPath(binding.Effect).StartsWith(Directory, StringComparison.Ordinal));
            VfxAuthoring.Bind(root, "AttackImpact", launch, firePoint);
            VfxAuthoring.Bind(root, "DeathImpact", dust, root.transform);
            Require(new SerializedObject(projectile).FindProperty("_impactEffect").objectReferenceValue == impact,
                "Eyebeast impact must be dispatched by projectile arrival.");
            Debug.Log($"[EyebeastVfxSetup] Eye bind-mesh diameter={eyeDiameter:F6}; burning fireball diameter={diameter:F6}; ratio={EyeDiameterRatio:F2}.", root);
        }

        private static float MeasureEyeDiameter(GameObject root)
        {
            SkinnedMeshRenderer eye = null;
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (renderer.name.Equals("Eye", StringComparison.OrdinalIgnoreCase)) eye = renderer;
            Require(eye != null && eye.sharedMesh != null, "Eyebeast must contain its separate Eye renderer for sizing.");
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            Matrix4x4 matrix = root.transform.worldToLocalMatrix * eye.transform.localToWorldMatrix;
            using (Mesh.MeshDataArray data = MeshUtility.AcquireReadOnlyMeshData(eye.sharedMesh))
            using (var vertices = new NativeArray<Vector3>(eye.sharedMesh.vertexCount, Allocator.Temp))
            {
                data[0].GetVertices(vertices);
                foreach (Vector3 vertex in vertices)
                {
                    float x = matrix.MultiplyPoint3x4(vertex).x;
                    min = Mathf.Min(min, x); max = Mathf.Max(max, x);
                }
            }
            Require(max > min && !float.IsInfinity(max - min), "Eyebeast eye bind-mesh diameter is invalid.");
            return max - min;
        }

        private static Projectile BuildProjectile(Projectile source, GameObject impact, float eyeDiameter)
        {
            GameObject root = Object.Instantiate(source.gameObject);
            root.name = "Eyebeast_PurpleFireball";
            try
            {
                float diameter = eyeDiameter * EyeDiameterRatio;
                // The basic sphere only records the eye-relative size; the entire visible volume is fire.
                Transform orb = root.transform.Find("Orb");
                Require(orb != null && orb.GetComponent<MeshRenderer>() != null,
                    "Eyebeast fireball requires the existing basic sphere named Orb.");
                orb.localScale = Vector3.one * diameter;
                orb.GetComponent<MeshRenderer>().enabled = false;
                Material flame = VfxAuthoring.Material("Eyebeast/PurpleFlame", Color.white, true);
                Material veil = VfxAuthoring.Material("Eyebeast/PurpleFlameVeil", Color.white);

                RemoveEmitter(root, "AngularFlames");
                RemoveEmitter(root, "PurpleEmberTrail");
                RemoveEmitter(root, "FlameVeil");
                RemoveEmitter(root, "FlameRim");
                AddFlames(root, "FlameVeil", veil, diameter, FlameLayer.Veil);
                AddFlames(root, "FlameRim", flame, diameter, FlameLayer.Rim);
                AddFlames(root, "AngularFlames", flame, diameter, FlameLayer.Flame);
                AddFlames(root, "PurpleEmberTrail", flame, diameter, FlameLayer.Trail);

                var serialized = new SerializedObject(root.GetComponent<Projectile>());
                SerializedProperty impactField = serialized.FindProperty("_impactEffect");
                Require(impactField != null, "Projectile._impactEffect is required for the arrival VFX.");
                impactField.objectReferenceValue = impact;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, Directory + "Eyebeast_PurpleFireball.prefab");
                Require(saved != null, "Could not save the Eyebeast fireball prefab.");
                ParticleSystem[] particles = saved.GetComponentsInChildren<ParticleSystem>(true);
                Transform savedOrb = saved.transform.Find("Orb");
                Require(!savedOrb.GetComponent<MeshRenderer>().enabled
                    && Mathf.Abs(savedOrb.localScale.x / eyeDiameter - EyeDiameterRatio) < .0001f,
                    "Eyebeast solid sphere must be invisible and the fireball must match the eye diameter.");
                Require(particles.Length == 4, "Eyebeast fireball needs its dense veil, colored rim, angular flames and trail.");
                foreach (ParticleSystem particle in particles)
                {
                    var renderer = particle.GetComponent<ParticleSystemRenderer>();
                    Require(particle.main.loop && renderer.renderMode == ParticleSystemRenderMode.Mesh
                        && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(renderer.mesh))
                        && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(renderer.sharedMaterial)),
                        "Eyebeast particles must reference persistent angular meshes and materials.");
                }
                return saved.GetComponent<Projectile>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void AddFlames(GameObject parent, string name, Material material, float diameter, FlameLayer layer)
        {
            bool trail = layer == FlameLayer.Trail;
            bool veil = layer == FlameLayer.Veil;
            bool rim = layer == FlameLayer.Rim;
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            child.layer = parent.layer;
            var particles = child.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.prewarm = !trail;
            main.duration = 1f;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(trail ? .3f : .2f, trail ? .55f : .34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(diameter * .025f, diameter * .12f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(diameter * (veil ? .35f : .15f), diameter * (veil ? .6f : .27f));
            main.startSizeY = new ParticleSystem.MinMaxCurve(diameter * (veil ? .4f : .25f), diameter * (veil ? .6f : .42f));
            main.startSizeZ = main.startSizeX;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = trail ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = trail ? 64 : 128;
            var emission = particles.emission;
            emission.rateOverTime = veil ? 190f : rim ? 145f : trail ? 60f : 95f;
            if (!trail) emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(veil ? 40 : 24)) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = diameter * (veil ? .18f : rim ? .26f : trail ? .08f : .22f);
            shape.radiusThickness = rim ? 0f : 1f;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-diameter * .1f, diameter * .1f);
            velocity.y = new ParticleSystem.MinMaxCurve(diameter * .05f, diameter * (veil ? .12f : .28f));
            velocity.z = new ParticleSystem.MinMaxCurve(-diameter * (veil ? .12f : .7f), -diameter * .08f);
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = diameter * (veil ? .045f : .1f);
            noise.frequency = 1.6f;
            noise.scrollSpeed = 2.5f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, .35f), new Keyframe(.15f, 1f), new Keyframe(1f, 0f)));
            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(veil ? new Color(.78f, .18f, .95f) : new Color(.95f, .55f, 1f), 0f),
                new GradientColorKey(Purple, .25f),
                new GradientColorKey(new Color(.32f, .015f, .62f), 1f)
            }, new[]
            {
                new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.95f, .08f),
                new GradientAlphaKey(.8f, .5f), new GradientAlphaKey(0f, 1f)
            });
            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = gradient;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = VfxAuthoring.ShardMesh();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void RemoveEmitter(GameObject root, string name)
        {
            Transform child = root.transform.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
