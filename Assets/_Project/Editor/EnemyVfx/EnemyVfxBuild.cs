using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UJam.Runtime.Enemy;
using UnityEditor;
using UnityEngine;

namespace UJam.Editor
{
    public static class EnemyVfxBuild
    {
        public static readonly string[] Names = { "Golem", "Eyebeast", "Big_Golem", "Sniper_Eyebeast", "Witch", "Skeleton", "Slime", "Rollbug" };

        public static List<AnimationEvent> Events(string enemy, string state, float length, float hitFrame)
        {
            var events = new List<AnimationEvent>();
            void Add(string signal, float time) => events.Add(new AnimationEvent { functionName = "OnAnimationSignal", stringParameter = signal, time = time });
            if (state == "Attack")
            {
                Add("AttackImpact", hitFrame / 60f);
                if (enemy == "Sniper_Eyebeast") Add("AttackCharge", .05f);
                if (enemy == "Golem" || enemy == "Big_Golem" || enemy == "Skeleton")
                {
                    Add("AttackTrailStart", .1f);
                    Add("AttackTrailEnd", enemy == "Skeleton" ? .3f : .4f);
                }
            }
            if (state == "Summon") { Add("SummonStart", 0); Add("SummonImpact", 21 / 60f); }
            if (state == "Spawn") Add("SpawnLand", 69 / 60f);
            if (state == "Move" && (enemy == "Golem" || enemy == "Big_Golem" || enemy == "Skeleton" || enemy == "Slime"))
            {
                Add("MoveStep", length * .2f);
                Add("MoveStep", length * .7f);
            }
            if (state == "Death")
            {
                if (enemy == "Golem" || enemy == "Big_Golem") { Add("EyesOff", 5 / 60f); Add("DeathImpact", .65f); }
                if (enemy == "Eyebeast" || enemy == "Sniper_Eyebeast") Add("DeathImpact", 1.256410f);
                if (enemy == "Skeleton") Add("DeathImpact", 28 / 60f);
            }
            if (state == "Attack" || state == "Death" || state == "Spawn" || state == "Summon") Add(state + "Complete", length);
            return events.OrderBy(item => item.time).ToList();
        }

        public static void ConfigureAll(params string[] selected)
        {
            Action<GameObject>[] setups = { GolemVfxSetup.Configure, EyebeastVfxSetup.Configure, BigGolemVfxSetup.Configure,
                SniperEyebeastVfxSetup.Configure, WitchVfxSetup.Configure, SkeletonVfxSetup.Configure, SlimeVfxSetup.Configure, RollbugVfxSetup.Configure };
            for (int index = 0; index < Names.Length; index++)
            {
                if (selected.Length > 0 && !selected.Contains(Names[index])) continue;
                string path = "Assets/_Project/Prefabs/Enemy/" + Names[index] + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    EnemyVfx existing = root.GetComponent<EnemyVfx>();
                    if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
                    setups[index](root);
                    if (root.GetComponent<EnemyVfx>() == null) root.AddComponent<EnemyVfx>();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("UJam/Enemies/Build Lowpoly VFX and Review Scene")]
        public static void Build()
        {
            try
            {
                EnemyPrefabBuilder.BuildAll();
                EnemyVfxChecks.ValidateAssets();
                EnemyVfxReviewSetup.Build();
                File.WriteAllText("Library/EnemyVfxBuildReport.json", "{\"result\":\"passed\",\"enemies\":8}");
            }
            catch (Exception error)
            {
                File.WriteAllText("Library/EnemyVfxBuildReport.json", JsonUtility.ToJson(new Failure { error = error.ToString() }, true));
                throw;
            }
        }
        [Serializable] private sealed class Failure { public string result = "failed", error; }
    }
}
