using System;
using System.Collections.Generic;
using System.Linq;
using UJam.Runtime.Enemy;
using UnityEditor;
using UnityEngine;

namespace UJam.Editor
{
    public static class SlimeVfxSetup
    {
        public static void Configure(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            Require(root.GetComponent<SlimeEnemy>() != null, "Slime VFX requires SlimeEnemy.");
            Transform ground = root.transform.Find("SlimeGroundVfx");
            if (ground == null)
            {
                ground = new GameObject("SlimeGroundVfx").transform;
                ground.gameObject.layer = root.layer;
                ground.SetParent(root.transform, false);
            }
            ground.localPosition = Vector3.up * .035f;
            ground.localRotation = Quaternion.identity;

            GameObject spray = Effect("GelSpray", new Color(.45f, .78f, .16f, .72f), 8, .10f, .8f, .3f, .07f, false);
            GameObject flecks = Effect("MoveFlecks", new Color(.51f, .75f, .27f, .30f), 5, .08f, .16f, .25f, .25f, true);
            VfxAuthoring.Bind(root, "AttackImpact", spray, atTarget: true);
            VfxAuthoring.Bind(root, "MoveStep", flecks, ground);
            GameObject split = SplitSplash();
            BoxCollider body = root.GetComponent<BoxCollider>();
            Require(body != null, "Slime split spray needs the body collider to locate the half-size children's heads.");
            float splashHeight = (body.center.y + body.size.y * .5f) * .5f + .18f;
            for (int side = -1; side <= 1; side += 2)
            {
                string name = side < 0 ? "SlimeSplitLeftVfx" : "SlimeSplitRightVfx";
                Transform anchor = root.transform.Find(name);
                if (anchor == null)
                {
                    anchor = new GameObject(name).transform;
                    anchor.gameObject.layer = root.layer;
                    anchor.SetParent(root.transform, false);
                }
                anchor.localPosition = new Vector3(.8f * side, splashHeight, 0);
                anchor.localRotation = Quaternion.identity;
                VfxAuthoring.Bind(root, "SplitStart", split, anchor);
            }

            EnemyVfx vfx = root.GetComponent<EnemyVfx>();
            Require(vfx.Bindings.Any(binding => binding.Signal == "AttackImpact" && binding.Effect == spray && binding.AtTarget),
                "Slime gel spray must use the validated attack target.");
            Require(vfx.Bindings.Any(binding => binding.Signal == "MoveStep" && binding.Effect == flecks && binding.Anchor == ground),
                "Slime movement flecks must use ground contact.");
            EnemyVfx.Binding[] splashes = vfx.Bindings.Where(binding => binding.Signal == "SplitStart").ToArray();
            Require(splashes.Length == 2 && splashes.All(binding => binding.Effect == split && binding.Anchor != null
                && binding.Anchor.localPosition.y == splashHeight && !binding.AtTarget && !binding.Follow)
                && splashes[0].Anchor.localPosition.x == -.8f && splashes[1].Anchor.localPosition.x == .8f,
                "Slime split requires two detached upward sprays over the newly created children's heads.");
        }

        private static GameObject SplitSplash()
        {
            GameObject prefab = VfxAuthoring.Burst("Slime/SplitSplash", new Color(.48f, .77f, .22f, .78f), 18, .18f, 3.4f, .8f);
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ParticleSystem particles = contents.GetComponent<ParticleSystem>();
                var main = particles.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(2.6f, 3.4f);
                main.gravityModifier = .65f;
                main.startSize3D = true;
                main.startSizeX = main.startSizeZ = new ParticleSystem.MinMaxCurve(.20f, .30f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(.22f, .32f);
                main.startRotationX = main.startRotationZ = 0;
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.rotation = new Vector3(-90, 0, 0);
                shape.angle = 24;
                shape.radius = .26f;
                particles.GetComponent<ParticleSystemRenderer>().mesh = DropletMesh();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, path);
                Require(saved != null && !saved.GetComponent<ParticleSystem>().main.loop
                    && saved.GetComponent<LowpolyVfxLifetime>() != null,
                    "Slime split spray must end and clean itself up.");
                return saved;
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static Mesh DropletMesh()
        {
            string path = VfxAuthoring.Directory + "Slime/RoundedDrop80.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            VfxAuthoring.Ensure(path);
            float t = (1 + Mathf.Sqrt(5)) * .5f;
            Vector3[] points = {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            int[] faces = {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            var vertices = new List<Vector3>(240);
            void Face(Vector3 a, Vector3 b, Vector3 c) => vertices.AddRange(new[] { a, b, c });
            for (int face = 0; face < faces.Length; face += 3)
            {
                Vector3 a = points[faces[face]].normalized * .5f;
                Vector3 b = points[faces[face + 1]].normalized * .5f;
                Vector3 c = points[faces[face + 2]].normalized * .5f;
                Vector3 ab = (a + b).normalized * .5f, bc = (b + c).normalized * .5f, ca = (c + a).normalized * .5f;
                Face(a, ab, ca); Face(b, bc, ab); Face(c, ca, bc); Face(ab, bc, ca);
            }
            mesh = new Mesh { name = "Rounded lowpoly liquid globule (80 triangles)" };
            mesh.vertices = vertices.ToArray();
            mesh.triangles = Enumerable.Range(0, vertices.Count).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Require(mesh.triangles.Length == 240 && mesh.vertexCount == 240
                && mesh.vertices.All(vertex => Mathf.Abs(vertex.magnitude - .5f) < .0001f),
                "Slime liquid globules require 80 flat faces on a round sphere, without pointed tips.");
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static GameObject Effect(string name, Color color, int count, float size, float speed, float lifetime, float radius, bool ground)
        {
            GameObject prefab = VfxAuthoring.Burst("Slime/" + name, color, count, size, speed, lifetime);
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ParticleSystem particles = contents.GetComponent<ParticleSystem>();
                var main = particles.main;
                main.gravityModifier = ground ? 0 : .2f;
                main.startSize3D = true;
                main.startSizeX = main.startSizeZ = new ParticleSystem.MinMaxCurve(size * .5f, size);
                main.startSizeY = new ParticleSystem.MinMaxCurve(size * .2f, size * .5f);
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.rotation = new Vector3(-90, 0, 0);
                shape.angle = ground ? 85 : 65;
                shape.radius = radius;
                var renderer = particles.GetComponent<ParticleSystemRenderer>();
                Require(renderer.renderMode == ParticleSystemRenderMode.Mesh
                    && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(renderer.mesh))
                    && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(renderer.sharedMaterial)),
                    "Slime VFX must use persistent angular mesh and material assets.");
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, path);
                Require(saved != null && !saved.GetComponent<ParticleSystem>().main.loop
                    && saved.GetComponent<LowpolyVfxLifetime>() != null,
                    "Slime VFX must be finite and clean themselves up.");
                return saved;
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
