using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ujam.Runtime.Item;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UJam.Runtime.Systems;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    [InitializeOnLoad]
    public static class DummyChecks
    {
        private const string Running = "UJam.DummyChecks.Running";
        private const string Completed = "UJam.DummyChecks.Completed";
        private const string Batch = "UJam.DummyChecks.Batch";
        private const string ExitCode = "UJam.DummyChecks.ExitCode";
        private const string ReportPath = "Library/DummyChecks.json";
        private const string Request = "Library/CheckDummy.request";
        private static Result result;
        private static double deadline;
        private static bool previousBackground;
        private static GameObject prefab, fallback, staging;

        [Serializable] private sealed class Result
        {
            public string result, error;
            public List<string> checks = new();
        }

        static DummyChecks()
        {
            EditorApplication.playModeStateChanged += ModeChanged;
            EditorApplication.update += Watchdog;
        }

        public static void RunBatch() => Begin(true);

        [MenuItem("Tools/Items/Run Dummy Checks")]
        public static void Run() => Begin(false);

        private static void Begin(bool batch)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Running, false))
                throw new InvalidOperationException("Run dummy checks from Edit mode.");
            result = new Result { result = "starting", error = "" };
            WriteReport();
            SessionState.SetBool(Batch, batch);
            SessionState.SetBool(Completed, false);
            SessionState.SetBool(Running, true);
            deadline = EditorApplication.timeSinceStartup + 60;
            // Entering Play mode preserves the open scenes, including unsaved edits.
            EditorApplication.EnterPlaymode();
        }

        private static void Watchdog()
        {
            if (SessionState.GetBool(Running, false) && deadline > 0 && EditorApplication.timeSinceStartup > deadline)
                Finish(new TimeoutException("Dummy checks exceeded 60 seconds."));
            if (!SessionState.GetBool(Running, false) && File.Exists(Request) && !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(Request);
                Begin(false);
            }
        }

        private static void ModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Completed, false))
            {
                SessionState.SetBool(Completed, false);
                if (SessionState.GetBool(Batch, false)) EditorApplication.Exit(SessionState.GetInt(ExitCode, 1));
                return;
            }
            if (!SessionState.GetBool(Running, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    result = new Result { result = "running", error = "" };
                    deadline = EditorApplication.timeSinceStartup + 60;
                    previousBackground = Application.runInBackground;
                    Application.runInBackground = true;
                    Time.timeScale = 1;
                    EditorApplication.isPaused = false;
                    var timer = RuntimeTimer.Ensure();
                    timer.transform.SetParent(null, true);
                    foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                        if (transform.parent == null && transform != timer.transform) transform.gameObject.SetActive(false);
                    foreach (Transform child in timer.transform) child.gameObject.SetActive(false);
                    foreach (MonoBehaviour behaviour in timer.GetComponents<MonoBehaviour>())
                        if (behaviour != timer) behaviour.enabled = false;
                    SceneManager.SetActiveScene(SceneManager.CreateScene("Dummy Runtime Checks"));
                    timer.StartCoroutine(Guard(CheckRuntime()));
                }
                catch (Exception error) { Finish(error); }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
                Finish(new InvalidOperationException("Play mode stopped before dummy checks finished."));
        }

        private static IEnumerator Guard(IEnumerator checks)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(checks);
            while (stack.Count > 0)
            {
                IEnumerator current = stack.Peek();
                bool more;
                try { more = current.MoveNext(); }
                catch (Exception error) { Finish(error); yield break; }
                if (!more) { stack.Pop(); (current as IDisposable)?.Dispose(); continue; }
                if (current.Current is IEnumerator nested && !(current.Current is CustomYieldInstruction)) stack.Push(nested);
                else yield return current.Current;
            }
            Finish(null);
        }

        private static IEnumerator CheckRuntime()
        {
            prefab = Resources.Load<GameObject>(ItemAssets.DummyPath);
            Require(prefab != null && prefab.activeSelf, "An active dummy prefab must load through ItemAssets.DummyPath.");
            var settings = new SerializedObject(prefab.GetComponent<DecoyController>());
            Require(Near(settings.FindProperty("maxHealth").floatValue, 100) &&
                Near(settings.FindProperty("duration").floatValue, 20) &&
                Near(settings.FindProperty("tauntRadius").floatValue, 6) &&
                settings.FindProperty("enemyMask").intValue == 64, "Dummy prefab defaults changed or are not serialized on DecoyController.");
            Passed("Prefab owns health 100, duration 20, world radius 6 and Enemy mask 64.");

            var gridHost = new GameObject("Dummy Check Grid");
            gridHost.SetActive(false);
            var grid = gridHost.AddComponent<GridSystem>();
            grid.CellWidth = grid.CellHeight = 2;
            grid.ColumnCount = grid.RowCount = 32;
            typeof(GridSystem).GetProperty(nameof(GridSystem.Instance)).SetValue(null, null);
            gridHost.SetActive(true);
            Require(grid.IsInitialized, "Dummy test grid did not initialize.");
            fallback = new GameObject("Dummy Check Base") { tag = "BaseCore" };
            fallback.transform.position = Vector3.forward * 40;
            staging = new GameObject("Inactive Dummy Staging");
            staging.SetActive(false);

            MeleeEnemy inside = Enemy(Vector3.right, multipleColliders: true);
            MeleeEnemy late = Enemy(Vector3.right * 10, multipleColliders: true);
            DecoyController a = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<DecoyController>();
            Require(Near(a.Health, 100) && Priority(inside) == a.gameObject &&
                inside.FSM.Targets.Count(target => target == a.gameObject) == 1 && Priority(late) == fallback,
                "Direct Instantiate did not autonomously overlap existing enemies exactly once.");
            Require(Near(a.GetComponent<SphereCollider>().radius * Mathf.Abs(a.transform.lossyScale.x), 6),
                "Dummy trigger does not follow its configured world radius.");
            int entries = 0;
            a.Taunted += enemy => { if (enemy == late) entries++; };
            yield return PhysicsFrames();
            late.transform.position = Vector3.right * 2;
            yield return PhysicsFrames();
            Require(Priority(late) == a.gameObject && entries == 1 && late.FSM.Targets.Count(target => target == a.gameObject) == 1,
                "Late multi-collider trigger entry did not share the initial taunt operation.");
            late.transform.position = Vector3.right * 10;
            yield return PhysicsFrames();
            Require(Priority(late) == a.gameObject, "Leaving the trigger prematurely released the dummy priority.");
            Passed("Direct overlap, multi-collider deduplication, later trigger entry and retained taunt after exit.");

            DecoyController b = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<DecoyController>();
            Require(Priority(inside) == b.gameObject, "The newest dummy did not take priority.");
            Require(Near(b.TakeDamage(new DamageInfo(1000)), 100) && Near(b.TakeDamage(new DamageInfo(1)), 0),
                "Lethal damage was not clamped or a dead dummy accepted extra damage.");
            Require(Priority(inside) == a.gameObject, "Destroyed dummy B did not immediately restore dummy A.");
            yield return null;
            Require(b == null, "Lethally damaged dummy was not destroyed.");
            Object.Destroy(a.gameObject);
            yield return null;
            Require(Priority(inside) == fallback && Priority(late) == fallback,
                "External Destroy did not release every taunted enemy, including one outside the trigger.");
            Passed("A/B priority stack, damage death, duplicate damage rejection, external Destroy and base fallback.");

            DecoyController reusable = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<DecoyController>();
            reusable.TakeDamage(new DamageInfo(30));
            reusable.gameObject.SetActive(false);
            Require(Priority(inside) == fallback, "Disabling a dummy did not release its targets.");
            reusable.gameObject.SetActive(true);
            Require(Near(reusable.Health, 100) && Priority(inside) == reusable.gameObject,
                "Re-enabling a dummy did not renew health and the initial overlap.");
            Object.Destroy(reusable.gameObject);
            yield return null;
            Passed("Disable cleanup and autonomous fresh overlap on re-enable.");

            MeleeEnemy startup = Enemy(Vector3.right * 100, prepare: false);
            DecoyController sameFrame = Object.Instantiate(prefab, startup.transform.position, Quaternion.identity).GetComponent<DecoyController>();
            Require(Priority(startup) == sameFrame.gameObject, "Same-frame initial overlap missed the new enemy.");
            yield return null;
            yield return null;
            Require(Priority(startup) == sameFrame.gameObject, "Enemy Start/BeginCombat erased a same-frame initial dummy taunt.");
            Object.Destroy(sameFrame.gameObject);
            Object.Destroy(startup.gameObject);
            yield return null;
            Passed("Same-frame enemy creation before Start preserves the initial dummy priority.");

            DecoyController timed = ConfiguredDummy(new Vector3(200, 0, 0), .8f, 2);
            MeleeEnemy near = Enemy(timed.transform.position + Vector3.right);
            MeleeEnemy far = Enemy(timed.transform.position + Vector3.right * 3);
            yield return PhysicsFrames();
            Require(Priority(near) == timed.gameObject && Priority(far) == fallback,
                "A custom prefab radius did not control actual trigger coverage.");
            float born = Time.time;
            Require(Near(timed.TakeDamage(new DamageInfo(50)), 50), "Nonlethal damage returned the wrong amount.");
            yield return new WaitForSeconds(.45f);
            Require(timed != null && timed.isActiveAndEnabled && Near(timed.Health, 50),
                "Nonlethal damage or elapsed time shortened the independent lifetime or drained health.");
            while (timed != null && Time.time - born < 1.2f) yield return null;
            Require(timed == null && Time.time - born < 1.1f && Priority(near) == fallback,
                "Duration expiry did not destroy the dummy and restore the fallback.");
            Passed("Configured radius and independent duration expiry after nonlethal damage.");

            inside.FSM.Targets.Clear();
            DecoyController last = Object.Instantiate(prefab, inside.transform.position + Vector3.right * 2, Quaternion.identity)
                .GetComponent<DecoyController>();
            inside.Status.SetRuntimeSpeed(1);
            inside.FSM.SetState(EnemyStateType.Move);
            yield return null;
            last.gameObject.SetActive(false);
            Vector3 stopped = inside.transform.position;
            yield return new WaitForSeconds(.1f);
            Require(!inside.FSM.TGV.TryGetTarget(out _) && Vector3.Distance(stopped, inside.transform.position) < .001f,
                "Releasing the last dummy retained an invalid target or continued moving toward the removed target.");
            Object.Destroy(last.gameObject);
            Passed("Releasing the final priority also handles an empty target stack.");
        }

        private static MeleeEnemy Enemy(Vector3 position, bool multipleColliders = false, bool prepare = true)
        {
            var host = new GameObject("Dummy Check Enemy");
            host.SetActive(false);
            host.layer = LayerMask.NameToLayer("Enemy");
            host.transform.position = position;
            host.AddComponent<EnemyStatus>().ConfigureSpawnStats(1000, 0, 0);
            host.AddComponent<SphereCollider>().radius = .2f;
            if (multipleColliders) host.AddComponent<BoxCollider>().size = Vector3.one * .3f;
            var enemy = host.AddComponent<MeleeEnemy>();
            host.SetActive(true);
            if (prepare) { enemy.PrepareForTesting(fallback); enemy.enabled = false; }
            return enemy;
        }

        private static DecoyController ConfiguredDummy(Vector3 position, float duration, float radius)
        {
            GameObject host = Object.Instantiate(prefab, position, Quaternion.identity, staging.transform);
            host.SetActive(false);
            host.transform.SetParent(null, true);
            var decoy = host.GetComponent<DecoyController>();
            var settings = new SerializedObject(decoy);
            settings.FindProperty("duration").floatValue = duration;
            settings.FindProperty("tauntRadius").floatValue = radius;
            settings.ApplyModifiedPropertiesWithoutUndo();
            host.SetActive(true);
            return decoy;
        }

        private static IEnumerator PhysicsFrames()
        {
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        private static GameObject Priority(EnemyBase enemy) => enemy.FSM.TGV.TryGetTarget(out GameObject target) ? target : null;
        private static bool Near(float a, float b) => Mathf.Abs(a - b) < .02f;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Passed(string check) { result.checks.Add(check); WriteReport(); }
        private static void WriteReport() => File.WriteAllText(ReportPath, JsonUtility.ToJson(result, true));

        private static void Finish(Exception error)
        {
            if (SessionState.GetBool(Completed, false)) return;
            SessionState.SetBool(Running, false);
            SessionState.SetBool(Completed, true);
            SessionState.SetInt(ExitCode, error == null ? 0 : 1);
            deadline = 0;
            if (result == null) result = new Result();
            result.result = error == null ? "passed" : "failed";
            result.error = error?.ToString() ?? "";
            WriteReport();
            if (error == null) Debug.Log("[DummyChecks] PASS: " + string.Join(" ", result.checks));
            else Debug.LogException(error);
            if (EditorApplication.isPlaying)
            {
                Application.runInBackground = previousBackground;
                EditorApplication.ExitPlaymode();
            }
            else if (SessionState.GetBool(Batch, false)) EditorApplication.Exit(error == null ? 0 : 1);
        }
    }
}
