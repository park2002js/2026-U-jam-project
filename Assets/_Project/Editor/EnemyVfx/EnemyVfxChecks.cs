using System;
using System.Collections.Generic;
using System.Linq;
using UJam.Runtime.Enemy;
using UJam.Runtime.Enemy.Projectiles;
using UJam.Runtime.Enemy.Vfx;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    public static class EnemyVfxChecks
    {
        public static void ValidateAssets()
        {
            foreach (string name in EnemyVfxBuild.Names)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/" + name + ".prefab");
                Require(root != null, name + " prefab is missing.");
                ValidateHierarchy(root);
                Require(root.GetComponent<EnemyBase>() != null && root.GetComponent<EnemyStatus>() != null
                    && root.GetComponent<Collider>() != null && root.GetComponent<Rigidbody>() != null, name + " lost its gameplay components.");
                Animator animator = root.GetComponentInChildren<Animator>(true);
                Require(animator != null && animator.GetComponent<EnemyAnimation>() != null
                    && animator.runtimeAnimatorController is AnimatorController, name + " animation receiver/controller is missing.");
                ValidateEvents(name, (AnimatorController)animator.runtimeAnimatorController);
                EnemyVfx vfx = root.GetComponent<EnemyVfx>();
                Require(vfx != null, name + " VFX dispatcher is missing.");
                foreach (EnemyVfx.Binding binding in vfx.Bindings) ValidateEffect(binding.Effect);
                foreach (TrailRenderer trail in vfx.Trails)
                    Require(trail != null && trail.transform.IsChildOf(root.transform) && !trail.emitting, name + " trail must start disabled on its animated model.");
                if (name == "Golem" || name == "Big_Golem")
                    Require(vfx.Eyes.Length == 2 && vfx.Eyes.All(eye => eye != null && eye.transform.IsChildOf(root.transform))
                        && vfx.Eyes.Distinct().Count() == 2 && vfx.Trails.Length > 0, name + " requires both eyes and its hand trail.");
                if (name == "Big_Golem" || name == "Witch")
                {
                    CrystalGlow[] glows = root.GetComponentsInChildren<CrystalGlow>(true);
                    Transform model = root.transform.Find("Model");
                    Require(glows.Length == (name == "Big_Golem" ? 27 : 1), name + " crystal glow count is incorrect.");
                    foreach (CrystalGlow glow in glows)
                        Require(glow.Source != null && glow.Source.transform.IsChildOf(model) && glow.Surface != null
                            && !glow.Surface.transform.IsChildOf(model) && glow.Bone != null,
                            name + " glow must follow a physical crystal without changing the source model.");
                    if (name == "Witch")
                    {
                        Require(glows[0].Source.name == "Witch_Staff_Crystal_20_Faces", "Witch glow must belong to its black staff crystal.");
                        WitchVfxSetup.ValidateMuzzle(root);
                        WitchVfxSetup.ValidateSummonField(root);
                    }
                }
                if (root.TryGetComponent(out RangedEnemy ranged))
                {
                    Projectile projectile = Reference(ranged, "_projectilePrefab") as Projectile;
                    Require(projectile != null, name + " projectile behaviour is missing.");
                    ValidateEffect(projectile.gameObject);
                    ValidateEffect(Reference(projectile, "_impactEffect") as GameObject);
                }
                if (root.TryGetComponent(out SniperEnemy sniper))
                {
                    GameObject shot = Reference(sniper, "_shotEffect") as GameObject;
                    ValidateEffect(shot);
                    SniperBeam beam = shot.GetComponent<SniperBeam>();
                    Require(beam != null, "Sniper shot lost its endpoint beam behaviour.");
                    ValidateEffect(Reference(beam, "_targetImpact") as GameObject);
                    ValidateEffect(Reference(beam, "_muzzleFlash") as GameObject);
                }
                if (root.TryGetComponent(out RollbugEnemy rollbug))
                {
                    GameObject detonation = Reference(rollbug, "_detonationEffect") as GameObject;
                    Require(new SerializedObject(rollbug).FindProperty("_deathEffect") == null,
                        "Rollbug must not retain a second death-explosion field.");
                    ValidateEffect(detonation);
                }
                if (name == "Witch")
                {
                    WitchDeathVfx witch = root.GetComponent<WitchDeathVfx>();
                    Require(witch != null && witch.BodyRendererCount == 9 && witch.StaffRendererCount == 3, "Witch requires the 9 + 3 dissolve groups.");
                    var serialized = new SerializedObject(witch);
                    Renderer[] groups = new[] { "_bodyRenderers", "_staffRenderers" }.SelectMany(field =>
                    {
                        SerializedProperty array = serialized.FindProperty(field);
                        return Enumerable.Range(0, array.arraySize).Select(index => array.GetArrayElementAtIndex(index).objectReferenceValue as Renderer);
                    }).ToArray();
                    Require(groups.All(renderer => renderer != null) && groups.Distinct().Count() == 12, "Witch dissolve groups contain missing or shared renderers.");
                }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Art/EnemyVfx" }))
                ValidateEffect(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
            SniperEyebeastVfxSetup.ValidateGeometry();
            Debug.Log("[EnemyVfxChecks] Eight enemy VFX assets, events, gameplay components and effect references passed.");
        }

        private static void ValidateEvents(string name, AnimatorController controller)
        {
            var hits = new Dictionary<string, float> { { "Golem", .3f }, { "Big_Golem", .3f }, { "Eyebeast", .25f },
                { "Sniper_Eyebeast", .5f }, { "Witch", .55f }, { "Skeleton", .2f }, { "Slime", .45f } };
            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            Require(name == "Rollbug" || states.Count(item => item.state.name == "Attack") == 1, name + " must preserve one Attack state.");
            foreach (ChildAnimatorState item in states)
            {
                string state = item.state.name;
                AnimationClip clip = item.state.motion as AnimationClip;
                Require(clip != null, name + " " + state + " clip is missing.");
                var expected = new List<KeyValuePair<string, float>>();
                void Add(string signal, float time) => expected.Add(new KeyValuePair<string, float>(signal, time));
                bool trail = name == "Golem" || name == "Big_Golem" || name == "Skeleton";
                bool golem = name == "Golem" || name == "Big_Golem";
                if (state == "Attack")
                {
                    Require(hits.ContainsKey(name), "Rollbug must retain its immediate attack without a damage animation event.");
                    Add("AttackImpact", hits[name]);
                    if (name == "Sniper_Eyebeast") Add("AttackCharge", .05f);
                    if (trail) { Add("AttackTrailStart", .1f); Add("AttackTrailEnd", name == "Skeleton" ? .3f : .4f); }
                }
                if (state == "Move" && (trail || name == "Slime")) { Add("MoveStep", clip.length * .2f); Add("MoveStep", clip.length * .7f); }
                if (state == "Spawn") Add("SpawnLand", 1.15f);
                if (state == "Summon") { Add("SummonStart", 0); Add("SummonImpact", .35f); }
                if (state == "Death")
                {
                    if (golem) { Add("EyesOff", 5f / 60f); Add("DeathImpact", .65f); }
                    if (name == "Eyebeast" || name == "Sniper_Eyebeast") Add("DeathImpact", 1.256410f);
                    if (name == "Skeleton") Add("DeathImpact", 28f / 60f);
                }
                if (new[] { "Attack", "Death", "Spawn", "Summon" }.Contains(state)) Add(state + "Complete", clip.length);
                AnimationEvent[] actual = AnimationUtility.GetAnimationEvents(clip);
                Require(actual.Length == expected.Count && expected.All(entry => actual.Count(evt => evt.functionName == "OnAnimationSignal"
                    && evt.stringParameter == entry.Key && Mathf.Abs(evt.time - entry.Value) < .0001f) == 1), name + " " + state + " animation event timing/count changed.");
            }
        }

        private static void ValidateHierarchy(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, root.name + " contains a missing script.");
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                foreach (Material material in renderer.sharedMaterials)
                    Require(material != null && AssetDatabase.Contains(material) && material.shader != null && !ShaderUtil.ShaderHasError(material.shader)
                        && (material.shader.isSupported || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null), root.name + " has a missing, unsaved or unsupported material/shader.");
        }

        private static void ValidateEffect(GameObject effect)
        {
            Require(effect != null && AssetDatabase.Contains(effect), "VFX reference must point to a saved prefab.");
            ValidateHierarchy(effect);
            LowpolyVfxLifetime lifetime = effect.GetComponent<LowpolyVfxLifetime>();
            ParticleSystem particles = effect.GetComponent<ParticleSystem>();
            Require((lifetime != null && lifetime.Lifetime > 0 && !float.IsInfinity(lifetime.Lifetime))
                || (particles != null && !particles.main.loop && particles.main.stopAction == ParticleSystemStopAction.Destroy
                    && particles.main.startLifetime.constantMax > 0 && !float.IsInfinity(particles.main.startLifetime.constantMax))
                || effect.GetComponent<Projectile>() != null || effect.GetComponent<SniperBeam>() != null, effect.name + " has no finite cleanup behaviour.");
        }

        private static Object Reference(Component component, string field)
        {
            SerializedProperty property = new SerializedObject(component).FindProperty(field);
            Require(property != null && property.objectReferenceValue != null, component.name + " is missing " + field + ".");
            return property.objectReferenceValue;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
