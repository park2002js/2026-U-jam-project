using System;
using System.Linq;
using UJam.Runtime.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    public static class RollbugVfxSetup
    {
        private const string Id = "Rollbug/";
        private const string Art = VfxAuthoring.Directory + Id;

        public static void Configure(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            RollbugEnemy enemy = root.GetComponent<RollbugEnemy>();
            Require(enemy != null, "Rollbug VFX requires RollbugEnemy.");
            ConfigureInfectedShell(root);
            GameObject detonation = Detonation();
            var serialized = new SerializedObject(enemy);
            serialized.FindProperty("_detonationEffect").objectReferenceValue = detonation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(detonation != null && serialized.FindProperty("_detonationEffect").objectReferenceValue == detonation,
                "Rollbug needs its persistent self-detonation effect.");
            ParticleSystem fire = detonation.GetComponentsInChildren<ParticleSystem>().Single(system => system.name == "OrangeFireLobes");
            Require(fire.main.startSize.constantMax >= 2f,
                "Rollbug self-detonation must keep its body-scale fire lobes.");
        }

        private static GameObject Detonation()
        {
            const string path = Art + "Detonation.prefab";
            var root = new GameObject("Detonation");
            try
            {
                Mesh lobe = LobeMesh();
                Material solid = SolidMaterial();
                ParticleSystem core = BlastLayer(root, root.name, 4, 2.1f, .24f, lobe, solid,
                    new Color(1f, .86f, .2f), new Color(1f, .65f, .06f), true);
                var coreShape = core.shape;
                coreShape.radius = .2f;
                var coreSize = core.sizeOverLifetime;
                coreSize.size = Curve(new Keyframe(0f, .12f), new Keyframe(.22f, 1f), new Keyframe(1f, 0f));

                ParticleSystem fire = BlastLayer(root, "OrangeFireLobes", 18, 2.15f, .72f, lobe, solid,
                    new Color(1f, .24f, .015f), new Color(1f, .68f, .05f));
                RadialLaunch(fire, 9f, 4f, .7f);
                var fireColor = fire.colorOverLifetime;
                fireColor.enabled = true;
                var cooling = new Gradient();
                cooling.SetKeys(new[] { new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, .7f, .35f), .45f),
                    new GradientColorKey(new Color(.28f, .15f, .1f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                fireColor.color = cooling;

                ParticleSystem smoke = BlastLayer(root, "CharcoalSmokeClusters", 14, 2.25f, 1.08f, lobe, solid,
                    new Color(.055f, .042f, .035f), new Color(.24f, .12f, .045f));
                var smokeMain = smoke.main;
                smokeMain.startDelay = .12f;
                var smokeShape = smoke.shape;
                smokeShape.radius = .55f;
                RadialLaunch(smoke, 5.3f, 1.2f, .12f);
                var rise = smoke.velocityOverLifetime;
                rise.y = .6f;
                var smokeSize = smoke.sizeOverLifetime;
                smokeSize.size = Curve(new Keyframe(0f, .18f), new Keyframe(.3f, .85f),
                    new Keyframe(.68f, 1.12f), new Keyframe(1f, 0f));

                ParticleSystem streaks = BlastLayer(root, "BlackOutwardStreaks", 11, 1.8f, .43f, StreakMesh(), solid,
                    new Color(.015f, .012f, .01f), new Color(.055f, .027f, .012f));
                RadialLaunch(streaks, 12f, 6f, 1f);
                streaks.GetComponent<ParticleSystemRenderer>().alignment = ParticleSystemRenderSpace.Velocity;
                var streakMain = streaks.main;
                streakMain.startRotationX = streakMain.startRotationY = streakMain.startRotationZ = 0f;
                var streakSize = streaks.sizeOverLifetime;
                streakSize.size = Curve(new Keyframe(0f, .2f), new Keyframe(.18f, 1f),
                    new Keyframe(.65f, .9f), new Keyframe(1f, 0f));

                ParticleSystem debris = BlastLayer(root, "BlackDebris", 18, .28f, .68f, lobe, solid,
                    new Color(.025f, .021f, .018f), new Color(.1f, .055f, .025f));
                var debrisMain = debris.main;
                debrisMain.startSpeed = new ParticleSystem.MinMaxCurve(2.7f, 4.7f);
                debrisMain.gravityModifier = .9f;

                ParticleSystem dust = BlastLayer(root, "LowGroundDust", 18, .8f, .6f, lobe, solid,
                    new Color(.3f, .19f, .1f), new Color(.52f, .34f, .15f));
                dust.transform.localPosition = Vector3.up * .09f;
                var dustMain = dust.main;
                dustMain.startSize3D = true;
                dustMain.startSizeX = dustMain.startSizeZ = new ParticleSystem.MinMaxCurve(.5f, .8f);
                dustMain.startSizeY = .12f;
                dustMain.startRotationX = dustMain.startRotationZ = 0f;
                var dustShape = dust.shape;
                dustShape.shapeType = ParticleSystemShapeType.Circle;
                dustShape.rotation = new Vector3(90f, 0f, 0f);
                dustShape.position = Vector3.zero;
                RadialLaunch(dust, 8.5f, 2.5f, .3f);

                ParticleSystem wave = BlastLayer(root, "GroundShockwave", 1, 1f, .48f, RingMesh(),
                    VfxAuthoring.Material(Id + "DetonationShockwave", new Color(1f, .55f, .13f, .72f)), Color.white, Color.white);
                wave.transform.localPosition = Vector3.up * .045f;
                var waveShape = wave.shape;
                waveShape.enabled = false;
                var waveMain = wave.main;
                waveMain.startSize = 1f;
                waveMain.startRotationX = waveMain.startRotationY = waveMain.startRotationZ = 0f;
                var waveSize = wave.sizeOverLifetime;
                waveSize.size = Curve(new Keyframe(0f, .3f), new Keyframe(.35f, 4.5f), new Keyframe(1f, 8f));
                var waveFade = wave.colorOverLifetime;
                waveFade.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(.65f, .45f), new GradientAlphaKey(0f, 1f) });
                waveFade.color = gradient;
                root.AddComponent<LowpolyVfxLifetime>().Lifetime = 1.35f;
                ValidateBurst(root, 7, 84);
                Require(lobe.triangles.Length == 60 && lobe.colors.Length == lobe.vertexCount
                    && solid.GetFloat("_ZWrite") == 1f && solid.GetFloat("_Surface") == 0f,
                    "Rollbug fire and smoke must remain solid flat-shaded geometry.");
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                Require(saved != null, "Could not save the Rollbug detonation.");
                ValidateBurst(saved, 7, 84);
                return saved;
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static ParticleSystem BlastLayer(GameObject root, string name, int count, float size, float lifetime,
            Mesh mesh, Material material, Color minimum, Color maximum, bool onRoot = false)
        {
            GameObject child = onRoot ? root : new GameObject(name);
            if (!onRoot)
            {
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = Vector3.up * .85f;
            }
            ParticleSystem particles = child.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = (uint)(100 + root.GetComponentsInChildren<ParticleSystem>().Length * 17);
            var main = particles.main;
            main.duration = .05f;
            main.loop = false;
            main.playOnAwake = true;
            main.maxParticles = count;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * .65f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(minimum, maximum);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation3D = true;
            main.startRotationX = main.startRotationY = main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.position = onRoot ? Vector3.up * .85f : Vector3.zero;
            shape.radius = .35f;
            var scale = particles.sizeOverLifetime;
            scale.enabled = true;
            scale.size = Curve(new Keyframe(0f, .16f), new Keyframe(.22f, 1f),
                new Keyframe(.55f, 1.05f), new Keyframe(1f, 0f));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.mesh = mesh;
            renderer.sharedMaterial = material;
            renderer.enableGPUInstancing = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        private static void RadialLaunch(ParticleSystem particles, float start, float middle, float end)
        {
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.radial = Curve(new Keyframe(0f, start), new Keyframe(.25f, middle),
                new Keyframe(.6f, end), new Keyframe(1f, 0f));
        }

        private static ParticleSystem.MinMaxCurve Curve(params Keyframe[] keys) =>
            new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));

        private static Material SolidMaterial()
        {
            Material material = VfxAuthoring.Material(Id + "SolidFacets", Color.white);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetShaderPassEnabled("DepthOnly", true);
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh LobeMesh()
        {
            float t = (1f + Mathf.Sqrt(5f)) * .5f;
            Vector3[] vertices = { new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0),
                new Vector3(0,-1,t), new Vector3(0,1,t), new Vector3(0,-1,-t), new Vector3(0,1,-t),
                new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1) };
            int[] faces = { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            return MeshAsset("FireSmokeLobe", faces.Select(index => vertices[index].normalized * .5f).ToArray());
        }

        private static Mesh StreakMesh() => MeshAsset("BlackStreak", new[] {
            new Vector3(0,0,.75f), new Vector3(-.055f,-.025f,-.5f), new Vector3(.055f,-.025f,-.5f),
            new Vector3(0,0,.75f), new Vector3(.055f,-.025f,-.5f), new Vector3(0,.04f,-.5f),
            new Vector3(0,0,.75f), new Vector3(0,.04f,-.5f), new Vector3(-.055f,-.025f,-.5f),
            new Vector3(-.055f,-.025f,-.5f), new Vector3(0,.04f,-.5f), new Vector3(.055f,-.025f,-.5f) });

        private static Mesh RingMesh()
        {
            var points = new Vector3[24 * 6];
            for (int index = 0; index < 24; index++)
            {
                float a = index * Mathf.PI * 2f / 24f, b = (index + 1) * Mathf.PI * 2f / 24f;
                Vector3 outerA = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * .5f;
                Vector3 outerB = new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * .5f;
                int start = index * 6;
                points[start] = outerA; points[start + 1] = outerA * .92f; points[start + 2] = outerB;
                points[start + 3] = outerB; points[start + 4] = outerA * .92f; points[start + 5] = outerB * .92f;
            }
            return MeshAsset("ShockwaveRing", points);
        }

        private static Mesh MeshAsset(string name, Vector3[] vertices)
        {
            string path = Art + name + ".asset";
            VfxAuthoring.Ensure(path);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = Enumerable.Range(0, vertices.Length).ToArray();
            mesh.colors = vertices.Select((_, index) => Color.white * (.72f + .28f * ((index / 3 * 7) % 13) / 12f)).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void ValidateBurst(GameObject root, int layerCount, int budget)
        {
            var cleanup = root.GetComponent<LowpolyVfxLifetime>();
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            Require(cleanup != null && cleanup.Lifetime <= 1.35f && systems.Length == layerCount
                && systems.Sum(system => system.main.maxParticles) <= budget,
                "Rollbug explosion layers must keep finite cleanup and their particle budget.");
            foreach (ParticleSystem system in systems)
            {
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                Require(!system.main.loop && system.main.playOnAwake && system.emission.rateOverTime.constantMax == 0f
                    && !system.collision.enabled && !system.trigger.enabled
                    && system.main.startDelay.constantMax + system.main.startLifetime.constantMax <= cleanup.Lifetime
                    && renderer.renderMode == ParticleSystemRenderMode.Mesh && AssetDatabase.Contains(renderer.mesh)
                    && AssetDatabase.Contains(renderer.sharedMaterial) && renderer.sharedMaterial.GetTexture("_BaseMap") == null,
                    "Rollbug explosion must use saved texture-free geometric bursts with no damage or collision behaviour.");
            }
            Require(root.GetComponentsInChildren<Collider>(true).Length == 0
                && root.GetComponentsInChildren<EnemyBase>(true).Length == 0, "Rollbug VFX must not add gameplay components.");
        }

        private static void ConfigureInfectedShell(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Material[] sources = renderers.SelectMany(renderer => renderer.sharedMaterials).Distinct().ToArray();
            Require(sources.Length == 1 && sources[0] != null,
                "Rollbug emission expects the documented single palette material.");
            Material source = sources[0];
            Texture palette = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
            if (palette == null && source.HasProperty("_MainTex")) palette = source.GetTexture("_MainTex");
            Require(palette != null, "Rollbug palette texture is missing.");
            Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
            Vector2 scale = source.mainTextureScale, offset = source.mainTextureOffset;
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "InfectedEmission.png");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Require(mask != null && mask.width == 128 && mask.height == 128 && shader != null,
                "Rollbug requires its palette emission mask and URP Lit shader.");
            string path = Art + "InfectedShell.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                VfxAuthoring.Ensure(path);
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material != source) EditorUtility.CopySerialized(source, material);
            // shortcut: mask follows the approved 4x4 palette; regenerate it if the palette layout changes.
            material.shader = shader;
            material.SetTexture("_BaseMap", palette);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .1f);
            material.SetFloat("_Metallic", 0f);
            material.SetTexture("_EmissionMap", mask);
            material.SetColor("_EmissionColor", new Color(.22f, .22f, .22f));
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
            Require(material.GetTexture("_BaseMap") == palette && material.GetColor("_BaseColor") == color
                && renderers.All(renderer => renderer.sharedMaterials.All(item => item == material)),
                "Rollbug must preserve its palette while using a separate masked emission material.");
        }

        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
