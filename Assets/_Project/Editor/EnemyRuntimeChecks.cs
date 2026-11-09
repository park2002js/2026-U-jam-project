using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Enemy.Projectiles;
using UJam.Runtime.Grid;
using UJam.Runtime.Phase;
using UJam.Runtime.Shop;
using UJam.Runtime.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    [InitializeOnLoad]
    public static class EnemyRuntimeChecks
    {
        private const string Request = "Library/CheckEnemyRuntime.request";
        private const string Pending = "UJam.EnemyChecks.Pending";
        private const string Scenes = "UJam.EnemyChecks.Scenes";
        private const string Report = "Library/EnemyRuntimeCheckReport.json";
        private const string Prefabs = "Assets/_Project/Prefabs/Enemy/";
        [Serializable] private class SceneRecord { public string path; public bool loaded, active; }
        [Serializable] private class SceneRecords { public List<SceneRecord> scenes = new List<SceneRecord>(); }
        [Serializable] private class Result { public string result, error; public List<string> checks = new List<string>(); }
        private static Result result;
        private static MeleeEnemy target;
        private static EnemyBase attacker;
        private static WitchEnemy witch;
        private static SlimeEnemy slime;
        private static RollbugEnemy movingBug;
        private static EnemyBase dyingGolem;
        private static MeleeEnemy stunnedTarget;
        private static RollbugEnemy stunnedBug;
        private static WaveController wave;
        private static Wallet rewardWallet;
        private static WitchEnemy receivingWitch;
        private static WitchDeathVfx receivingWitchVfx;
        private static float receivingWitchDeathAt;
        private static bool witchBodyChecked, witchBodyGoneChecked, witchStaffGoneChecked, witchVfxGoneChecked;
        private static float started, attackStarted, hpBefore, launchAt, secondHitAt, firstHitAt;
        private static int attackCase;
        private static bool summonChecked, splitChecked, bounceChecked, deathChecked, attackFinished, stunChecked;
        private static bool transitionsReported;
        private static WitchEnemy fieldWitch;
        private static EnemyVfx.Binding casterField;
        private static GameObject fieldInstance;
        private static readonly object fieldStun = new object();
        private static int fieldCase;
        private static float fieldSince;
        private static bool fieldCancelled, fieldChecksComplete, summonFieldSeen, summonImpactSeen;
        private static SkeletonEnemy[] firstFrameSkeletons;
        private static Vector3[][] firstFrameVertices;
        private static bool spawnPoseChecked;
        private static float spawnPoseSince;
        private static float previousBugX;
        private static readonly string[] AttackNames = { "Golem", "Big_Golem", "Eyebeast", "Sniper_Eyebeast", "Witch", "Skeleton", "Slime", "Golem" };
        private static readonly float[] HitTimes = { .3f, .3f, .25f, .5f, .55f, 1.7f, .45f, .15f };
        private static readonly float[] Damages = { 20, 30, 80, 100, 50, 80, 80, 20 };

        static EnemyRuntimeChecks()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += ModeChanged;
        }

        private static void Update()
        {
            if (SessionState.GetBool(Pending, false))
            {
                if (EditorApplication.isPlaying && result != null)
                {
                    try { Tick(); }
                    catch (Exception exception) { Finish(exception); }
                }
                return;
            }
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { File.Delete(Request); }
            catch (IOException) { return; }
            Run();
        }

        [MenuItem("Tools/Enemies/Run Runtime Checks")]
        public static void Run()
        {
            result = new Result { result = "running" };
            try
            {
                var saved = new SceneRecords();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    Require(!SceneManager.GetSceneAt(i).isDirty, "Save the open scenes before running isolated runtime checks.");
                foreach (SceneSetup scene in EditorSceneManager.GetSceneManagerSetup())
                    saved.scenes.Add(new SceneRecord { path = scene.path, loaded = scene.isLoaded, active = scene.isActive });
                SessionState.SetString(Scenes, JsonUtility.ToJson(saved));
                EnemyCoreChecks.Run();
                SessionState.SetBool(Pending, true);
                File.WriteAllText(Report, JsonUtility.ToJson(result, true));
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static void ModeChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                try { Setup(); }
                catch (Exception exception) { Finish(exception); }
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
                var saved = JsonUtility.FromJson<SceneRecords>(SessionState.GetString(Scenes, "{}"));
                SceneSetup[] scenes = saved.scenes.Where(s => !string.IsNullOrEmpty(s.path))
                    .Select(s => new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray();
                if (scenes.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(scenes);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static T Spawn<T>(string name, Vector3 position) where T : EnemyBase
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
            return Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<T>();
        }

        private static void Setup()
        {
            result = new Result { result = "running" };
            rewardWallet = new GameObject("Test Reward Wallet").AddComponent<Wallet>();
            var gridObject = new GameObject("Test Grid");
            gridObject.SetActive(false);
            GridSystem grid = gridObject.AddComponent<GridSystem>();
            grid.CellWidth = grid.CellHeight = 2;
            grid.ColumnCount = 16; grid.RowCount = 100; grid.BaseCoreRow = 0;
            gridObject.SetActive(true);
            CheckIncomingDamage();
            CheckGolemStrikeContact();
            EnemyAnimationTransitionChecks.Begin();
            target = Spawn<MeleeEnemy>("Golem", Vector3.zero);
            target.Status.ConfigureSpawnStats(100000, 0, 0);
            target.BeginCombat(target.gameObject);
            target.FSM.SetState(EnemyStateType.Idle);
            started = Time.time;
            attackCase = 0;
            StartAttack();

            witch = Spawn<WitchEnemy>("Witch", new Vector3(-8, 0, -70));
            witch.BeginCombat(target.gameObject);
            fieldWitch = Spawn<WitchEnemy>("Witch", new Vector3(8, 0, 100));
            fieldWitch.PrepareForTesting(null);
            casterField = fieldWitch.GetComponent<EnemyVfx>().Bindings.Single(binding => binding.Signal == "SummonStart");
            fieldWitch.AnimationSignal("SummonStart");
            Require(FindCasterField() == null, "Idle Witch accepted a stray SummonStart signal.");
            fieldCase = 0;
            fieldCancelled = fieldChecksComplete = summonFieldSeen = summonImpactSeen = false;
            fieldWitch.FSM.SetState(EnemyStateType.Summon);
            fieldSince = Time.time;
            CheckSkeletonSpawnStart();
            slime = Spawn<SlimeEnemy>("Slime", new Vector3(8, 0, -70));
            slime.BeginCombat(target.gameObject);
            var waveObject = new GameObject("Test Wave");
            waveObject.SetActive(false);
            wave = waveObject.AddComponent<WaveController>();
            typeof(WaveController).GetProperty("Instance").SetValue(null, wave);
            Set(wave, "_isWaveRunning", true);
            Set(wave, "_remainingEnemyCount", 1);
            ((HashSet<int>)Get(wave, "_aliveEnemyIds")).Add(slime.gameObject.GetInstanceID());
            long goldBeforeSlime = rewardWallet.Gold;
            slime.TakeDamage(new DamageInfo(1000));
            Require(rewardWallet.Gold - goldBeforeSlime == slime.Status.Credits,
                "Main Slime did not pay its death reward once.");
            Require(slime.TakeDamage(new DamageInfo(1000)) == 0 &&
                    rewardWallet.Gold - goldBeforeSlime == slime.Status.Credits,
                "Repeated damage paid the main Slime reward twice.");
            Require(Object.FindObjectsByType<SlimeEnemy>(FindObjectsSortMode.None).Count(s => s.transform.position.z < -30f && s != slime) == 2
                && (int)Get(wave, "_remainingEnemyCount") == 2,
                "Slime must create and register both children in the death call before reporting the parent.");
            Require(slime.GetComponent<EnemyVfx>().PlayedEffects == 2, "Slime death must immediately splash once over each child.");

            MeleeEnemy distantTarget = Spawn<MeleeEnemy>("Golem", new Vector3(100, 0, 100));
            distantTarget.Status.ConfigureSpawnStats(10000, 0, 0);
            distantTarget.BeginCombat(distantTarget.gameObject);
            distantTarget.FSM.SetState(EnemyStateType.Idle);
            stunnedTarget = distantTarget;
            stunnedBug = Spawn<RollbugEnemy>("Rollbug", new Vector3(100, 0, 99.5f));
            var stunSource = new object();
            BuffManager.Instance.SetCondition(stunnedBug.Status, stunSource, BuffStat.Stun, 100);
            stunnedBug.BeginCombat(distantTarget.gameObject);
            Require(stunnedBug.Status.HP == 20 && distantTarget.Status.HP == 10000, "Stunned Rollbug detonated early.");
            BuffManager.Instance.Remove(stunnedBug.Status, stunSource, BuffStat.Stun);
            movingBug = Spawn<RollbugEnemy>("Rollbug", new Vector3(14, 0, -20));
            movingBug.BeginCombat(distantTarget.gameObject);
            previousBugX = movingBug.transform.position.x;

            float hp = target.Status.HP;
            RollbugEnemy immediate = Spawn<RollbugEnemy>("Rollbug", new Vector3(0, 0, -.5f));
            immediate.BeginCombat(target.gameObject);
            Require(Mathf.Approximately(hp - target.Status.HP, 200) && immediate.Status.HP == 0,
                "Rollbug range entry did not detonate exactly once.");
            immediate.AnimationSignal("AttackImpact");
            Require(Mathf.Approximately(hp - target.Status.HP, 200), "Rollbug repeated detonation.");
            RollbugEnemy killed = Spawn<RollbugEnemy>("Rollbug", new Vector3(0, 0, -40));
            killed.BeginCombat(target.gameObject);
            hp = target.Status.HP;
            int explosionsBeforeDeath = Object.FindObjectsByType<LowpolyVfxLifetime>(FindObjectsSortMode.None).Length;
            killed.TakeDamage(new DamageInfo(100));
            Require(target.Status.HP == hp && Object.FindObjectsByType<LowpolyVfxLifetime>(FindObjectsSortMode.None).Length == explosionsBeforeDeath,
                "Killed Rollbug must not damage a target or create a separate death explosion.");
            result.checks.Add("Rollbug immediate 200 damage once; ordinary death adds no damage or second explosion");
            hpBefore = target.Status.HP;

            dyingGolem = Spawn<EnemyBase>("Golem", new Vector3(0, 0, -1));
            dyingGolem.BeginCombat(target.gameObject);
            dyingGolem.TakeDamage(new DamageInfo(1000));
            Require(!dyingGolem.FSM.SetState(EnemyStateType.Move), "Dead state allowed movement.");
        }

        private static void StartAttack()
        {
            attacker = Spawn<EnemyBase>(AttackNames[attackCase], new Vector3(0, 0, -1));
            if (attackCase == 7) attacker.Status.ModifyAttackSpeed(1);
            attackStarted = Time.time;
            hpBefore = target.Status.HP;
            launchAt = -1;
            firstHitAt = secondHitAt = -1;
            attacker.BeginCombat(target.gameObject);
        }

        private static void Tick()
        {
            float elapsed = Time.time - started;
            Require(elapsed < 22, "Runtime checks timed out.");
            EnemyAnimationTransitionChecks.Tick();
            if (!transitionsReported && EnemyAnimationTransitionChecks.IsComplete)
            {
                result.checks.AddRange(EnemyAnimationTransitionChecks.Results);
                transitionsReported = true;
            }
            CheckWitchDeathVfx();
            CheckCasterField();
            if (!spawnPoseChecked && Time.time - spawnPoseSince >= .2f)
            {
                for (int i = 0; i < firstFrameSkeletons.Length; i++)
                {
                    SkeletonEnemy skeleton = firstFrameSkeletons[i];
                    Require(skeleton.FSM.state == EnemyStateType.Spawn && !skeleton.IsAttackPlaying &&
                            !skeleton.Animation.IsBlending && skeleton.Animation.NormalizedTime > 0f &&
                            BakedVertices(skeleton).Where((vertex, index) => Vector3.Distance(vertex, firstFrameVertices[i][index]) > .001f).Any(),
                        "Skeleton did not advance naturally from its buried Spawn pose before acting.");
                }
                spawnPoseChecked = true;
                result.checks.Add("Witch SummonImpact and wave-style BeginCombat apply Skeleton's buried Spawn F0 immediately without Idle blending, then advance Spawn before acting");
            }
            if (!attackFinished) CheckAttack();
            if (!stunChecked && elapsed > .15f)
            {
                Require(stunnedBug == null && stunnedTarget.Status.HP == 9800, "Rollbug did not detonate once after stun ended.");
                stunChecked = true;
                result.checks.Add("Rollbug waits through stun and detonates once after recovery");
            }
            if (elapsed < .58f)
                Require(Object.FindObjectsByType<SlimeEnemy>(FindObjectsSortMode.None).Count(s => s.transform.position.z < -30f && s != slime) == 2,
                    "Slime's immediate split children vanished during the parent sinking animation.");
            if (elapsed < 4.8f) Require(dyingGolem != null, "Golem disappeared before its five-second Death finished.");
            if (!splitChecked && elapsed >= .72f)
            {
                SlimeEnemy[] children = Object.FindObjectsByType<SlimeEnemy>(FindObjectsSortMode.None)
                    .Where(s => s.transform.position.z < -30f && s != slime).ToArray();
                Require(children.Length == 2, "Slime did not keep exactly two immediately created children.");
                long goldBeforeChildren = rewardWallet.Gold;
                foreach (SlimeEnemy child in children)
                {
                    Require(child.transform.localScale == Vector3.one * .5f && child.Status.HP == 75 &&
                        child.Status.OutgoingDamage == 40 && child.Status.Credits == 0, "Slime child stats or scale are wrong.");
                    int deathCount = 0;
                    int effectsBeforeDeath = child.GetComponent<EnemyVfx>().PlayedEffects;
                    child.Status.Died += () => deathCount++;
                    Require(child.TakeDamage(new DamageInfo(1000)) == 75f &&
                            child.TakeDamage(new DamageInfo(1000)) == 0f && deathCount == 1,
                        "Slime child death was not applied exactly once.");
                    Require(child.GetComponent<EnemyVfx>().PlayedEffects == effectsBeforeDeath,
                        "A non-splitting child must not play the parent split splash.");
                }
                Require(rewardWallet.Gold == goldBeforeChildren, "Slime children paid an extra reward.");
                slime.AnimationSignal("DeathSplit");
                slime.AnimationSignal("DeathSplit");
                Require(Object.FindObjectsByType<SlimeEnemy>(FindObjectsSortMode.None).Count(s => s.transform.position.z < -30f && s != slime) == 2,
                    "Repeated Slime split signals created extra children.");
                Require((int)Get(wave, "_remainingEnemyCount") == 0, "Dynamic wave counting did not finish after children died.");
                splitChecked = true;
                result.checks.Add("Slime splits immediately once with two child splashes; half-size/75HP/40damage/0reward children never resplit or splash, and wave/reward counting remains exact");
            }
            if (!bounceChecked && movingBug != null)
            {
                Require(movingBug.GetComponent<Collider>().bounds.max.x <= 16.001f, "Rollbug collider crossed the right wall.");
                if (elapsed > .5f && movingBug.transform.position.x < previousBugX)
                {
                    Require(movingBug.transform.position.z > -20, "Rollbug stopped moving down after bounce.");
                    bounceChecked = true;
                    result.checks.Add("Rollbug bounces within x=16 while continuing +Z movement");
                }
                previousBugX = movingBug.transform.position.x;
            }
            if (!deathChecked && elapsed > 5.15f)
            {
                Require(dyingGolem == null, "Golem Death did not remove it after the five-second animation.");
                deathChecked = true;
                result.checks.Add("Death cancels attack; Golem removed after its five-second Death clip");
            }
            if (!summonChecked)
            {
                SkeletonEnemy[] skeletons = Object.FindObjectsByType<SkeletonEnemy>(FindObjectsSortMode.None)
                    .Where(s => s.transform.position.z < -30f).ToArray();
                if (witch.FSM.state == EnemyStateType.Summon && witch.Animation.NormalizedTime > .05f &&
                    witch.Animation.NormalizedTime < .9f)
                {
                    EnemyVfx.Binding binding = witch.GetComponent<EnemyVfx>().Bindings.Single(item => item.Signal == "SummonStart");
                    Require(!witch.IsAttackPlaying && binding.Anchor.GetComponentsInChildren<ParticleSystem>()
                            .Any(system => system.particleCount > 0),
                        "Witch caster field disappeared during Summon while no Attack was playing.");
                    summonFieldSeen = true;
                }
                if (!summonImpactSeen && skeletons.Length > 0)
                {
                    Require(skeletons.Length == 2 && witch.FSM.state == EnemyStateType.Summon &&
                            witch.Animation.NormalizedTime >= .2f && witch.Animation.NormalizedTime < .4f,
                        "Witch field changed the two-Skeleton Summon21F spawn timing.");
                    summonImpactSeen = true;
                }
                if (elapsed < 10.25f) Require(skeletons.Length == 0, "Witch summoned before its first ten-second cooldown and21F impact.");
                if (elapsed > 12.6f)
                {
                    Require(summonFieldSeen && summonImpactSeen, "Natural Witch summon did not display its caster field and create two Skeletons at21F.");
                    EnemyVfx.Binding binding = witch.GetComponent<EnemyVfx>().Bindings.Single(item => item.Signal == "SummonStart");
                    Require(binding.Anchor.childCount == 0, "Witch caster field remained after SummonComplete.");
                    Require(skeletons.Length == 2, "Witch did not summon two Skeletons.");
                    Require(skeletons.All(s => s.FSM.state == EnemyStateType.Move), "Skeleton did not finish Spawn before moving.");
                    witch.TakeDamage(new DamageInfo(1000));
                    Require(skeletons.All(s => s != null && s.Status.HP > 0), "Witch death removed its Skeletons.");
                    summonChecked = true;
                    result.checks.Add("Witch first summon after10s at21F; two Skeletons finish1.5s Spawn and survive Witch death");
                }
            }
            if (attackFinished && summonChecked && splitChecked && bounceChecked && deathChecked && stunChecked && witchVfxGoneChecked && transitionsReported && fieldChecksComplete && spawnPoseChecked) Finish(null);
        }

        private static void CheckSkeletonSpawnStart()
        {
            SkeletonEnemy waveSkeleton = Spawn<SkeletonEnemy>("Skeleton", new Vector3(12, 0, 100));
            waveSkeleton.BeginCombat(null);
            var existing = new HashSet<SkeletonEnemy>(Object.FindObjectsByType<SkeletonEnemy>(FindObjectsSortMode.None));
            fieldWitch.AnimationSignal("SummonImpact");
            SkeletonEnemy[] summoned = Object.FindObjectsByType<SkeletonEnemy>(FindObjectsSortMode.None)
                .Where(skeleton => !existing.Contains(skeleton)).ToArray();
            Require(summoned.Length == 2, "Witch SummonImpact did not create exactly two Skeletons in its call frame.");
            firstFrameSkeletons = summoned.Concat(new[] { waveSkeleton }).ToArray();
            firstFrameVertices = new Vector3[firstFrameSkeletons.Length][];
            AnimationClip spawn = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/EnemyChar/Skeleton/Skeleton_Spawn.anim");
            Require(spawn != null, "Skeleton first-frame check requires its Spawn clip.");
            for (int i = 0; i < firstFrameSkeletons.Length; i++)
            {
                SkeletonEnemy skeleton = firstFrameSkeletons[i];
                Require(skeleton.FSM.state == EnemyStateType.Spawn && skeleton.Animation.CurrentAnimation == "Spawn" &&
                        !skeleton.Animation.IsBlending && skeleton.Animation.NormalizedTime == 0f && !skeleton.IsAttackPlaying,
                    "New Skeleton did not enter Spawn without an Idle blend in its creation call.");
                Vector3[] actual = BakedVertices(skeleton);
                spawn.SampleAnimation(skeleton.transform.Find("Model").gameObject, 0f);
                Vector3[] expected = BakedVertices(skeleton);
                Require(actual.Length > 0 && actual.Length == expected.Length &&
                        actual.Where((vertex, index) => Vector3.Distance(vertex, expected[index]) > .001f).Any() == false &&
                        expected.Max(vertex => vertex.y) < .05f,
                    "New Skeleton's visible mesh did not match its buried Spawn F0 pose in the creation call.");
                firstFrameVertices[i] = actual;
            }
            spawnPoseSince = Time.time;
            spawnPoseChecked = false;
        }

        private static Vector3[] BakedVertices(EnemyBase enemy)
        {
            var vertices = new List<Vector3>();
            var mesh = new Mesh();
            try
            {
                foreach (SkinnedMeshRenderer renderer in enemy.transform.Find("Model").GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.BakeMesh(mesh);
                    foreach (Vector3 vertex in mesh.vertices)
                        vertices.Add(enemy.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex)));
                }
            }
            finally { Object.DestroyImmediate(mesh); }
            return vertices.ToArray();
        }

        private static GameObject FindCasterField() => casterField.Anchor.Cast<Transform>()
            .Where(child => child.name == casterField.Effect.name + "(Clone)").Select(child => child.gameObject).SingleOrDefault();

        private static void CheckCasterField()
        {
            if (fieldChecksComplete || Time.time - fieldSince < .15f) return;
            if (!fieldCancelled)
            {
                fieldInstance = FindCasterField();
                Require(fieldInstance != null && !fieldWitch.IsAttackPlaying &&
                        fieldInstance.GetComponent<ParticleSystem>().particleCount > 0,
                    "Witch caster field did not appear on a fresh Summon playback.");
                fieldWitch.transform.position += Vector3.right * .25f;
                Require(Vector3.Distance(fieldInstance.transform.position, casterField.Anchor.position) < .001f,
                    "Witch caster field did not follow its caster's ground anchor.");
                switch (fieldCase)
                {
                    case 0: fieldWitch.FSM.SetState(EnemyStateType.Move); break;
                    case 1: BuffManager.Instance.SetCondition(fieldWitch.Status, fieldStun, BuffStat.Stun, 100); break;
                    case 2: fieldWitch.gameObject.SetActive(false); break;
                    case 3: fieldWitch.TakeDamage(new DamageInfo(1000)); break;
                }
                fieldCancelled = true;
                fieldSince = Time.time;
                return;
            }
            Require(fieldInstance == null && FindCasterField() == null,
                "Witch caster field survived cancellation case " + fieldCase + " (Move/stun/deactivate/death).");
            if (fieldCase == 3)
            {
                fieldChecksComplete = true;
                result.checks.Add("Witch caster field: stray Idle signal rejected; visible without Attack; follows caster; clears on Move/stun/deactivate/death and restarts on recast");
                return;
            }
            if (fieldCase == 1) BuffManager.Instance.Remove(fieldWitch.Status, fieldStun, BuffStat.Stun);
            if (fieldCase == 2) fieldWitch.gameObject.SetActive(true);
            fieldWitch.FSM.SetState(EnemyStateType.Idle);
            fieldWitch.FSM.SetState(EnemyStateType.Summon);
            fieldCase++;
            fieldCancelled = false;
            fieldSince = Time.time;
        }

        private static void CheckGolemStrikeContact()
        {
            foreach (string name in new[] { "Golem", "Big_Golem" })
            {
                EnemyBase golem = Spawn<EnemyBase>(name, new Vector3(40, 1, 100));
                EnemyVfx vfx = golem.GetComponent<EnemyVfx>();
                EnemyVfx.Binding impact = vfx.Bindings.Single(binding => binding.Signal == "AttackImpact");
                Require(impact.AtTarget && impact.UseAnchorPosition && impact.Anchor != null && impact.Anchor.parent.name == "Hand_R",
                    name + " attack dust must use its animated arm-end contact.");
                Vector3 raisedTarget = new Vector3(41, 50, 101);
                int before = vfx.PlayedEffects;
                vfx.Signal("AttackImpact", false, raisedTarget);
                Require(vfx.PlayedEffects == before, name + " invalid target emitted impact dust.");
                vfx.Signal("AttackImpact", true, raisedTarget);
                Vector3 contact = impact.Anchor.position;
                LowpolyVfxLifetime effect = Object.FindObjectsByType<LowpolyVfxLifetime>(FindObjectsSortMode.None)
                    .Single(item => item.name == impact.Effect.name + "(Clone)" && Vector3.Distance(item.transform.position, contact) < .001f);
                Require(vfx.PlayedEffects == before + 1 && effect != null, name + " dust did not originate at the animated arm tip.");
                Object.Destroy(effect.gameObject);
                Object.Destroy(golem.gameObject);
            }
            result.checks.Add("Both Golem impact clouds originate at the animated distal arm surface, reject invalid targets and ignore target pivots");
        }

        private static void CheckIncomingDamage()
        {
            string[] names = { "Golem", "Big_Golem", "Eyebeast", "Sniper_Eyebeast", "Witch", "Skeleton", "Slime", "Rollbug" };
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            Require(enemyLayer >= 0, "Enemy layer is missing.");
            for (int i = 0; i < names.Length; i++)
            {
                string enemyName = names[i];
                EnemyBase victim = Spawn<EnemyBase>(enemyName, new Vector3(-24f - i * 12f, 0f, 200f));
                EnemyStatus status = victim.Status;
                int hpEvents = 0, reports = 0, deaths = 0, rewardEvents = 0;
                float eventHp = -1, eventMax = -1, reportedHp = -1, reportedMax = -1, reportedDamage = -1;
                Action<float, float> changed = (hp, max) => { hpEvents++; eventHp = hp; eventMax = max; };
                Action<EnemyStatus, float, float, float> reported = (source, hp, max, applied) =>
                {
                    if (source != status) return;
                    reports++; reportedHp = hp; reportedMax = max; reportedDamage = applied;
                };
                Action died = () => deaths++;
                Action<long> rewarded = gold => rewardEvents++;
                status.OnEnemyHpChanged += changed;
                status.Died += died;
                EnemyStatus.DamageReported += reported;
                rewardWallet.OnGoldChanged += rewarded;
                try
                {
                    Collider collider = victim.GetComponent<Collider>();
                    Require(collider != null && collider.enabled && !collider.isTrigger && collider.gameObject.layer == enemyLayer,
                        enemyName + " does not expose an enabled non-trigger Enemy collider.");
                    Physics.SyncTransforms();
                    Bounds bounds = collider.bounds;
                    var ray = new Ray(bounds.center - Vector3.right * (bounds.extents.x + 1f), Vector3.right);
                    Require(Physics.Raycast(ray, out RaycastHit hit, bounds.size.x + 2f, 1 << enemyLayer, QueryTriggerInteraction.Ignore)
                            && hit.collider == collider,
                        enemyName + " could not be hit by the player's non-trigger raycast.");
                    IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                    Require(ReferenceEquals(damageable, victim), enemyName + " ray hit did not resolve its EnemyBase damage receiver.");
                    float maximum = status.MaxHealth;
                    Require(status.HP == maximum && maximum > 0f, enemyName + " did not initialize at full health.");
                    foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                        Require(damageable.TakeDamage(new DamageInfo(invalid, "Incoming damage check", DamageSourceKind.Player)) == 0f,
                            enemyName + " accepted invalid damage.");
                    Require(status.HP == maximum && hpEvents == 0 && reports == 0 && deaths == 0,
                        enemyName + " changed HP or published health events for rejected damage.");

                    float partial = maximum * .25f;
                    float applied = damageable.TakeDamage(new DamageInfo(partial, "Incoming damage check", DamageSourceKind.Player));
                    Require(Mathf.Approximately(applied, partial) && Mathf.Approximately(status.HP, maximum - partial),
                        enemyName + " incoming damage return did not match its HP loss.");
                    Require(hpEvents == 1 && reports == 1 && deaths == 0 &&
                            Mathf.Approximately(eventHp, status.HP) && Mathf.Approximately(reportedHp, status.HP) &&
                            Mathf.Approximately(eventMax, maximum) && Mathf.Approximately(reportedMax, maximum) &&
                            Mathf.Approximately(reportedDamage, applied),
                        enemyName + " partial-hit HP events reported incorrect values or counts.");
                    victim.Idle();
                    Require(Mathf.Approximately(status.HP, maximum - partial), enemyName + " Idle reset damaged health.");
                    if (victim is SlimeEnemy) Set(victim, "_canSplit", false);
                    if (victim is WitchEnemy incomingWitch)
                    {
                        receivingWitch = incomingWitch;
                        receivingWitchVfx = victim.GetComponentInChildren<WitchDeathVfx>(true);
                        Require(receivingWitchVfx != null && !receivingWitchVfx.IsPlaying &&
                                receivingWitchVfx.BodyDissolve == 0f && receivingWitchVfx.StaffDissolve == 0f &&
                                receivingWitchVfx.BodyRendererCount == 9 && receivingWitchVfx.StaffRendererCount == 3 &&
                                WitchRenderersMatch("_bodyRenderers", true) && WitchRenderersMatch("_staffRenderers", true),
                            "Living Witch did not have a reset dissolve effect with nine body and three staff renderers.");
                        receivingWitchDeathAt = Time.time;
                    }
                    long goldBeforeDeath = rewardWallet.Gold;
                    float remaining = status.HP;
                    applied = damageable.TakeDamage(new DamageInfo(maximum * 2f, "Incoming damage check", DamageSourceKind.Player));
                    Require(Mathf.Approximately(applied, remaining) && status.HP == 0f && victim.FSM.state == EnemyStateType.Dead,
                        enemyName + " lethal hit did not clamp damage and enter Dead.");
                    Require(hpEvents == 2 && reports == 2 && deaths == 1 && eventHp == 0f && reportedHp == 0f &&
                            Mathf.Approximately(reportedDamage, remaining),
                        enemyName + " lethal hit health/death notifications were incorrect.");
                    Require(victim.GetComponentsInChildren<Collider>(true).All(c => !c.enabled),
                        enemyName + " still had enabled colliders after death.");
                    Require(rewardWallet.Gold - goldBeforeDeath == status.Credits && rewardEvents == 1,
                        enemyName + " death reward was missing or duplicated.");
                    Require(!victim.FSM.SetState(EnemyStateType.Move), enemyName + " left its terminal Dead state.");
                    Require(damageable.TakeDamage(new DamageInfo(maximum * 2f, "Incoming damage check", DamageSourceKind.Player)) == 0f &&
                            status.HP == 0f && hpEvents == 2 && reports == 2 && deaths == 1 &&
                            rewardWallet.Gold - goldBeforeDeath == status.Credits && rewardEvents == 1,
                        enemyName + " repeated lethal damage changed HP, death notifications or rewards.");
                    result.checks.Add(enemyName + " receives player-ray damage: invalid input rejected; partial HP loss and notifications correct; lethal/overkill death, collider disable and reward occur once");
                }
                finally
                {
                    status.OnEnemyHpChanged -= changed;
                    status.Died -= died;
                    EnemyStatus.DamageReported -= reported;
                    rewardWallet.OnGoldChanged -= rewarded;
                    if (victim != receivingWitch)
                    {
                        victim.gameObject.SetActive(false);
                        Object.Destroy(victim.gameObject);
                    }
                }
            }
        }

        private static void CheckWitchDeathVfx()
        {
            float elapsed = Time.time - receivingWitchDeathAt;
            if (!witchBodyChecked && elapsed >= .4f)
            {
                Require(receivingWitchVfx != null && receivingWitchVfx.IsPlaying &&
                        receivingWitchVfx.BodyDissolve > 0f && receivingWitchVfx.BodyDissolve < 1f &&
                        receivingWitchVfx.StaffDissolve == 0f && WitchRenderersMatch("_staffRenderers", true),
                    "Witch body did not dissolve during its first0.7s while the staff remained visible.");
                witchBodyChecked = true;
            }
            if (!witchBodyGoneChecked && elapsed >= .78f)
            {
                Require(receivingWitchVfx != null && receivingWitchVfx.BodyDissolve == 1f &&
                        receivingWitchVfx.StaffDissolve == 0f &&
                        WitchRenderersMatch("_bodyRenderers", false) && WitchRenderersMatch("_staffRenderers", true),
                    "Witch body did not finish dissolving by0.7s or the staff vanished early.");
                witchBodyGoneChecked = true;
            }
            if (!witchStaffGoneChecked && elapsed >= 2.78f)
            {
                Require(receivingWitchVfx != null && receivingWitchVfx.BodyDissolve == 1f &&
                        receivingWitchVfx.StaffDissolve == 1f && WitchRenderersMatch("_staffRenderers", false),
                    "Witch staff did not finish dissolving after its2.2-2.7s interval.");
                witchStaffGoneChecked = true;
            }
            if (!witchVfxGoneChecked && elapsed >= 3.15f)
            {
                Require(receivingWitch == null, "Witch remained after its three-second Death animation.");
                witchVfxGoneChecked = true;
                result.checks.Add("Witch death dissolve: nine body renderers disappear during0-0.7s; three staff renderers during2.2-2.7s; root removed after3s");
            }
        }

        private static bool WitchRenderersMatch(string field, bool enabled)
        {
            var renderers = (Renderer[])Get(receivingWitchVfx, field);
            return renderers != null && renderers.Length > 0 && renderers.All(r => r != null && r.enabled == enabled);
        }

        private static void CheckAttack()
        {
            float elapsed = Time.time - attackStarted;
            float expected = HitTimes[attackCase];
            bool projectile = attackCase == 2 || attackCase == 4;
            if (projectile && launchAt < 0 && Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)
                    .Any(p => ReferenceEquals(Get(p, "_owner"), attacker)))
            {
                launchAt = elapsed;
                Require(elapsed >= expected - .07f, "Projectile launched before its animation impact frame.");
            }
            float dealt = hpBefore - target.Status.HP;
            if (elapsed < expected - .07f) Require(dealt == 0, "Attack dealt damage before its animation impact frame.");
            if (attackCase == 5 && elapsed < 1.45f)
                Require(attacker.FSM.state == EnemyStateType.Spawn, "Skeleton acted before Spawn completed.");
            if (dealt > 0 && firstHitAt < 0)
            {
                firstHitAt = elapsed;
                Require(Mathf.Approximately(dealt, Damages[attackCase]), "Attack applied incorrect or duplicate damage.");
                Require(elapsed >= expected - .07f, "Early damage.");
                if (projectile) Require(launchAt >= 0 && elapsed >= launchAt, "Projectile damage did not follow launch.");
                if (attackCase == 7)
                {
                    attacker.AnimationSignal("AttackImpact");
                    Require(target.Status.HP == hpBefore - 20, "Repeated animation event applied duplicate damage.");
                }
                if (attackCase != 7)
                {
                    result.checks.Add(AttackNames[attackCase] + " attack: first damage at" + elapsed.ToString("F3") + "s; expected impact" + expected.ToString("F3") + "s" + (projectile ? "; projectile arrival follows launch" : ""));
                    Object.Destroy(attacker.gameObject);
                    attackCase++;
                    StartAttack();
                    return;
                }
            }
            if (attackCase == 7 && dealt >= 40)
            {
                secondHitAt = elapsed;
                Require(Mathf.Approximately(dealt, 40) && secondHitAt - firstHitAt >= .42f && secondHitAt - firstHitAt <= .65f,
                    "Attack speed doubled the cooldown or repeated a hit.");
                Require(attacker.FSM.state == EnemyStateType.Attack, "Animation completion changed the logical Attack state.");
                Require(attacker.FSM.SetState(EnemyStateType.Idle) && attacker.FSM.state == EnemyStateType.Attack && attacker.IsAttackPlaying,
                    "Requested Idle interrupted an unfinished Attack motion.");
                attacker.AnimationSignal("AttackImpact");
                Require(Mathf.Approximately(hpBefore - target.Status.HP, 40), "Duplicate impact after a queued state request dealt damage.");
                Object.Destroy(attacker.gameObject);
                attackFinished = true;
                result.checks.Add("2 attacks/sec scales Golem impact to0.15s; start cadence0.5s; duplicate impact is ignored while requested Idle waits for completion");
            }
            Animator currentAnimator = attacker != null ? attacker.GetComponentInChildren<Animator>() : null;
            AnimatorStateInfo currentState = currentAnimator != null ? currentAnimator.GetCurrentAnimatorStateInfo(0) : default;
            Require(elapsed < expected + 3f, "Attack timed out for" + AttackNames[attackCase] +
                "; FSM=" + attacker?.FSM.state + "; pending=" + attacker?.IsAttackPlaying +
                "; animation=" + currentState.shortNameHash + "; normalizedTime=" + currentState.normalizedTime +
                "; expectedAttackHash=" + Animator.StringToHash("Attack") + "; target=" + attacker?.AttackTarget +
                "; targetHP=" + target.Status.HP + "; animationRequest=" + attacker?.Animation?.CurrentAnimation);
        }

        private static void Finish(Exception error)
        {
            if (result == null) result = new Result();
            result.result = error == null ? "passed" : "failed";
            result.error = error?.ToString();
            File.WriteAllText(Report, JsonUtility.ToJson(result, true));
            if (error != null) Debug.LogException(error);
            else Debug.Log("[EnemyRuntimeChecks] Passed" + result.checks.Count + " runtime checks.");
            result = null;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            else SessionState.SetBool(Pending, false);
        }

        private static object Get(object targetObject, string field) => targetObject.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(targetObject);
        private static void Set(object targetObject, string field, object value) => targetObject.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(targetObject, value);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
