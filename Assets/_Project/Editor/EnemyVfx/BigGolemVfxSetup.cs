using System;
using System.Linq;
using UJam.Runtime.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    public static class BigGolemVfxSetup
    {
        private const float SpreadScale = .9f;
        private static readonly Color DustColor = new Color(.66f, .59f, .48f, .3f);

        public static void Configure(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            Transform hand = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(renderer => renderer.bones)
                .FirstOrDefault(bone => bone != null && bone.name.Equals("Hand_R", StringComparison.OrdinalIgnoreCase))
                ?? VfxAuthoring.Find(root, "Hand_R");
            Transform model = root.transform.Find("Model");
            Require(model != null, "Big Golem requires its preserved Model subtree.");
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(renderers.Length == 61, "Big Golem source Model must retain its 61 physical renderers.");
            Renderer[] eyes = renderers.Where(renderer => renderer.name.StartsWith("Eye_", StringComparison.OrdinalIgnoreCase)
                || renderer.name.IndexOf("_Eye_", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            SkinnedMeshRenderer handMesh = renderers.FirstOrDefault(renderer =>
                renderer.name.StartsWith("Hand_R", StringComparison.OrdinalIgnoreCase));
            Require(hand != null && handMesh != null, "Big Golem VFX requires the right-hand bone and renderer.");
            Require(eyes.Length == 2, "Big Golem EyesOff requires both separate eye renderers.");

            BoxCollider body = root.GetComponent<BoxCollider>();
            Require(body != null && body.bounds.size.x > 0 && body.bounds.size.z > 0,
                "Big Golem dust requires its measured body footprint.");
            Bounds footprint = body.bounds;
            float bodyEdge = Mathf.Max(footprint.extents.x, footprint.extents.z);
            float bodyRadius = new Vector2(footprint.extents.x, footprint.extents.z).magnitude;
            Transform ground = Anchor(root.transform, "BigGolemGroundContact", new Vector3(footprint.center.x,
                footprint.min.y + .15f, footprint.center.z));
            Transform strikeTip = Anchor(hand, "BigGolemHandStrikeTip", GolemVfxSetup.HandStrikeTipPosition(hand, handMesh));
            Transform stroke = Anchor(hand, "BigGolemPurpleHandStreak", handMesh.bounds.center);
            TrailRenderer trail = stroke.GetComponent<TrailRenderer>() ?? stroke.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = VfxAuthoring.Material("Big_Golem/PurpleHandStreak", new Color(.62f, .15f, 1f, .66f), true);
            trail.time = .11f;
            trail.minVertexDistance = .18f;
            trail.startWidth = .32f;
            trail.endWidth = .035f;
            trail.numCornerVertices = trail.numCapVertices = 0;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            trail.colorGradient = fade;

            Mesh cloud = GolemVfxSetup.RoundedCloudMesh();
            GameObject step = Dust("StepDust", cloud, 22, 1f, 1f, .6f, bodyRadius + .15f, bodyEdge + .05f);
            GameObject impact = Dust("MeleeContactDust", cloud, 28, 1.05f, 1.5f, .5f, .32f);
            GameObject collapse = Dust("CollapseContactDust", cloud, 42, 1.25f, 1.3f, .85f, bodyRadius + .35f, bodyEdge + .1f);
            ParticleSystem collapseParticles = collapse.GetComponent<ParticleSystem>();
            float collapseEnvelope = collapseParticles.shape.radius + collapseParticles.main.startSpeed.constantMax * collapseParticles.main.startLifetime.constantMax;
            Require(collapseParticles.shape.shapeType == ParticleSystemShapeType.Circle
                && collapseParticles.shape.radiusThickness > 0 && collapseParticles.shape.radiusThickness < 1
                && collapseEnvelope > bodyRadius + .15f,
                "Big Golem death dust must travel outside its measured collider corner footprint.");
            EnemyVfx vfx = root.GetComponent<EnemyVfx>() ?? root.AddComponent<EnemyVfx>();
            string[] contacts = { "MoveStep", "AttackTrailStart", "AttackImpact", "DeathImpact" };
            vfx.Bindings = vfx.Bindings.Where(binding => !contacts.Contains(binding.Signal)).ToArray();
            VfxAuthoring.Bind(root, "MoveStep", step, ground);
            VfxAuthoring.Bind(root, "AttackImpact", impact, strikeTip, atTarget: true);
            VfxAuthoring.Bind(root, "DeathImpact", collapse, ground);
            EnemyVfx.Binding strike = vfx.Bindings.Single(binding => binding.Signal == "AttackImpact");
            strike.UseAnchorPosition = true;
            strike.TargetOffset = Vector3.zero;
            vfx.Eyes = eyes;
            vfx.Trails = new[] { trail };
            Require(contacts.All(signal => vfx.Bindings.Count(binding => binding.Signal == signal) == (signal == "AttackTrailStart" ? 0 : 1)),
                "Big Golem requires movement, arm-tip attack and death dust only; its hand trail must not emit dust.");
            Require(strike.AtTarget && strike.UseAnchorPosition && strike.Anchor == strikeTip && strikeTip.parent == hand
                && strike.Effect == impact && !strike.Follow
                && impact.GetComponent<ParticleSystem>().shape.shapeType == ParticleSystemShapeType.Sphere
                && impact.GetComponent<ParticleSystem>().shape.radius < 1 && strike.TargetOffset == Vector3.zero,
                "Big Golem attack dust must spawn on the animated hand's distal surface while retaining valid-hit gating.");
            Require(vfx.Eyes.All(eye => eye != null) && !trail.emitting
                && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(trail.sharedMaterial)),
                "Big Golem eye references, trail material and initial trail state must be valid.");
            Renderer[] crystals = renderers
                .Where(renderer => renderer.name.StartsWith("Crystal_", StringComparison.Ordinal)).Cast<Renderer>().ToArray();
            Require(crystals.Length == 27, "Big Golem glow must cover exactly its 27 physical attached crystals.");
            CrystalGlowAuthoring.Configure(root, crystals, new Color(.66f, .16f, 1f), "Big_Golem");
            Require(model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 61,
                "Crystal glow must leave the Big Golem source Model renderer scope unchanged.");
        }

        private static Transform Anchor(Transform parent, string name, Vector3 position)
        {
            Transform[] existing = parent.Cast<Transform>().Where(child => child.name == name).ToArray();
            Transform anchor = existing.FirstOrDefault();
            for (int i = 1; i < existing.Length; i++) Object.DestroyImmediate(existing[i].gameObject);
            if (anchor == null) anchor = new GameObject(name).transform;
            anchor.gameObject.layer = parent.gameObject.layer;
            anchor.SetParent(parent, false);
            anchor.SetPositionAndRotation(position, Quaternion.identity);
            return anchor;
        }

        private static GameObject Dust(string name, Mesh cloud, int count, float size, float speed, float lifetime, float radius, float innerRadius = 0)
        {
            float previousRadius = radius * SpreadScale, previousSpeed = speed * SpreadScale;
            radius *= SpreadScale;
            innerRadius *= SpreadScale;
            speed *= SpreadScale;
            if (name == "StepDust") { radius *= .9f; innerRadius *= .9f; speed *= .9f; }
            GameObject prefab = VfxAuthoring.Burst("Big_Golem/" + name, DustColor, count, size, speed, lifetime);
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject instance = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ParticleSystem particles = instance.GetComponent<ParticleSystem>();
                bool annulus = innerRadius > 0;
                var main = particles.main;
                main.startSpeed = annulus ? new ParticleSystem.MinMaxCurve(speed * .8f, speed) : new ParticleSystem.MinMaxCurve(0);
                main.gravityModifier = 0;
                var shape = particles.shape;
                shape.shapeType = annulus ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Sphere;
                shape.radius = radius;
                shape.radiusThickness = annulus ? 1 - innerRadius / radius : 1;
                shape.rotation = annulus ? new Vector3(90, 0, 0) : Vector3.zero;
                shape.scale = annulus ? Vector3.one : new Vector3(1, .025f, 1);
                shape.randomDirectionAmount = 0;
                shape.sphericalDirectionAmount = annulus ? 1 : 0;
                var velocity = particles.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = velocity.z = annulus ? new ParticleSystem.MinMaxCurve(0, 0) : new ParticleSystem.MinMaxCurve(-speed, speed);
                velocity.y = new ParticleSystem.MinMaxCurve(.04f, .06f);
                GolemVfxSetup.ConfigureRoundedDust(particles, cloud, size);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
                GolemVfxSetup.ValidateRoundedDust(saved, cloud);
                if (name == "StepDust") GolemVfxSetup.ValidateWalkingSpread(saved, previousRadius, previousSpeed);
                return saved;
            }
            finally { PrefabUtility.UnloadPrefabContents(instance); }
        }

        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
