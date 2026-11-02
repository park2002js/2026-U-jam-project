using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    [InitializeOnLoad]
    public static class EnemyViewerPlayChecks
    {
        private const string Request = "Library/CheckEnemyViewer.request";
        private const string Pending = "UJam.EnemyViewerPlayChecks.Pending";
        private const string ScenePath = "Assets/_Project/Scenes/PJS_Scene.unity";
        private const string ReportPath = "Library/EnemyViewerPlayCheckReport.json";
        private static readonly string[] Names = { "Golem", "Big_Golem", "Eyebeast", "Sniper_Eyebeast", "Witch", "Skeleton", "Slime", "Rollbug" };
        private enum Step { WaitForViewer, Moving, Dying, RollbugRemoved, NaturalRollbug, NaturalSummon }
        [Serializable] private sealed class Report { public string result, error; public List<string> checks = new List<string>(); }
        private static Report report;
        private static EnemyAnimationViewer viewer;
        private static EnemyBase enemy;
        private static EnemyViewerTarget target;
        private static Vector3 targetPosition, movingFrom;
        private static int index, requestedFrame;
        private static float waitUntil, summonStarted, naturalRollHp;
        private static double deadline;
        private static Step step;
        private static string runtimeError;

        static EnemyViewerPlayChecks()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += ModeChanged;
            Application.logMessageReceived += CaptureError;
        }

        private static void CaptureError(string condition, string trace, LogType type)
        {
            if (SessionState.GetBool(Pending, false) && EditorApplication.isPlaying && runtimeError == null &&
                (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                runtimeError = condition + "\n" + trace;
        }

        private static void Poll()
        {
            if (SessionState.GetBool(Pending, false))
            {
                if (EditorApplication.isPlaying && report != null)
                {
                    try { Tick(); }
                    catch (Exception error) { Finish(error); }
                }
                return;
            }
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { File.Delete(Request); }
            catch (IOException) { return; }
            Run();
        }

        [MenuItem("UJam/Enemies/Check PJS Viewer in Play Mode")]
        public static void Run()
        {
            report = new Report { result = "running" };
            runtimeError = null;
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play mode before checking the PJS viewer.");
                Scene scene = SceneManager.GetActiveScene();
                Require(scene.path == ScenePath, "Open PJS_Scene before running its viewer checks.");
                for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
                    Require(!SceneManager.GetSceneAt(sceneIndex).isDirty, "Save open scenes before running viewer Play checks.");
                EnemyAnimationViewer configured = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyAnimationViewer>(true)).SingleOrDefault();
                Require(configured != null && configured.isActiveAndEnabled, "PJS_Scene requires its enabled Enemy Animation Viewer.");
                SessionState.SetBool(Pending, true);
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Finish(error); }
        }

        private static void ModeChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                report = new Report { result = "running" };
                step = Step.WaitForViewer;
                deadline = EditorApplication.timeSinceStartup + 35;
                index = 0;
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
                EnemyAnimationViewer restored = Object.FindFirstObjectByType<EnemyAnimationViewer>();
                if (restored != null) Selection.activeGameObject = restored.gameObject;
            }
        }

        private static void Tick()
        {
            Require(runtimeError == null, "PJS viewer produced a runtime error: " + runtimeError);
            Require(EditorApplication.timeSinceStartup < deadline, "PJS viewer Play checks timed out: " + step +
                "; time=" + Time.time + "; frame=" + Time.frameCount + "; scale=" + Time.timeScale +
                "; background=" + Application.runInBackground + "; enemy=" + enemy?.FSM.state);
            if (step == Step.WaitForViewer)
            {
                viewer = Object.FindFirstObjectByType<EnemyAnimationViewer>();
                if (viewer == null || viewer.ActiveEnemy == null) return;
                Require(Time.timeScale > 0, "Viewer test mode did not resume simulation time.");
                Require(viewer.Target != null && viewer.Target.TryGetComponent(out target), "Viewer target is missing its actual damage receiver.");
                targetPosition = viewer.Target.transform.position;
                var serialized = new SerializedObject(viewer);
                Require(serialized.FindProperty("_enemyPrefabs").arraySize == 8, "Viewer does not have eight assigned enemies.");
                Camera camera = serialized.FindProperty("_previewCamera").objectReferenceValue as Camera;
                Require(camera != null && camera.enabled && camera.gameObject.activeInHierarchy, "Viewer preview camera is not rendering.");
                Vector3 point = camera.WorldToViewportPoint(viewer.ActiveEnemy.transform.position + Vector3.up * 2);
                Require(point.z > 0 && point.x > .25f && point.x < .97f && point.y > .05f && point.y < .95f,
                    "Default enemy is outside the visible preview camera.");
                report.checks.Add("PJS opens with a visible Golem, eight assigned prefabs, active preview camera and actual damageable target");
                StartCase();
                return;
            }

            if (step == Step.Moving)
            {
                if (Time.time < waitUntil) return;
                Require(enemy != null && enemy.FSM.state == EnemyStateType.Move, Names[index] + " did not remain in actual Move toward the distant target.");
                Require(Vector3.Distance(enemy.transform.position, movingFrom) > .02f, Names[index] + " Move button did not move its root.");
                viewer.Target.transform.position = enemy.transform.position + Vector3.forward * .75f;
                float before = target.HP;
                GameObject detonation = enemy is RollbugEnemy
                    ? new SerializedObject(enemy).FindProperty("_detonationEffect").objectReferenceValue as GameObject : null;
                Require(viewer.CommandState(EnemyStateType.Attack), Names[index] + " Attack command was rejected in range.");
                if (enemy is RollbugEnemy)
                {
                    Require(enemy.FSM.state == EnemyStateType.Dead && enemy.Status.HP == 0 && Mathf.Approximately(before - target.HP, 200),
                        "Rollbug viewer Attack did not damage once and die in the same command.");
                    Require(detonation != null && Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(system =>
                        system.gameObject.name.StartsWith(detonation.name, StringComparison.Ordinal) && system.transform.parent == null),
                        "Rollbug detonation VFX was not created independently before its body removal.");
                    report.checks.Add("Rollbug viewer: Move follows the actual path; Attack gives exactly200 damage and creates independent explosion VFX before same-frame Dead");
                    requestedFrame = Time.frameCount;
                    step = Step.RollbugRemoved;
                    return;
                }
                Require(enemy.FSM.state == EnemyStateType.Attack && enemy.IsAttackPlaying, Names[index] + " did not begin its actual animated attack.");
                Require(viewer.CommandState(EnemyStateType.Dead) && enemy.FSM.state == EnemyStateType.Dead && !enemy.IsAttackPlaying,
                    Names[index] + " Dead command was queued instead of immediately interrupting the attack.");
                Require(enemy.Animation.CurrentAnimation == "Death", Names[index] + " did not request the Death animation immediately.");
                requestedFrame = Time.frameCount;
                step = Step.Dying;
                return;
            }

            if (step == Step.Dying)
            {
                if (Time.frameCount <= requestedFrame) return;
                Require(enemy != null, Names[index] + " was removed before its Death clip could play.");
                Animator animator = enemy.GetComponentInChildren<Animator>();
                Require(animator != null && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash("Death"),
                    Names[index] + " Animator did not enter Death on the next frame.");
                report.checks.Add(Names[index] + " viewer: initialized HP/target, real Move, real Attack and immediate next-frame Death interruption");
                index++;
                StartCase();
                return;
            }

            if (step == Step.RollbugRemoved)
            {
                if (Time.frameCount <= requestedFrame) return;
                Require(enemy == null, "Rollbug body remained after its same-frame detonation.");
                viewer.Target.transform.position = targetPosition;
                Require(viewer.SelectEnemy(7), "Could not reset Rollbug for its natural approach.");
                enemy = viewer.ActiveEnemy;
                naturalRollHp = target.HP;
                waitUntil = Time.time + 5f;
                step = Step.NaturalRollbug;
                return;
            }

            if (step == Step.NaturalRollbug)
            {
                Require(Time.time < waitUntil || enemy == null, "Default Rollbug did not naturally reach the diagonal test target.");
                if (enemy != null) return;
                Require(Mathf.Approximately(naturalRollHp - target.HP, 200), "Natural Rollbug approach did not detonate exactly once.");
                report.checks.Add("Default Rollbug starts from prepared Idle, approaches its diagonal target and naturally detonates once");
                Require(viewer.SelectEnemy(4) && viewer.ActiveEnemy is WitchEnemy, "Could not select Witch for its natural timer test.");
                enemy = viewer.ActiveEnemy;
                summonStarted = Time.time;
                step = Step.NaturalSummon;
                return;
            }

            if (step == Step.NaturalSummon)
            {
                Require(enemy != null, "Witch vanished before natural summoning.");
                float elapsed = Time.time - summonStarted;
                SkeletonEnemy[] skeletons = enemy.transform.parent.GetComponentsInChildren<SkeletonEnemy>();
                if (elapsed < 10.2f) Require(skeletons.Length == 0, "Prepared Witch summoned before its ten-second timer and21F event.");
                if (elapsed < 13.2f) return;
                Require(skeletons.Length == 2 && skeletons.All(skeleton => skeleton.Status.HP > 0 &&
                    (skeleton.FSM.state == EnemyStateType.Move || skeleton.FSM.state == EnemyStateType.Attack)),
                    "Viewer-prepared Witch did not naturally summon two Skeletons which finish Spawn.");
                report.checks.Add("Viewer PrepareForTesting also starts Witch's natural10s summon clock; two Skeletons finish actual Spawn and act independently");
                Finish(null);
            }
        }

        private static void StartCase()
        {
            viewer.Target.transform.position = targetPosition;
            Require(viewer.SelectEnemy(index), "Could not select viewer enemy " + Names[index]);
            enemy = viewer.ActiveEnemy;
            Require(enemy != null && enemy.Status != null && enemy.Status.HP == enemy.Status.MaxHealth && enemy.Status.HP > 0,
                Names[index] + " selection did not run full initialization.");
            Require(enemy.FSM.Targets.Count > 0 && enemy.FSM.Targets[0] == viewer.Target, Names[index] + " was not initialized with the assigned target.");
            Require(enemy.transform.localScale == Vector3.one, Names[index] + " viewer selection changed the enemy scale.");
            foreach (EnemyStateType unsupported in new[] { EnemyStateType.Spawn, EnemyStateType.Summon })
                if (!enemy.Animation.HasState(unsupported.ToString()))
                {
                    EnemyStateType previous = enemy.FSM.state;
                    Require(!viewer.CommandState(unsupported) && enemy.FSM.state == previous, Names[index] + " accepted unsupported " + unsupported);
                }
            movingFrom = enemy.transform.position;
            Require(viewer.CommandState(EnemyStateType.Move), Names[index] + " Move command was rejected.");
            waitUntil = Time.time + .2f;
            step = Step.Moving;
        }

        private static void Finish(Exception error)
        {
            if (report == null) report = new Report();
            report.result = error == null ? "passed" : "failed";
            report.error = error?.ToString();
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            if (error == null) Debug.Log("[EnemyViewerPlayChecks] Passed actual PJS viewer checks for all eight enemies.");
            else Debug.LogException(error);
            report = null;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            else SessionState.SetBool(Pending, false);
        }

        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
