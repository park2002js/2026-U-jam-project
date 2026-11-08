using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using PolygonArsenal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    public static class PolygonArsenalDemoChecks
    {
        const string Root = "Assets/Polygon Arsenal/Demo";
        const string Work = ".utmp/ArsenalDemoChecks";
        static readonly List<string> Results = new List<string>();
        static readonly HashSet<string> Errors = new HashSet<string>();
        static readonly string[] CanvasNames = { "CanvasSceneSelectCom", "CanvasMissiles", "CanvasTips", "CanvasSceneSelectInt", "CanvasSceneSelectEnv" };
        static readonly MethodInfo Toggle = typeof(PolygonSceneSelect).GetMethod("ToggleCanvas", BindingFlags.Static | BindingFlags.NonPublic);
        static string[] paths;
        static Dictionary<string, MethodInfo> loaders;
        static PolygonSceneSelect dispatcher;
        static int index, loadedFrame, exercisedFrame;
        static double requestedAt, loadedAt, exercisedAt;
        static bool exercised;
        public static bool IsRunning { get; private set; }
        public static bool IsComplete { get; private set; }
        public static int FailureCount { get; private set; }

        [MenuItem("Tools/Polygon Arsenal/Run All Demo Checks", true)]
        static bool CanBegin() => Application.isPlaying && Time.frameCount >= 3 && !IsRunning;

        [MenuItem("Tools/Polygon Arsenal/Run All Demo Checks")]
        public static void Begin()
        {
            if (IsRunning) return;
            if (!Application.isPlaying || Time.frameCount < 3) throw new InvalidOperationException("Run the demo checks after Play mode has started.");
            Directory.CreateDirectory(Work);
            Results.Clear();
            FailureCount = 0;
            IsComplete = false;
            index = 0;
            try
            {
                paths = AssetDatabase.FindAssets("t:Scene", new[] { Root + "/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(path => Path.GetFileNameWithoutExtension(path) == "PolyBeamUp" ? 0 : 1).ThenBy(path => path).ToArray();
                Require(paths.Length == 60, "Expected all 60 Arsenal demo scenes.");
                Require(paths.Select(Path.GetFileNameWithoutExtension).Distinct().Count() == paths.Length, "Demo scene names are ambiguous.");
                loaders = Regex.Matches(File.ReadAllText(Root + "/Scripts/PolygonSceneSelect.cs"),
                    @"public void\s+(\w+)\s*\(\)\s*\{\s*LoadDemoScene\(""([^""]+)""\)")
                    .Cast<Match>().ToDictionary(match => match.Groups[2].Value, match => typeof(PolygonSceneSelect).GetMethod(match.Groups[1].Value));
                Require(loaders.Count == 55 && loaders.Values.All(method => method != null), "Expected the 55 real navigation callbacks.");
                Require(loaders.Keys.All(name => paths.Any(path => Path.GetFileNameWithoutExtension(path) == name)), "A navigation callback points to a missing scene.");
                CheckButtonLinks();
                var root = new GameObject("PolygonArsenalDemoChecks") { hideFlags = HideFlags.HideAndDontSave };
                root.SetActive(false);
                Object.DontDestroyOnLoad(root);
                dispatcher = root.AddComponent<PolygonSceneSelect>();
                dispatcher.enabled = false;
                IsRunning = true;
                Application.logMessageReceived += Capture;
                EditorApplication.update += Step;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                LoadCurrent();
            }
            catch (Exception exception) { Fail(exception); Finish(); }
        }

        static void CheckButtonLinks()
        {
            int calls = 0;
            var seen = new HashSet<MethodInfo>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Demo Prefabs/GUI" }))
                foreach (var button in AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponentsInChildren<Button>(true))
                    for (int call = 0; call < button.onClick.GetPersistentEventCount(); call++)
                    {
                        string name = button.onClick.GetPersistentMethodName(call);
                        if (!Regex.IsMatch(name, @"^(CB|ENV|INT)Load")) continue;
                        var method = typeof(PolygonSceneSelect).GetMethod(name);
                        Require(button.onClick.GetPersistentTarget(call) is PolygonSceneSelect && method != null,
                            button.name + " has an invalid scene button target: " + name);
                        seen.Add(method);
                        calls++;
                    }
            Require(calls == 93 && loaders.Values.All(seen.Contains), "Expected all 93 valid UI links covering the 55 navigation callbacks.");
        }

        static void LoadCurrent()
        {
            Errors.Clear();
            loadedAt = -1;
            exercised = false;
            requestedAt = EditorApplication.timeSinceStartup;
            WriteProgress();
            try
            {
                if (loaders.TryGetValue(Path.GetFileNameWithoutExtension(paths[index]), out var method)) method.Invoke(dispatcher, null);
                else EditorSceneManager.LoadSceneInPlayMode(paths[index], new LoadSceneParameters(LoadSceneMode.Single));
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception exception) { Errors.Add(Unwrap(exception).ToString()); CompleteCurrent(); }
        }

        static void Step()
        {
            if (!IsRunning) return;
            try
            {
                EditorApplication.QueuePlayerLoopUpdate();
                double now = EditorApplication.timeSinceStartup;
                var scene = SceneManager.GetActiveScene();
                if (now - requestedAt > 15)
                {
                    Errors.Add("Scene startup/control checks timed out: active=" + scene.path + "; loaded=" + scene.isLoaded +
                        "; expectedLoaded=" + SceneManager.GetSceneByPath(paths[index]).isLoaded +
                        "; frames=" + Time.frameCount + "; loadedFrame=" + loadedFrame + "; paused=" + EditorApplication.isPaused);
                    CompleteCurrent();
                    return;
                }
                if (scene.path != paths[index] || !scene.isLoaded)
                    return;
                if (loadedAt < 0) { loadedAt = now; loadedFrame = Time.frameCount; }
                if (!exercised && now - loadedAt >= .4 && Time.frameCount - loadedFrame >= 3)
                {
                    exercised = true;
                    exercisedAt = now;
                    exercisedFrame = Time.frameCount;
                    try { ExerciseControls(); }
                    catch (Exception exception) { Errors.Add(Unwrap(exception).ToString()); }
                }
                if (exercised && now - exercisedAt >= .6 && Time.frameCount - exercisedFrame >= 3) CompleteCurrent();
            }
            catch (Exception exception) { Errors.Add(Unwrap(exception).ToString()); CompleteCurrent(); }
        }

        static void ExerciseControls()
        {
            Require(Toggle != null, "The shared canvas toggle was not found.");
            foreach (string name in CanvasNames)
            {
                var target = GameObject.Find(name);
                var canvas = target != null ? target.GetComponent<Canvas>() : null;
                bool original = canvas != null && canvas.enabled;
                object[] arguments = { name, false };
                Toggle.Invoke(null, arguments);
                Require(canvas == null || (canvas.enabled != original && (bool)arguments[1] == !canvas.enabled), name + " did not toggle correctly.");
                Toggle.Invoke(null, arguments);
                Require(canvas == null ? !(bool)arguments[1] : canvas.enabled == original, name + " was not restored.");
            }
            foreach (var projectile in Object.FindObjectsByType<PolygonFireProjectile>(FindObjectsSortMode.None).Where(value => value.isActiveAndEnabled))
            {
                Require(Camera.main != null && EventSystem.current != null, "Projectile demo has no Main Camera or EventSystem.");
                projectile.nextEffect();
                projectile.previousEffect();
            }
            foreach (var beam in Object.FindObjectsByType<PolygonBeamScript>(FindObjectsSortMode.None).Where(value => value.isActiveAndEnabled))
            {
                beam.nextBeam();
                beam.previousBeam();
            }
        }

        static void Capture(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert ||
                (type == LogType.Warning && message.IndexOf("script", StringComparison.OrdinalIgnoreCase) >= 0 && message.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0))
                Errors.Add(message + (string.IsNullOrEmpty(stack) ? "" : "\n" + stack.Trim()));
        }

        static void CompleteCurrent()
        {
            if (Errors.Count > 0) FailureCount++;
            Results.Add((Errors.Count == 0 ? "PASS " : "FAIL ") + Path.GetFileNameWithoutExtension(paths[index]) +
                (Errors.Count == 0 ? "" : "\n" + string.Join("\n", Errors)));
            index++;
            WriteResults();
            if (index >= paths.Length) Finish();
            else LoadCurrent();
        }

        static void Fail(Exception exception)
        {
            FailureCount++;
            Results.Add("FAIL runner: " + Unwrap(exception));
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode || !IsRunning) return;
            Fail(new InvalidOperationException("Play mode ended before the complete demo scan."));
            Finish();
        }

        static void Finish()
        {
            Application.logMessageReceived -= Capture;
            EditorApplication.update -= Step;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (dispatcher != null)
            {
                if (Application.isPlaying) Object.Destroy(dispatcher.gameObject);
                else Object.DestroyImmediate(dispatcher.gameObject);
            }
            dispatcher = null;
            IsRunning = false;
            IsComplete = true;
            WriteResults();
            File.WriteAllText(Work + "/progress.txt", "Complete: " + Results.Count + " scenes/checks; failures=" + FailureCount);
            Debug.Log("[PolygonArsenalDemoChecks] Completed " + Results.Count + " scenes/checks; failures=" + FailureCount + ". See " + Work + "/results.txt");
        }

        static void WriteProgress() => File.WriteAllText(Work + "/progress.txt", (index + 1) + "/" + paths.Length + " " + Path.GetFileNameWithoutExtension(paths[index]) + "; failed scenes=" + FailureCount);
        static void WriteResults() => File.WriteAllText(Work + "/results.txt", "Completed=" + index + "; failures=" + FailureCount + "; running=" + IsRunning + "\n" + string.Join("\n", Results));
        static Exception Unwrap(Exception exception) => exception is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : exception;
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
