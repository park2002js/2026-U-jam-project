using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UJam.Editor
{
    [InitializeOnLoad]
    public static class EnemyVfxRuntimeBatch
    {
        private const string Pending = "UJam.EnemyVfxRuntimeBatch";
        private const string Report = "Library/EnemyRuntimeCheckReport.json";
        [Serializable] private sealed class Result { public string result, error; }
        static EnemyVfxRuntimeBatch() => EditorApplication.update += Tick;

        public static void Run()
        {
            SessionState.SetBool(Pending, true);
            SessionState.SetFloat(Pending + ".Deadline", (float)EditorApplication.timeSinceStartup + 180);
            UJam.EditorTools.EnemyRuntimeChecks.Run();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Pending + ".Deadline", 0))
            {
                SessionState.SetBool(Pending, false);
                Debug.LogError("Enemy runtime checks exceeded three minutes.");
                EditorApplication.Exit(1);
                return;
            }
            if (!File.Exists(Report)) return;
            Result result = JsonUtility.FromJson<Result>(File.ReadAllText(Report));
            if (result.result == "running") return;
            Directory.CreateDirectory(EnemyVfxCapture.Output);
            File.Copy(Report, Path.Combine(EnemyVfxCapture.Output, "EnemyRuntimeCheckReport.json"), true);
            SessionState.SetBool(Pending, false);
            if (Application.isBatchMode) EditorApplication.Exit(result.result == "passed" ? 0 : 1);
        }
    }
}
