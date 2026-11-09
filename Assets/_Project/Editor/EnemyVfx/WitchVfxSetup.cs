using System;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UJam.Runtime.Enemy.Projectiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    public static class WitchVfxSetup
    {
        private const string Directory = "Assets/_Project/Art/EnemyVfx/Witch/";
        private const string CrystalName = "Witch_Staff_Crystal_20_Faces";
        private const float CasterRadius = 2.5f;
        private static readonly Color Spell = new Color(.65f, .79f, .86f);
        private static readonly Color GhostMint = new Color(.82f, .98f, .88f, .9f);
        private static readonly Color DarkTeal = new Color(.025f, .12f, .11f, .85f);

        public static void Configure(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            RangedEnemy ranged = root.GetComponent<RangedEnemy>();
            if (ranged == null) throw new InvalidOperationException("Witch VFX requires RangedEnemy.");
            WitchEnemy witch = root.GetComponent<WitchEnemy>();
            if (witch == null) throw new InvalidOperationException("Witch VFX requires WitchEnemy.");
            EnsureDirectory();
            Mesh orb = OrbMesh();
            Mesh shard = ShardMesh();
            Mesh ring = RingMesh();
            Material spell = Material("SpellParticles", Spell, true);
            Material summonParticles = Material("SummonParticles", Color.white, true);
            Material summonDust = Material("SummonDustParticles", Color.white, false);
            Material ash = Material("AshParticles", Color.white, false);
            GameObject launch = Launch(shard, spell);
            GameObject impact = Impact(shard, spell);
            GameObject summon = Summon(ring, shard, summonParticles, summonDust);
            GameObject casterField = CasterField(CasterFieldMesh(), Material("CasterFieldParticles", Color.white, true));

            var serialized = new SerializedObject(ranged);
            SerializedProperty projectile = Required(serialized, "_projectilePrefab");
            Projectile source = projectile.objectReferenceValue as Projectile;
            if (source == null) throw new InvalidOperationException("Witch has no projectile prefab to preserve.");
            projectile.objectReferenceValue = ProjectilePrefab(source, orb, shard, spell, impact);
            SkinnedMeshRenderer crystal = StaffCrystal(root);
            Transform firePoint = ConfigureMuzzle(root, crystal,
                Required(serialized, "_firePoint").objectReferenceValue as Transform);
            Required(serialized, "_firePoint").objectReferenceValue = firePoint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EnemyVfx vfx = root.GetComponent<EnemyVfx>();
            if (vfx != null)
                vfx.Bindings = Array.FindAll(vfx.Bindings ?? Array.Empty<EnemyVfx.Binding>(), binding => binding != null
                    && binding.Signal != "AttackImpact" && binding.Signal != "SummonImpact" && binding.Signal != "SummonStart");
            VfxAuthoring.Bind(root, "AttackImpact", launch, firePoint);

            float offset = Required(new SerializedObject(witch), "_summonOffset").floatValue;
            VfxAuthoring.Bind(root, "SummonStart", casterField, Anchor(root, "WitchCasterFieldVfx", 0), follow: true);
            VfxAuthoring.Bind(root, "SummonImpact", summon, Anchor(root, "WitchSummonLeftVfx", -offset));
            VfxAuthoring.Bind(root, "SummonImpact", summon, Anchor(root, "WitchSummonRightVfx", offset));
            EnhanceAsh(root, shard, ash);
            CrystalGlowAuthoring.Configure(root, new Renderer[] { crystal }, new Color(.65f, .32f, 1f), "Witch");
            Require(root.transform.Find("Model").GetComponentsInChildren<Renderer>(true).Length == 12,
                "Witch crystal glow must stay outside Model and preserve its nine body/three staff renderers.");
            float muzzleError = ValidateMuzzle(root);
            Debug.Log("[WitchVfxSetup] Attack F33 fire point is at the staff crystal center; error=" + muzzleError.ToString("F6") + "m.");
            ValidateSummonField(root);
            Require(AssetDatabase.Contains(projectile.objectReferenceValue), "Witch projectile must be a saved asset.");
            AssetDatabase.SaveAssets();
        }

        public static void ValidateSummonField(GameObject root)
        {
            EnemyVfx vfx = root != null ? root.GetComponent<EnemyVfx>() : null;
            Require(vfx != null && vfx.Bindings != null, "Witch summon field requires its VFX dispatcher.");
            EnemyVfx.Binding[] fields = Array.FindAll(vfx.Bindings, binding => binding != null && binding.Signal == "SummonStart");
            Require(fields.Length == 1 && fields[0].Follow && !fields[0].AtTarget && fields[0].Effect != null,
                "Witch requires exactly one caster-following SummonStart field.");
            Transform anchor = fields[0].Anchor;
            Require(anchor != null && anchor.parent == root.transform
                && Vector3.Distance(anchor.localPosition, new Vector3(0, .035f, 0)) < .0001f,
                "Witch summon field must be centered at the caster's ground position.");
            ParticleSystem field = fields[0].Effect.GetComponent<ParticleSystem>();
            Require(field != null && !field.main.loop && field.main.playOnAwake
                && field.main.simulationSpace == ParticleSystemSimulationSpace.Local
                && Mathf.Abs(field.main.startLifetime.constant - 1.5f) < .0001f
                && Mathf.Abs(field.main.startSize.constant - 2.5f) < .0001f
                && field.GetComponent<ParticleSystemRenderer>().alignment == ParticleSystemRenderSpace.World,
                "Witch summon field must remain horizontal at 2.5m radius and expire with its 1.5 second summon.");
            AnimationCurve size = field.sizeOverLifetime.size.curve;
            Gradient fade = field.colorOverLifetime.color.gradient;
            Require(size != null && size.Evaluate(0) < .2f && Mathf.Abs(size.Evaluate(.35f / 1.5f) - 1) < .001f
                && Mathf.Abs(size.Evaluate(1.25f / 1.5f) - 1) < .001f && fade.Evaluate(.5f).a > .7f
                && fade.Evaluate(1).a < .001f,
                "Witch summon field must expand, hold through summoning, then extinguish.");
            Transform filled = fields[0].Effect.transform.Find("FilledArea");
            ParticleSystem area = filled != null ? filled.GetComponent<ParticleSystem>() : null;
            Require(area != null && area.main.startColor.color.a >= .08f && area.main.startColor.color.a <= .12f
                && area.main.simulationSpace == ParticleSystemSimulationSpace.Local
                && Mathf.Abs(area.main.startLifetime.constant - 1.5f) < .0001f
                && Mathf.Abs(area.main.startSize.constant - 2.5f) < .0001f
                && area.main.startColor.color.r > .5f && area.main.startColor.color.g > area.main.startColor.color.b
                && Mathf.Abs(area.sizeOverLifetime.size.curve.Evaluate(.35f / 1.5f) - 1) < .001f
                && area.colorOverLifetime.color.gradient.Evaluate(1).a < .001f
                && area.GetComponent<ParticleSystemRenderer>().mesh.triangles.Length == 32 * 3,
                "Witch summon field requires a faint filled 32-triangle area with the same expansion and fade.");
            Transform outer = fields[0].Effect.transform.Find("DarkOuterEdge");
            ParticleSystem edge = outer != null ? outer.GetComponent<ParticleSystem>() : null;
            Require(edge != null, "Witch summon field requires its dark contrast edge.");
            Color bright = field.main.startColor.color, dark = edge.main.startColor.color;
            ParticleSystemRenderer edgeRenderer = edge.GetComponent<ParticleSystemRenderer>();
            Require(bright.r > .75f && bright.g > .9f && bright.b > .8f && bright.g >= bright.r && bright.g >= bright.b
                && dark.r < .1f && dark.g < .2f && dark.b < .2f && dark.a > .7f && bright.grayscale - dark.grayscale > .65f
                && edge.main.simulationSpace == ParticleSystemSimulationSpace.Local
                && Mathf.Abs(edge.main.startLifetime.constant - 1.5f) < .0001f
                && edgeRenderer.alignment == ParticleSystemRenderSpace.World
                && edgeRenderer.sharedMaterial.GetFloat("_DstBlend") == (float)BlendMode.OneMinusSrcAlpha
                && Mathf.Abs(edgeRenderer.mesh.bounds.extents.x * edge.main.startSize.constant - 2.5f) < .001f
                && edge.colorOverLifetime.color.gradient.Evaluate(1).a < .001f,
                "Witch summon field needs a bright desaturated mint inner rim and dark alpha-blended teal outer rim at 2.5m.");
            EnemyVfx.Binding[] destinations = Array.FindAll(vfx.Bindings, binding => binding != null && binding.Signal == "SummonImpact");
            Require(destinations.Length == 2,
                "Witch summon field must preserve both skeleton destination rings.");
            foreach (EnemyVfx.Binding destination in destinations)
            {
                ParticleSystem particles = destination.Effect.GetComponent<ParticleSystem>();
                Color tint = particles.main.startColor.color;
                Require(tint.r > .75f && tint.g > .9f && tint.b > .8f
                    && AssetDatabase.GetAssetPath(particles.GetComponent<ParticleSystemRenderer>().sharedMaterial) == Directory + "SummonParticles.mat",
                    "Witch skeleton destination rings must use their own matching ghostly mint material.");
            }
        }

        private static GameObject CasterField(Mesh mesh, Material material)
        {
            var root = new GameObject("WitchCasterField");
            try
            {
                ParticleSystem field = Particles(root, mesh, material, 1, CasterRadius, 0, 1.5f, GhostMint);
                FieldTiming(field);
                ParticleSystem area = Child(root, "FilledArea", CasterFieldDiscMesh(),
                    Material("CasterFieldArea", Color.white, false), 1, CasterRadius, 0, 1.5f, new Color(.7f, .92f, .78f, .1f));
                area.transform.localPosition = Vector3.down * .002f;
                FieldTiming(area);
                ParticleSystem edge = Child(root, "DarkOuterEdge", CasterOuterEdgeMesh(),
                    Material("CasterFieldOuterEdge", Color.white, false), 1, CasterRadius, 0, 1.5f, DarkTeal);
                edge.transform.localPosition = Vector3.down * .001f;
                FieldTiming(edge);
                return Save(root, "CasterField");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void FieldTiming(ParticleSystem field)
        {
            var main = field.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSize = CasterRadius;
            main.startRotationX = main.startRotationY = main.startRotationZ = 0;
            field.GetComponent<ParticleSystemRenderer>().alignment = ParticleSystemRenderSpace.World;
            var size = field.sizeOverLifetime;
            size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
                new Keyframe(0, .12f), new Keyframe(.35f / 1.5f, 1), new Keyframe(1, 1)));
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .09f / 1.5f),
                    new GradientAlphaKey(1, 1.25f / 1.5f), new GradientAlphaKey(0, 1) });
            var color = field.colorOverLifetime;
            color.color = gradient;
        }

        public static float ValidateMuzzle(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            AnimationClip attack = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/_Project/Art/EnemyChar/Witch/Witch_Attack.anim");
            Require(attack != null, "Witch muzzle validation requires its Attack clip.");
            GameObject sample = Object.Instantiate(root);
            sample.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Transform model = sample.transform.Find("Model");
                Require(model != null, "Witch muzzle validation requires Model.");
                foreach (Animator animator in sample.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                attack.SampleAnimation(model.gameObject, 33 / 60f);
                RangedEnemy ranged = sample.GetComponent<RangedEnemy>();
                Require(ranged != null, "Witch muzzle validation requires RangedEnemy.");
                Transform firePoint = Required(new SerializedObject(ranged), "_firePoint").objectReferenceValue as Transform;
                Require(firePoint != null && firePoint.parent != null && firePoint.parent.name == "Staff",
                    "Witch fire point must follow the Staff bone.");
                float distance = Vector3.Distance(firePoint.position, BakedCenter(StaffCrystal(sample)));
                Require(distance <= .03f, "Witch Attack F33 muzzle is " + distance.ToString("F6")
                    + "m from the posed staff crystal center (maximum .03m).");
                EnemyVfx vfx = sample.GetComponent<EnemyVfx>();
                Require(vfx != null && vfx.Bindings != null && Array.FindAll(vfx.Bindings, binding => binding != null
                    && binding.Signal == "AttackImpact" && binding.Anchor == firePoint).Length == 1,
                    "Witch launch VFX must bind exactly once to the crystal muzzle.");
                return distance;
            }
            finally { Object.DestroyImmediate(sample); }
        }

        private static Transform ConfigureMuzzle(GameObject root, SkinnedMeshRenderer crystal, Transform firePoint)
        {
            Transform staff = Array.Find(root.transform.Find("Model").GetComponentsInChildren<Transform>(true),
                bone => bone.name == "Staff");
            Require(staff != null, "Witch staff crystal requires its exact Staff bone.");
            if (firePoint == null) firePoint = new GameObject("FirePoint").transform;
            firePoint.gameObject.layer = root.layer;
            firePoint.SetParent(staff, false);
            // Skinning supplies the physical crystal center; body bounds place this near the hand instead.
            firePoint.localPosition = staff.InverseTransformPoint(BakedCenter(crystal));
            firePoint.localRotation = Quaternion.identity;
            firePoint.localScale = Vector3.one;
            return firePoint;
        }

        private static SkinnedMeshRenderer StaffCrystal(GameObject root)
        {
            Transform model = root.transform.Find("Model");
            Require(model != null, "Witch VFX requires Model.");
            SkinnedMeshRenderer crystal = Array.Find(model.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                renderer => renderer.name == CrystalName);
            Require(crystal != null && crystal.sharedMesh != null, "Witch VFX requires exact crystal " + CrystalName + ".");
            return crystal;
        }

        private static Vector3 BakedCenter(SkinnedMeshRenderer crystal)
        {
            var baked = new Mesh();
            try
            {
                crystal.BakeMesh(baked);
                Vector3[] vertices = baked.vertices;
                Require(vertices.Length > 0, "Witch staff crystal has no posed vertices.");
                Vector3 center = Vector3.zero;
                foreach (Vector3 vertex in vertices) center += vertex;
                return crystal.transform.TransformPoint(center / vertices.Length);
            }
            finally { Object.DestroyImmediate(baked); }
        }

        private static GameObject Launch(Mesh shard, Material material)
        {
            GameObject root = new GameObject("WitchLaunch");
            try
            {
                ParticleSystem flash = Particles(root, shard, material, 10, .085f, 1.2f, .3f, Spell);
                var shape = flash.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = .025f;
                return Save(root, "Launch");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject Impact(Mesh shard, Material material)
        {
            GameObject root = new GameObject("WitchImpact");
            try
            {
                ParticleSystem chips = Particles(root, shard, material, 15, .1f, 1.3f, .5f, Spell);
                var shape = chips.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = .06f;
                ParticleSystem mist = Child(root, "GrayFragments", shard, material, 9, .14f, .35f, .4f,
                    new Color(.58f, .64f, .7f, .75f));
                var mistShape = mist.shape;
                mistShape.enabled = true;
                mistShape.shapeType = ParticleSystemShapeType.Sphere;
                mistShape.radius = .12f;
                return Save(root, "Impact");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject Summon(Mesh ring, Mesh shard, Material material, Material dustMaterial)
        {
            GameObject root = new GameObject("WitchSummon");
            try
            {
                ParticleSystem outer = Particles(root, ring, material, 1, 1.1f, 0, .9f, GhostMint);
                outer.GetComponent<ParticleSystemRenderer>().alignment = ParticleSystemRenderSpace.World;
                var main = outer.main;
                main.startRotationX = main.startRotationY = main.startRotationZ = 0;
                var size = outer.sizeOverLifetime;
                size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .55f, 1, 1.35f));
                ParticleSystem inner = Child(root, "InnerOctagon", ring, material, 1, .65f, 0, .65f,
                    new Color(.94f, 1, .88f));
                inner.GetComponent<ParticleSystemRenderer>().alignment = ParticleSystemRenderSpace.World;
                main = inner.main;
                main.startDelay = .1f;
                main.startRotationX = main.startRotationZ = 0;
                main.startRotationY = Mathf.PI / 8;
                size = inner.sizeOverLifetime;
                size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .5f, 1, 1.5f));
                ParticleSystem shards = Child(root, "RisingShards", shard, material, 14, .16f, 1.3f, .7f, GhostMint);
                shards.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                var shape = shards.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.radius = .45f;
                shape.angle = 25;
                main = shards.main;
                main.gravityModifier = .25f;
                ParticleSystem dust = Child(root, "GroundDust", shard, dustMaterial, 18, .13f, .35f, .8f,
                    new Color(.32f, .45f, .4f, .8f));
                dust.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                shape = dust.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.radius = .65f;
                shape.angle = 65;
                return Save(root, "Summon");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Projectile ProjectilePrefab(Projectile source, Mesh orb, Mesh shard, Material particles, GameObject impact)
        {
            GameObject root = Object.Instantiate(source.gameObject);
            root.name = "WitchLowpolyProjectile";
            try
            {
                if (PrefabUtility.IsPartOfPrefabInstance(root))
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                // Keep the movement/damage component; only replace the source prefab's visual children.
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                var visual = new GameObject("PaleBlueGrayOrb", typeof(MeshFilter), typeof(MeshRenderer));
                visual.layer = root.layer;
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = Vector3.one * .17f;
                visual.GetComponent<MeshFilter>().sharedMesh = orb;
                MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = Material("Orb", Spell, false, false);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                ParticleSystem trail = Child(root, "FacetedTrail", shard, particles, 0, .07f, .035f, .32f,
                    new Color(.62f, .75f, .82f, .7f));
                var main = trail.main;
                main.loop = true;
                main.duration = .5f;
                main.stopAction = ParticleSystemStopAction.None;
                var emission = trail.emission;
                emission.SetBursts(new ParticleSystem.Burst[0]);
                emission.rateOverTime = 35;
                var shape = trail.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = .055f;
                Projectile projectile = root.GetComponent<Projectile>();
                var serialized = new SerializedObject(projectile);
                Required(serialized, "_impactEffect").objectReferenceValue = impact;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                GameObject saved = Save(root, "Projectile");
                Require(saved.GetComponent<Projectile>() != null, "Witch projectile lost its damage/movement component.");
                return saved.GetComponent<Projectile>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void EnhanceAsh(GameObject root, Mesh shard, Material material)
        {
            Transform death = root.transform.Find("DeathVFX");
            if (death == null) return;
            foreach (ParticleSystem system in death.GetComponentsInChildren<ParticleSystem>(true))
            {
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = shard;
                renderer.sharedMaterial = material;
                var main = system.main;
                main.startRotation3D = true;
                main.startRotationX = main.startRotationY = main.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
                Require(!main.playOnAwake, "Witch ash must remain controlled by WitchDeathVfx.");
            }
        }

        private static ParticleSystem Child(GameObject parent, string name, Mesh mesh, Material material,
            int count, float size, float speed, float lifetime, Color color)
        {
            var child = new GameObject(name);
            child.layer = parent.layer;
            child.transform.SetParent(parent.transform, false);
            ParticleSystem system = Particles(child, mesh, material, count, size, speed, lifetime, color);
            var main = system.main;
            main.stopAction = ParticleSystemStopAction.None;
            return system;
        }

        private static ParticleSystem Particles(GameObject root, Mesh mesh, Material material,
            int count, float size, float speed, float lifetime, Color color)
        {
            ParticleSystem system = root.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.duration = .1f;
            main.playOnAwake = true;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(size * .65f, size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .5f, speed);
            main.startColor = color;
            main.startRotation3D = true;
            main.startRotationX = main.startRotationY = main.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.maxParticles = Mathf.Max(64, count);
            var emission = system.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
            var shape = system.shape;
            shape.enabled = false;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .35f), new GradientAlphaKey(0, 1) });
            var overColor = system.colorOverLifetime;
            overColor.enabled = true;
            overColor.color = fade;
            var overSize = system.sizeOverLifetime;
            overSize.enabled = true;
            overSize.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, .1f));
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        private static Transform Anchor(GameObject root, string name, float x)
        {
            Transform anchor = root.transform.Find(name);
            if (anchor == null)
            {
                anchor = new GameObject(name).transform;
                anchor.SetParent(root.transform, false);
            }
            anchor.localPosition = new Vector3(x, .035f, 0);
            anchor.localRotation = Quaternion.identity;
            return anchor;
        }

        private static Material Material(string name, Color color, bool additive, bool particle = true)
        {
            string path = Directory + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(particle ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Lit");
            Require(shader != null, "Witch VFX requires the URP shader " + name + ".");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            if (particle)
            {
                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", additive ? 2 : 0);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetFloat("_ZWrite", 0);
                material.SetFloat("_Cull", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.SetFloat("_Metallic", .05f);
                material.SetFloat("_Smoothness", .2f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh OrbMesh()
        {
            const float t = 1.618034f;
            Vector3[] vertices = {
                new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0),
                new Vector3(0,-1,t), new Vector3(0,1,t), new Vector3(0,-1,-t), new Vector3(0,1,-t),
                new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1)
            };
            for (int i = 0; i < vertices.Length; i++) vertices[i] = vertices[i].normalized;
            return Mesh("Orb20Faces", vertices, new[] {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            });
        }

        private static Mesh ShardMesh() => Mesh("AshShard", new[] {
            new Vector3(0,.65f,0), new Vector3(-.3f,-.3f,-.2f), new Vector3(.3f,-.3f,-.2f), new Vector3(0,-.3f,.3f)
        }, new[] { 0,2,1, 0,3,2, 0,1,3, 1,2,3 });

        private static Mesh CasterFieldDiscMesh()
        {
            var vertices = new Vector3[33];
            var triangles = new int[32 * 3];
            for (int segment = 0; segment < 32; segment++)
            {
                float angle = segment * Mathf.PI / 16;
                vertices[segment + 1] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                triangles[segment * 3] = 0;
                triangles[segment * 3 + 1] = segment + 1;
                triangles[segment * 3 + 2] = (segment + 1) % 32 + 1;
            }
            return Mesh("CasterFieldDisc32", vertices, triangles);
        }

        private static Mesh CasterFieldMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float[] inner = { .945f, .76f, .84f };
            float[] outer = { .965f, .78f, .87f };
            for (int band = 0; band < inner.Length; band++)
                for (int segment = 0; segment < 32; segment++)
                {
                    if (band == 2 && segment % 4 == 3) continue;
                    float a = segment * Mathf.PI / 16;
                    float b = (segment + 1) * Mathf.PI / 16;
                    int first = vertices.Count;
                    vertices.Add(new Vector3(Mathf.Cos(a) * outer[band], 0, Mathf.Sin(a) * outer[band]));
                    vertices.Add(new Vector3(Mathf.Cos(b) * outer[band], 0, Mathf.Sin(b) * outer[band]));
                    vertices.Add(new Vector3(Mathf.Cos(a) * inner[band], 0, Mathf.Sin(a) * inner[band]));
                    vertices.Add(new Vector3(Mathf.Cos(b) * inner[band], 0, Mathf.Sin(b) * inner[band]));
                    triangles.AddRange(new[] { first, first + 1, first + 2, first + 1, first + 3, first + 2 });
                }
            return Mesh("CasterFieldGhostRings32", vertices.ToArray(), triangles.ToArray());
        }

        private static Mesh CasterOuterEdgeMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int segment = 0; segment < 32; segment++)
            {
                float a = segment * Mathf.PI / 16;
                float b = (segment + 1) * Mathf.PI / 16;
                int first = vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                vertices.Add(new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)));
                vertices.Add(new Vector3(Mathf.Cos(a) * .96f, 0, Mathf.Sin(a) * .96f));
                vertices.Add(new Vector3(Mathf.Cos(b) * .96f, 0, Mathf.Sin(b) * .96f));
                triangles.AddRange(new[] { first, first + 1, first + 2, first + 1, first + 3, first + 2 });
            }
            return Mesh("CasterFieldOuterEdge32", vertices.ToArray(), triangles.ToArray());
        }

        private static Mesh RingMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                vertices.Add(new Vector3(Mathf.Cos(angle) * .65f, 0, Mathf.Sin(angle) * .65f));
                vertices.Add(new Vector3(Mathf.Cos(angle) * .56f, 0, Mathf.Sin(angle) * .56f));
                int next = (i + 1) % 8 * 2;
                triangles.AddRange(new[] { i * 2, next, i * 2 + 1, next, next + 1, i * 2 + 1 });
            }
            return Mesh("OctagonalRing", vertices.ToArray(), triangles.ToArray());
        }

        private static Mesh Mesh(string name, Vector3[] vertices, int[] triangles)
        {
            string path = Directory + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            var flat = new Vector3[triangles.Length];
            var indices = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i++) { flat[i] = vertices[triangles[i]]; indices[i] = i; }
            mesh = new Mesh { name = name, vertices = flat, triangles = indices };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            Require(mesh.vertexCount == triangles.Length && AssetDatabase.Contains(mesh), "Witch lowpoly mesh was not saved.");
            return mesh;
        }

        private static GameObject Save(GameObject root, string name)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Directory + name + ".prefab");
            Require(prefab != null && AssetDatabase.Contains(prefab), "Could not save Witch " + name + ".");
            foreach (ParticleSystemRenderer renderer in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
                Require(renderer.renderMode == ParticleSystemRenderMode.Mesh && renderer.mesh != null
                    && AssetDatabase.Contains(renderer.mesh) && renderer.sharedMaterial != null
                    && AssetDatabase.Contains(renderer.sharedMaterial), "Witch mesh particle has a transient or missing asset.");
            return prefab;
        }

        private static void EnsureDirectory()
        {
            string[] parts = Directory.TrimEnd('/').Split('/');
            string path = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = path + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(path, parts[i]);
                path = next;
            }
        }

        private static SerializedProperty Required(SerializedObject serialized, string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException(serialized.targetObject.GetType().Name + " requires " + name + ".");
            return property;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
