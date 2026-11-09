using System;
using System.Collections.Generic;
using System.Linq;
using UJam.Runtime.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UJam.Editor
{
    public static class GolemVfxSetup
    {
        private const float SpreadScale = .9f;
        private static readonly Color DustColor = new Color(.66f, .59f, .48f, .3f);
        private static readonly string[] ContactSignals = { "MoveStep", "AttackTrailStart", "AttackImpact", "DeathImpact" };

        public static void Configure(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            Transform hand = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(renderer => renderer.bones)
                .FirstOrDefault(bone => bone != null && bone.name.Equals("Hand_R", StringComparison.OrdinalIgnoreCase))
                ?? VfxAuthoring.Find(root, "Hand_R");
            Renderer[] eyes = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.name.StartsWith("Eye_", StringComparison.OrdinalIgnoreCase)
                    || renderer.name.IndexOf("_Eye_", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            SkinnedMeshRenderer handMesh = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(renderer => renderer.name.StartsWith("Hand_R", StringComparison.OrdinalIgnoreCase));
            Require(hand != null && handMesh != null, "Golem VFX requires the right-hand bone and renderer.");
            Require(eyes.Length == 2, "Golem EyesOff requires both separate eye renderers.");

            BoxCollider body = root.GetComponent<BoxCollider>();
            Require(body != null && body.bounds.size.x > 0 && body.bounds.size.z > 0, "Golem dust requires the measured body footprint.");
            Bounds footprint = body.bounds;
            float bodyEdge = Mathf.Max(footprint.extents.x, footprint.extents.z);
            float bodyRadius = new Vector2(footprint.extents.x, footprint.extents.z).magnitude;
            Transform ground = Anchor(root.transform, "GolemGroundContact", new Vector3(footprint.center.x,
                footprint.min.y + .15f, footprint.center.z));
            Transform strikeTip = Anchor(hand, "GolemHandStrikeTip", HandStrikeTipPosition(hand, handMesh));
            Transform stroke = Anchor(hand, "GolemHandStreak", handMesh.bounds.center);
            TrailRenderer trail = stroke.GetComponent<TrailRenderer>();
            if (trail == null) trail = stroke.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = VfxAuthoring.Material("Golem/HandStreak", new Color(.72f, .68f, .56f, .34f));
            trail.time = .1f;
            trail.minVertexDistance = .14f;
            trail.startWidth = .2f;
            trail.endWidth = .025f;
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

            Mesh cloud = RoundedCloudMesh();
            GameObject step = Dust("StepDust", cloud, 40, .85f, 1.1f, .95f, bodyEdge + .35f, bodyEdge);
            GameObject impact = Dust("HandImpactDust", cloud, 26, .85f, 1.4f, .9f, .65f);
            GameObject collapse = Dust("CollapseContactDust", cloud, 48, 1f, 1.1f, 1.1f, bodyEdge + .5f, bodyEdge + .05f);
            ValidateFootprint(collapse, bodyRadius);
            EnemyVfx vfx = root.GetComponent<EnemyVfx>();
            if (vfx != null) vfx.Bindings = vfx.Bindings.Where(binding => !ContactSignals.Contains(binding.Signal)).ToArray();
            VfxAuthoring.Bind(root, "MoveStep", step, ground);
            VfxAuthoring.Bind(root, "AttackImpact", impact, strikeTip, atTarget: true);
            VfxAuthoring.Bind(root, "DeathImpact", collapse, ground);
            vfx = root.GetComponent<EnemyVfx>();
            EnemyVfx.Binding strike = vfx.Bindings.Single(binding => binding.Signal == "AttackImpact");
            strike.UseAnchorPosition = true;
            strike.TargetOffset = Vector3.zero;
            vfx.Eyes = eyes;
            vfx.Trails = new[] { trail };
            Require(ContactSignals.All(signal => vfx.Bindings.Count(binding => binding.Signal == signal) == (signal == "AttackTrailStart" ? 0 : 1)),
                "Golem requires movement, arm-tip attack and death dust only; its hand trail must not emit dust.");
            Require(strike.AtTarget && strike.UseAnchorPosition && strike.Anchor == strikeTip && strikeTip.parent == hand
                && strike.Effect == impact && !strike.Follow
                && impact.GetComponent<ParticleSystem>().shape.shapeType == ParticleSystemShapeType.Sphere
                && impact.GetComponent<ParticleSystem>().shape.radius < 1 && strike.TargetOffset == Vector3.zero,
                "Golem attack dust must spawn on the animated hand's distal surface while retaining valid-hit gating.");
            Require(vfx.Eyes.All(eye => eye != null) && !trail.emitting,
                "Golem eye references and initial trail state are invalid.");
        }

        private static Transform Anchor(Transform parent, string name, Vector3 position)
        {
            Transform anchor = parent.Find(name);
            if (anchor == null) anchor = new GameObject(name).transform;
            anchor.gameObject.layer = parent.gameObject.layer;
            anchor.SetParent(parent, false);
            anchor.SetPositionAndRotation(position, Quaternion.identity);
            return anchor;
        }

        internal static Vector3 HandStrikeTipPosition(Transform hand, SkinnedMeshRenderer renderer)
        {
            Mesh mesh = renderer.sharedMesh;
            int bone = Array.IndexOf(renderer.bones, hand);
            Require(mesh != null && hand.parent != null && bone >= 0 && bone < mesh.bindposes.Length,
                "Golem strike tip requires the hand mesh's owning bone and bindpose.");
            Require(mesh.boneWeights.Length == mesh.vertexCount
                && mesh.boneWeights.All(weight => weight.boneIndex0 == bone && weight.weight0 > .999f),
                "Golem strike mesh must remain rigidly weighted to Hand_R.");
            Vector3 distal = hand.InverseTransformVector(hand.position - hand.parent.position).normalized;
            Require(distal.sqrMagnitude > .99f, "Golem forearm-to-hand direction must be valid.");
            Matrix4x4 meshToHand = mesh.bindposes[bone];
            Vector3[] points = mesh.vertices.Select(point => meshToHand.MultiplyPoint3x4(point)).ToArray();
            Require(points.Length > 0, "Golem strike hand must have physical surface vertices.");
            float near = points.Min(point => Vector3.Dot(point, distal));
            float far = points.Max(point => Vector3.Dot(point, distal));
            float capDepth = Mathf.Max((far - near) * .05f, .0001f);
            Vector3[] cap = points.Where(point => Vector3.Dot(point, distal) >= far - capDepth).ToArray();
            Require(cap.Length > 0, "Golem hand must have a finite distal surface cap.");
            Vector3 tip = cap.Aggregate(Vector3.zero, (sum, point) => sum + point) / cap.Length;
            Require(far > near && Vector3.Dot(tip, distal) >= far - capDepth - .0001f,
                "Golem strike anchor must lie on the hand's outermost distal surface cap.");
            return hand.TransformPoint(tip);
        }

        internal static Mesh RoundedCloudMesh()
        {
            const string path = "Assets/_Project/Art/EnemyVfx/Golem/RoundedDustCloud.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            float t = (1 + Mathf.Sqrt(5)) / 2;
            Vector3[] corners =
            {
                new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0),
                new Vector3(0,-1,t), new Vector3(0,1,t), new Vector3(0,-1,-t), new Vector3(0,1,-t),
                new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1)
            };
            int[] faces =
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            var sphere = new List<Vector3>(240);
            for (int i = 0; i < faces.Length; i += 3)
            {
                Vector3 a = corners[faces[i]].normalized, b = corners[faces[i + 1]].normalized, c = corners[faces[i + 2]].normalized;
                Vector3 ab = (a + b).normalized, bc = (b + c).normalized, ca = (c + a).normalized;
                sphere.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            Vector3[] centers = { new Vector3(-.22f,.02f,.04f), new Vector3(.22f,.06f,.03f), new Vector3(0,-.02f,-.24f) };
            float[] radii = { .52f, .58f, .48f };
            var vertices = new List<Vector3>(sphere.Count * centers.Length);
            for (int i = 0; i < centers.Length; i++)
                foreach (Vector3 point in sphere)
                {
                    float irregularity = 1 + .045f * Mathf.Sin(point.x * 11 + point.y * 7 + point.z * 5);
                    vertices.Add(centers[i] + point * (radii[i] * irregularity));
                }
            var bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (Vector3 point in vertices) bounds.Encapsulate(point);
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            // A bottom-origin mesh and Y-only rotations keep expanding clouds above the floor.
            Vector3 origin = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            for (int i = 0; i < vertices.Count; i++) vertices[i] = (vertices[i] - origin) / width;
            mesh = new Mesh { name = "Rounded lowpoly dust cloud (240 triangles)" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(Enumerable.Range(0, vertices.Count).ToArray(), 0);
            mesh.uv = new Vector2[vertices.Count];
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Require(mesh.triangles.Length / 3 == 240 && mesh.normals.Length == mesh.vertexCount && mesh.bounds.min.y >= -.0001f
                && Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z) <= 1.001f,
                "Rounded dust cloud must have 240 non-shared flat-shaded faces and a normalized, above-ground footprint.");
            VfxAuthoring.Ensure(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static GameObject Dust(string name, Mesh cloud, int count, float size, float speed, float lifetime, float radius, float innerRadius = 0)
        {
            float previousRadius = radius * SpreadScale, previousSpeed = speed * SpreadScale;
            radius *= SpreadScale;
            innerRadius *= SpreadScale;
            speed *= SpreadScale;
            if (name == "StepDust") { radius *= .9f; innerRadius *= .9f; speed *= .9f; }
            GameObject prefab = VfxAuthoring.Burst("Golem/" + name, DustColor, count, size, speed, lifetime);
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject instance = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ParticleSystem particles = instance.GetComponent<ParticleSystem>();
                bool annulus = innerRadius > 0;
                var main = particles.main;
                main.startSpeed = annulus ? new ParticleSystem.MinMaxCurve(speed * .8f, speed) : new ParticleSystem.MinMaxCurve(0);
                main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * .9f, lifetime);
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
                ConfigureRoundedDust(particles, cloud, size);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
                ValidateRoundedDust(saved, cloud);
                if (name == "StepDust") ValidateWalkingSpread(saved, previousRadius, previousSpeed);
                return saved;
            }
            finally { PrefabUtility.UnloadPrefabContents(instance); }
        }

        internal static void ConfigureRoundedDust(ParticleSystem particles, Mesh cloud, float size)
        {
            var main = particles.main;
            main.startRotation3D = true;
            main.startRotationX = main.startRotationZ = 0;
            main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startSize3D = true;
            main.startSizeX = main.startSizeZ = new ParticleSystem.MinMaxCurve(size * .65f, size);
            main.startSizeY = new ParticleSystem.MinMaxCurve(size * .325f, size * .5f);
            var sizes = particles.sizeOverLifetime;
            sizes.enabled = true;
            sizes.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
                new Keyframe(0, .3f), new Keyframe(.3f, 1.2f), new Keyframe(.65f, .95f), new Keyframe(1, 0)));
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var cloudFade = new Gradient();
            cloudFade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(.7f, .55f), new GradientAlphaKey(0, 1) });
            colors.color = cloudFade;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = cloud;
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.enableGPUInstancing = false;
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
                { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
            Material material = renderer.sharedMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            Require(shader != null, "Golem dust requires the installed URP particle Simple Lit shader.");
            material.shader = shader;
            material.SetTexture("_BaseMap", null);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_SpecColor", Color.black);
            material.SetFloat("_Smoothness", 0);
            material.SetFloat("_SpecularHighlights", 0);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.SetFloat("_AlphaClip", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            Require(renderer.renderMode == ParticleSystemRenderMode.Mesh && renderer.mesh == cloud && main.startSize3D
                && material.GetTexture("_BaseMap") == null && material.GetColor("_BaseColor") == Color.white
                && AssetDatabase.Contains(cloud) && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)),
                "Golem dust must use persistent rounded cloud geometry with white materials and no image texture.");
        }

        internal static void ValidateRoundedDust(GameObject effect, Mesh cloud)
        {
            Require(effect != null && AssetDatabase.Contains(effect), "Rounded dust must save as a persistent prefab.");
            ParticleSystem particles = effect.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
            var streams = new List<ParticleSystemVertexStream>();
            renderer.GetActiveVertexStreams(streams);
            Require(!particles.main.loop && effect.GetComponent<LowpolyVfxLifetime>() != null
                && effect.GetComponent<LowpolyVfxLifetime>().Lifetime > particles.main.startLifetime.constantMax,
                "Rounded dust must remain finite and clean itself up after its particles.");
            Require(renderer.mesh == cloud && AssetDatabase.Contains(cloud) && cloud.triangles.Length == 720
                && cloud.vertexCount == 720 && cloud.normals.Length == cloud.vertexCount
                && cloud.vertices.All(point => !float.IsNaN(point.sqrMagnitude) && !float.IsInfinity(point.sqrMagnitude)),
                "Rounded dust must reference the persistent finite 240-triangle cloud mesh.");
            Require(!renderer.enableGPUInstancing && streams.SequenceEqual(new[] { ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV }),
                "Rounded dust requires the non-instanced Simple Lit vertex stream layout.");
        }

        internal static void ValidateWalkingSpread(GameObject effect, float previousRadius, float previousSpeed)
        {
            ParticleSystem particles = effect.GetComponent<ParticleSystem>();
            float lifetime = particles.main.startLifetime.constantMax;
            float envelope = particles.shape.radius + particles.main.startSpeed.constantMax * lifetime;
            Require(particles.shape.shapeType == ParticleSystemShapeType.Circle
                && Mathf.Approximately(particles.shape.radius, previousRadius * .9f)
                && Mathf.Approximately(particles.main.startSpeed.constantMax, previousSpeed * .9f)
                && Mathf.Approximately(envelope, (previousRadius + previousSpeed * lifetime) * .9f),
                "Golem walking dust radius and outward travel must shrink exactly ten percent from the preceding version.");
        }

        private static void ValidateFootprint(GameObject effect, float bodyRadius)
        {
            ParticleSystem particles = effect.GetComponent<ParticleSystem>();
            var shape = particles.shape;
            float envelope = shape.radius + particles.main.startSpeed.constantMax * particles.main.startLifetime.constantMax;
            Require(shape.shapeType == ParticleSystemShapeType.Circle && shape.radiusThickness > 0 && shape.radiusThickness < 1
                && Mathf.Approximately(shape.sphericalDirectionAmount, 1) && envelope >= 3 && envelope > bodyRadius + .15f,
                "Golem dust clouds must keep a six-unit outward diameter beyond the collider footprint after the ten-percent reduction.");
            Debug.Log("[GolemVfxSetup] " + effect.name + " starts at radii "
                + (shape.radius * (1 - shape.radiusThickness)).ToString("F2") + "–" + shape.radius.ToString("F2")
                + "; outward center envelope=" + envelope.ToString("F2") + "; collider corner radius=" + bodyRadius.ToString("F2"));
        }

        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
