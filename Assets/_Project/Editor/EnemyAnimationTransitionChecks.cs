using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UJam.Runtime.Enemy;
using UJam.Runtime.Enemy.Projectiles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    // Uses the existing isolated runtime-check scene and its real production prefabs.
    public static class EnemyAnimationTransitionChecks
    {
        private sealed class Pose
        {
            public Transform[] bones;
            public Vector3[] positions;
            public Quaternion[] rotations;
            private readonly Transform root;
            private readonly SkinnedMeshRenderer[] skins;
            private readonly float[][] weights;
            public Pose(EnemyBase enemy)
            {
                root = enemy.transform;
                skins = enemy.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                bones = skins
                    .SelectMany(renderer => renderer.bones).Where(bone => bone != null).Distinct().ToArray();
                positions = bones.Select(bone => root.InverseTransformPoint(bone.position)).ToArray();
                rotations = bones.Select(bone => Quaternion.Inverse(root.rotation) * bone.rotation).ToArray();
                weights = skins.Select(skin => Enumerable.Range(0, skin.sharedMesh.blendShapeCount)
                    .Select(skin.GetBlendShapeWeight).ToArray()).ToArray();
            }
            public bool Changed()
            {
                for (int i = 0; i < bones.Length; i++)
                    if (bones[i] != null && (Vector3.Distance(root.InverseTransformPoint(bones[i].position), positions[i]) > .00005f ||
                        Quaternion.Angle(Quaternion.Inverse(root.rotation) * bones[i].rotation, rotations[i]) > .01f)) return true;
                for (int skin = 0; skin < skins.Length; skin++)
                    for (int shape = 0; shape < weights[skin].Length; shape++)
                        if (Mathf.Abs(skins[skin].GetBlendShapeWeight(shape) - weights[skin][shape]) > .001f) return true;
                return false;
            }
        }

        private sealed class Case
        {
            public string name;
            public EnemyBase enemy, dying;
            public MeleeEnemy primary, fallback;
            public Animator animator;
            public Vector3 origin, committed;
            public Pose pose;
            public int phase, deathIndex;
            public float since, primaryHp;
            public bool poseChanged, projectileSeen, complete, movedTarget, destroyedTarget;
            public EnemyStateType[] deathSources;
        }

        private static readonly string[] Names = { "Golem", "Big_Golem", "Eyebeast", "Sniper_Eyebeast", "Witch", "Skeleton", "Slime", "Rollbug" };
        private static readonly List<Case> cases = new List<Case>();
        public static readonly List<string> Results = new List<string>();
        public static bool IsComplete => cases.Count == 8 && cases.All(test => test.complete);

        public static void Begin()
        {
            cases.Clear();
            Results.Clear();
            for (int i = 0; i < Names.Length; i++)
            {
                var test = new Case { name = Names[i], origin = new Vector3(Names[i] == "Rollbug" ? 0 : 300 + i * 20, 0, 300), since = Time.time };
                test.enemy = Spawn(test.name, test.origin);
                test.enemy.PrepareForTesting(null);
                test.animator = test.enemy.GetComponentInChildren<Animator>();
                test.primary = Target(test.origin + Vector3.forward);
                test.fallback = Target(test.origin + Vector3.forward * 30);
                var sources = new List<EnemyStateType> { EnemyStateType.Idle, EnemyStateType.Move };
                if (!test.enemy.ImmediateAttack) sources.Add(EnemyStateType.Attack);
                if (test.enemy.Animation.HasState("Spawn")) sources.Add(EnemyStateType.Spawn);
                if (test.enemy.Animation.HasState("Summon")) sources.Add(EnemyStateType.Summon);
                test.deathSources = sources.ToArray();
                cases.Add(test);
            }
        }

        private static EnemyBase Spawn(string name, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/" + name + ".prefab");
            EnemyBase enemy = Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<EnemyBase>();
            // The existing runtime checks exercise real splitting independently of these transition fixtures.
            if (enemy is SlimeEnemy) Set(enemy, "_canSplit", false);
            return enemy;
        }

        private static MeleeEnemy Target(Vector3 position)
        {
            var target = (MeleeEnemy)Spawn("Golem", position);
            target.Status.ConfigureSpawnStats(100000, 0, 0);
            target.PrepareForTesting(null);
            return target;
        }

        public static void Tick()
        {
            foreach (Case test in cases)
            {
                if (test.complete) continue;
                float elapsed = Time.time - test.since;
                switch (test.phase)
                {
                    case 0:
                        if (elapsed < .2f) break;
                        Require(AnimationIs(test.animator, "Idle"), test.name + " did not enter Idle.");
                        test.pose = new Pose(test.enemy);
                        Next(test, 1);
                        break;
                    case 1:
                        if (elapsed < .25f) break;
                        Require(test.name == "Rollbug" || test.pose.Changed(), test.name + " Idle has no changing bone pose.");
                        test.enemy.PrepareForTesting(test.fallback.gameObject);
                        test.enemy.FSM.SetState(EnemyStateType.Move);
                        AssertCommand(test, "Move", test.enemy.ImmediateAttack);
                        Next(test, 2);
                        break;
                    case 2:
                        if (elapsed < .18f) break;
                        test.pose = new Pose(test.enemy);
                        Next(test, 3);
                        break;
                    case 3:
                        if (elapsed < .22f) break;
                        Require(test.pose.Changed(), test.name + " Move has no changing bone pose.");
                        test.fallback.gameObject.SetActive(false);
                        test.enemy.FSM.SetState(EnemyStateType.Idle);
                        AssertCommand(test, "Idle", test.enemy.ImmediateAttack);
                        Next(test, 4);
                        break;
                    case 4:
                        if (elapsed < .18f) break;
                        test.fallback.gameObject.SetActive(true);
                        test.enemy.PrepareForTesting(test.fallback.gameObject);
                        if (test.enemy.Animation.HasState("Spawn") || test.enemy.Animation.HasState("Summon"))
                        {
                            string special = test.enemy.Animation.HasState("Spawn") ? "Spawn" : "Summon";
                            test.enemy.FSM.SetState(special == "Spawn" ? EnemyStateType.Spawn : EnemyStateType.Summon);
                            AssertCommand(test, special, false);
                            test.pose = null;
                            Next(test, 5);
                        }
                        else StartAttack(test, false);
                        break;
                    case 5:
                        if (elapsed < .14f) break;
                        if (test.pose == null) { test.pose = new Pose(test.enemy); break; }
                        if (elapsed < .28f) break;
                        Require(test.pose.Changed(), test.name + " special animation has no changing bone pose.");
                        test.enemy.FSM.SetState(EnemyStateType.Move);
                        AssertCommand(test, "Move", false);
                        Results.Add(test.name + " special animation is interrupted by Move before its clip finishes");
                        StartAttack(test, false);
                        break;
                    case 6:
                        CheckAttack(test, elapsed);
                        break;
                    case 7:
                        CheckDeath(test, elapsed);
                        break;
                }
            }
        }

        private static void StartAttack(Case test, bool movedTarget)
        {
            test.movedTarget = movedTarget;
            test.enemy.transform.position = test.origin;
            if (test.primary == null) test.primary = Target(test.origin + Vector3.forward);
            test.primary.gameObject.SetActive(true);
            test.primary.transform.position = test.origin + Vector3.forward;
            test.enemy.PrepareForTesting(test.fallback.gameObject);
            test.enemy.FSM.Targets.Add(test.primary.gameObject);
            test.primaryHp = test.primary.Status.HP;
            int frame = Time.frameCount;
            GameObject effectPrefab = test.enemy is RollbugEnemy ? (GameObject)Get(test.enemy, "_detonationEffect") : null;
            int effectCount = effectPrefab != null ? EffectCount(effectPrefab.name) : 0;
            test.enemy.FSM.SetState(EnemyStateType.Attack);
            if (test.enemy.ImmediateAttack)
            {
                Require(Time.frameCount == frame && test.enemy.FSM.state == EnemyStateType.Dead && test.enemy.Status.HP == 0 &&
                        Mathf.Approximately(test.primaryHp - test.primary.Status.HP, 200),
                    "Rollbug did not apply attack damage and enter Dead in the command frame.");
                Require(effectPrefab != null && EffectCount(effectPrefab.name) > effectCount,
                    "Rollbug did not start an independent explosion VFX in the attack frame.");
                Results.Add("Rollbug Attack starts independent explosion VFX, applies one hit and enters Dead in one frame");
                StartDeath(test);
                return;
            }
            Require(test.enemy.IsAttackPlaying, test.name + " did not commit an attack.");
            test.committed = test.enemy.AttackTargetPosition;
            test.pose = null;
            test.poseChanged = test.projectileSeen = false;
            test.destroyedTarget = !movedTarget && test.name == "Eyebeast";
            if (movedTarget) test.primary.transform.position += Vector3.right * 20;
            else if (test.destroyedTarget) Object.Destroy(test.primary.gameObject);
            else test.primary.gameObject.SetActive(false);
            test.enemy.ReTargeting();
            Require(test.enemy.FSM.SetState(EnemyStateType.Move) && test.enemy.FSM.state == EnemyStateType.Attack &&
                    test.enemy.IsAttackPlaying && test.enemy.FSM.Targets.Count == 2,
                test.name + " target loss or Move request interrupted its committed Attack.");
            Next(test, 6);
        }

        private static void CheckAttack(Case test, float elapsed)
        {
            foreach (Projectile projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                if (!ReferenceEquals(Get(projectile, "_owner"), test.enemy)) continue;
                Vector3 destination = (Vector3)Get(projectile, "_destination");
                Require(Vector2.Distance(new Vector2(destination.x, destination.z),
                        new Vector2(test.committed.x, test.committed.z)) < .001f,
                    test.name + " aimed its projectile at a new target position during a committed attack.");
                test.projectileSeen = true;
            }
            if (test.primary != null)
                Require(test.primary.Status.HP == test.primaryHp, test.name + " damaged an absent or moved target.");
            if (test.enemy.IsAttackPlaying)
            {
                Require(test.enemy.FSM.state == EnemyStateType.Attack && test.enemy.FSM.Targets.Count == 2,
                    test.name + " revalidated or left Attack before its motion finished.");
                if (elapsed >= .16f && test.pose == null) test.pose = new Pose(test.enemy);
                else if (test.pose != null) test.poseChanged |= test.pose.Changed();
                Require(elapsed < 2f, test.name + " committed Attack never completed.");
                return;
            }
            Require(test.enemy.FSM.state == EnemyStateType.Move, test.name + " did not execute the queued Move after attack completion.");
            if (test.pose != null) test.poseChanged |= test.pose.Changed();
            Require(test.poseChanged, test.name + " Attack had no changing bone pose after its entry blend.");
            if (test.enemy is RangedEnemy) Require(test.projectileSeen, test.name + " failed to fire at the committed position after target loss.");
            if (!test.movedTarget)
            {
                Require(test.enemy.FSM.Targets.Count == 1 && test.enemy.FSM.Targets[0] == test.fallback.gameObject,
                    test.name + " did not discard its missing target after attack completion.");
                if (test.destroyedTarget) Require(test.primary == null, "Eyebeast's target was not actually destroyed during its committed attack.");
                Results.Add(test.name + " committed Attack finishes after target loss, fires once and then resumes the fallback target");
                StartAttack(test, true);
            }
            else
            {
                Results.Add(test.name + " moved-target attack keeps its original aim point and deals no damage");
                StartDeath(test);
            }
        }

        private static void StartDeath(Case test)
        {
            if (test.deathIndex >= test.deathSources.Length)
            {
                if (test.enemy != null) Object.Destroy(test.enemy.gameObject);
                Object.Destroy(test.primary.gameObject);
                Object.Destroy(test.fallback.gameObject);
                test.complete = true;
                Results.Add(test.name + " Idle/Move blends and all supported immediate Death transitions passed");
                return;
            }
            test.dying = Spawn(test.name, test.origin);
            test.dying.PrepareForTesting(null);
            EnemyStateType source = test.deathSources[test.deathIndex];
            if (source == EnemyStateType.Move) test.dying.PrepareForTesting(test.fallback.gameObject);
            if (source == EnemyStateType.Attack)
            {
                test.primary.transform.position = test.origin + Vector3.forward;
                test.dying.PrepareForTesting(test.primary.gameObject);
            }
            if (source != EnemyStateType.Idle) test.dying.FSM.SetState(source);
            test.pose = null;
            Next(test, 7);
        }

        private static void CheckDeath(Case test, float elapsed)
        {
            if (test.pose == null)
            {
                if (elapsed < .15f) return;
                test.dying.FSM.SetState(EnemyStateType.Dead);
                Require(test.dying.FSM.state == EnemyStateType.Dead && !test.dying.IsAttackPlaying,
                    test.name + " Death did not immediately cancel its prior action.");
                if (test.dying.ImmediateAttack)
                {
                    test.deathIndex++;
                    StartDeath(test);
                    return;
                }
                Animator animator = test.dying.GetComponentInChildren<Animator>();
                animator.Update(0);
                Require(AnimationIs(animator, "Death") && !animator.IsInTransition(0),
                    test.name + " Death waited for exit time or a previous blend.");
                test.pose = new Pose(test.dying);
                test.since = Time.time;
                return;
            }
            if (elapsed < .3f) return;
            Require(test.dying != null && test.pose.Changed(), test.name + " Death has no changing bone pose.");
            Results.Add(test.name + " " + test.deathSources[test.deathIndex] + " is preempted immediately by animated Death");
            Object.Destroy(test.dying.gameObject);
            test.deathIndex++;
            StartDeath(test);
        }

        private static bool AnimationIs(Animator animator, string state)
        {
            int hash = Animator.StringToHash(state);
            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash ||
                   animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash == hash;
        }

        private static void AssertCommand(Case test, string animation, bool immediate)
        {
            test.animator.Update(0);
            Require(AnimationIs(test.animator, animation), test.name + " did not immediately target animation " + animation);
            Require(immediate || test.animator.IsInTransition(0), test.name + " " + animation + " did not blend from its prior animation.");
        }

        private static int EffectCount(string name) => Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
            .Count(particle => particle.transform.root.name.StartsWith(name, StringComparison.Ordinal));
        private static void Next(Case test, int phase) { test.phase = phase; test.since = Time.time; }
        private static object Get(object value, string field) => value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
        private static void Set(object value, string field, object replacement) => value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, replacement);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
