using System;
using System.Linq;
using UJam.Runtime.Enemy;
using UJam.Runtime.Enemy.Vfx;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    public static class SniperEyebeastVfxSetup
    {
        private const string Id = "Sniper_Eyebeast/";
        private const string Art = "Assets/_Project/Art/EnemyVfx/Sniper_Eyebeast/";
        private static readonly Color Purple = new Color(.68f, .12f, 1f, 1f);

        public static void Configure(GameObject root)
        {
            ValidateGeometry();
            SniperEnemy enemy = root.GetComponent<SniperEnemy>();
            if (enemy == null) throw new InvalidOperationException("Sniper VFX requires SniperEnemy.");
            Transform body = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(renderer => renderer.bones).FirstOrDefault(bone => bone != null && bone.name == "Body")
                ?? VfxAuthoring.Find(root, "Body") ?? root.transform;
            Transform firePoint = VfxAuthoring.Find(root, "FirePoint");
            if (firePoint == null)
            {
                firePoint = new GameObject("FirePoint").transform;
                firePoint.SetParent(body, false);
                BoxCollider bounds = root.GetComponent<BoxCollider>();
                Vector3 local = bounds != null
                    ? bounds.center + new Vector3(0f, bounds.size.y * .075f, bounds.size.z * .5f)
                    : new Vector3(0f, 1.5f, 1f);
                firePoint.position = root.transform.TransformPoint(local);
                firePoint.rotation = root.transform.rotation;
                firePoint.gameObject.layer = root.layer;
            }
            else if (firePoint.parent != body) firePoint.SetParent(body, true);

            GameObject charge = Charge();
            GameObject muzzle = VfxAuthoring.Burst(Id + "MuzzleFlash", new Color(.95f, .65f, 1f), 18, .24f, 2f, .18f);
            GameObject impact = VfxAuthoring.Burst(Id + "TargetImpact", new Color(.8f, .3f, 1f), 24, .22f, 3.3f, .32f);
            GameObject shot = Beam(muzzle, impact);
            SetReference(enemy, "_firePoint", firePoint);
            SetReference(enemy, "_shotEffect", shot);
            EnemyVfx vfx = root.GetComponent<EnemyVfx>();
            if (vfx != null)
                vfx.Bindings = Array.FindAll(vfx.Bindings, binding => binding.Effect == null
                    || !AssetDatabase.GetAssetPath(binding.Effect).StartsWith(Art, StringComparison.Ordinal));
            VfxAuthoring.Bind(root, "AttackCharge", charge, firePoint, follow: true);
            VfxAuthoring.Bind(root, "DeathImpact", VfxAuthoring.Burst(Id + "DeathSparks", Purple, 20, .1f, 1.9f, .4f), body);
        }

        private static GameObject Charge()
        {
            GameObject prefab = VfxAuthoring.Burst(Id + "Charge", Purple, 24, .1f, 1.6f, .36f);
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ParticleSystem sparks = contents.GetComponentInChildren<ParticleSystem>();
                var main = sparks.main;
                main.startSpeed = -1.65f;
                main.gravityModifier = 0f;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var shape = sparks.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = .6f;
                shape.radiusThickness = .08f;
                var size = sparks.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, .35f), new Keyframe(.7f, 1f), new Keyframe(1f, 0f)));
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static GameObject Beam(GameObject muzzle, GameObject impact)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Art + "SniperCurrent.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader)
                || (!shader.isSupported && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null))
                throw new InvalidOperationException("The procedural Sniper current shader must compile for the active URP renderer.");
            Material trunk = CurrentMaterial("LightningTrunk", shader, new Color(.95f, .8f, 1f), 3.4f);
            Material halo = CurrentMaterial("LightningTrunkGlow", shader, new Color(.56f, .16f, 1f), .3f);
            Material current = CurrentMaterial("LightningSideCurrent", shader, new Color(.76f, .42f, 1f), 2.4f);
            Material branch = CurrentMaterial("LightningOffshoot", shader, new Color(.68f, .27f, 1f), 1.9f);
            var root = new GameObject("Sniper_InstantLightning");
            try
            {
                LineRenderer shellLine = Line(root, "MainLightningTrunk", trunk, .30f);
                LineRenderer haloLine = Line(root, "MainTrunkGlow", halo, .76f);
                LineRenderer[] currents = Enumerable.Range(0, 3)
                    .Select(i => Line(root, "WrappingCurrent" + (i + 1), current, .09375f - i * .01f)).ToArray();
                LineRenderer[] offshoots = Enumerable.Range(0, 4)
                    .Select(i => Line(root, "ShortOffshoot" + (i + 1), branch, .05625f - i * .00375f, 5, true)).ToArray();
                SniperBeam beam = root.AddComponent<SniperBeam>();
                SetReference(beam, "_shell", shellLine);
                SetReference(beam, "_halo", haloLine);
                SetReferences(beam, "_currents", currents);
                SetReferences(beam, "_offshoots", offshoots);
                SetReference(beam, "_targetEnergy", TargetEnergy(root, shader));
                SetReference(beam, "_muzzleFlash", muzzle);
                SetReference(beam, "_targetImpact", impact);
                if (root.GetComponentsInChildren<LineRenderer>(true).Length != 9
                    || currents.Any(line => line.widthMultiplier >= shellLine.widthMultiplier * .5f))
                    throw new InvalidOperationException("Sniper lightning requires one dominant trunk and separately rendered thin currents/offshoots.");
                return PrefabUtility.SaveAsPrefabAsset(root, Art + "InstantLightning.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Material CurrentMaterial(string name, Shader shader, Color color, float intensity, float shape = 0f)
        {
            string path = Art + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Intensity", intensity);
            material.SetFloat("_Shape", shape);
            material.SetFloat("_Pulse", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject TargetEnergy(GameObject parent, Shader shader)
        {
            var energy = new GameObject("TargetEnergyConcentration");
            energy.transform.SetParent(parent.transform, false);
            foreach (bool inner in new[] { false, true })
            {
                GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                glow.name = inner ? "ConcentratedWhiteGlow" : "PurpleEnergyGlow";
                glow.transform.SetParent(energy.transform, false);
                glow.transform.localScale = Vector3.one * (inner ? .48f : 1.25f);
                Object.DestroyImmediate(glow.GetComponent<Collider>());
                Renderer renderer = glow.GetComponent<Renderer>();
                renderer.sharedMaterial = CurrentMaterial(inner ? "TargetWhiteGlow" : "TargetPurpleGlow", shader,
                    inner ? new Color(.96f, .8f, 1f) : new Color(.62f, .15f, 1f), inner ? 2.3f : .95f, 1f);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            var core = new GameObject("TargetPolyCore");
            core.transform.SetParent(energy.transform, false);
            core.transform.localScale = Vector3.one * .24f;
            Mesh mesh = VfxAuthoring.ShardMesh();
            core.AddComponent<MeshFilter>().sharedMesh = mesh;
            core.transform.localPosition = -mesh.bounds.center * .24f;
            var surface = core.AddComponent<MeshRenderer>();
            surface.sharedMaterial = CurrentMaterial("TargetPolyCore", shader, new Color(.96f, .74f, 1f), 1.6f, 2f);
            surface.shadowCastingMode = ShadowCastingMode.Off;
            surface.receiveShadows = false;
            energy.SetActive(false);
            return energy;
        }

        private static LineRenderer Line(GameObject parent, string name, Material material, float width, int pointCount = 25, bool branch = false)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = pointCount;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, branch ? .85f : .55f), new Keyframe(.12f, 1f),
                new Keyframe(.8f, branch ? .5f : .9f), new Keyframe(1f, branch ? 0f : .35f));
            line.widthMultiplier = width;
            if (line.widthMultiplier <= 0f) throw new InvalidOperationException("Sniper current lost its authored width.");
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private static void SetReferences(Object target, string field, LineRenderer[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null || !property.isArray) throw new InvalidOperationException(target.name + " has no array " + field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException(target.name + " has no field " + field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ValidateGeometry()
        {
            if (SniperBeam.WidthAt(0f) != 1f || SniperBeam.WidthAt(.08f) != 1f ||
                Mathf.Abs(SniperBeam.WidthAt(.13f) - .5f) > .0001f || SniperBeam.WidthAt(.18f) != 0f)
                throw new InvalidOperationException("Sniper lightning must hold full intensity before its single flash fades.");
            if (SniperBeam.EnergyAt(0f) != 1f || SniperBeam.EnergyAt(.1f) != 1f || SniperBeam.EnergyAt(.42f) != 0f)
                throw new InvalidOperationException("Sniper target energy must be immediate and fade within effect cleanup.");
            var points = new Vector3[25];
            var current = new Vector3[25];
            var other = new Vector3[25];
            var branch = new Vector3[5];
            Vector3 start = new Vector3(2f, 1.5f, -3f);
            foreach (Vector3 target in new[] { new Vector3(-3f, .2f, 5f), start + Vector3.up * 5f,
                start + new Vector3(.001f, 5f, .002f), start + Vector3.down * 5f, start })
            {
                foreach (float phase in new[] { 0f, 2f })
                {
                    SniperBeam.BuildPoints(start, target, points, phase);
                    ValidateCurrent(start, target, points);
                    for (int j = 0; j < 3; j++)
                    {
                        SniperBeam.BuildCurrent(start, target, current, j, phase);
                        ValidateCurrent(start, target, current);
                        for (int b = 0; b < 4; b++)
                        {
                            SniperBeam.BuildOffshoot(current, b, branch, phase);
                            if (Array.IndexOf(current, branch[0]) < 0 || Vector3.Distance(branch[0], branch[branch.Length - 1]) > .75f)
                                throw new InvalidOperationException("Sniper offshoot must attach to its own current and remain short.");
                            foreach (Vector3 point in branch) ValidateFinite(point);
                        }
                    }
                }
                SniperBeam.BuildCurrent(start, target, current, 0, 0f);
                SniperBeam.BuildCurrent(start, target, other, 1, 0f);
                if (start != target && Vector3.Distance(current[12], other[12]) < .02f)
                    throw new InvalidOperationException("Sniper side currents collapsed onto the same ribbon.");
                SniperBeam.BuildCurrent(start, target, other, 0, 2f);
                if (start != target && Vector3.Distance(current[12], other[12]) < .01f)
                    throw new InvalidOperationException("Sniper side-current geometry must flicker independently over time.");
            }
        }

        private static void ValidateCurrent(Vector3 start, Vector3 target, Vector3[] points)
        {
            if (points[0] != start || points[points.Length - 1] != target)
                throw new InvalidOperationException("Sniper current did not preserve the exact source and target.");
            for (int i = 0; i < points.Length; i++)
            {
                ValidateFinite(points[i]);
                float t = i / (points.Length - 1f);
                float projected = Vector3.Dot(points[i] - start, target - start);
                if (Mathf.Abs(projected - (target - start).sqrMagnitude * t) > .001f)
                    throw new InvalidOperationException("Sniper current doubled back along its target axis.");
                if (Vector3.Distance(points[i], Vector3.Lerp(start, target, t)) > .3f)
                    throw new InvalidOperationException("Sniper current spread beyond the main trunk's local envelope.");
            }
        }

        private static void ValidateFinite(Vector3 point)
        {
            if (float.IsNaN(point.sqrMagnitude) || float.IsInfinity(point.sqrMagnitude))
                throw new InvalidOperationException("Sniper current produced an invalid vertex.");
        }
    }
}
