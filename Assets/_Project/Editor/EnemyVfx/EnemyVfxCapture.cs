using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UJam.Editor
{
    [InitializeOnLoad]
    public static class EnemyVfxCapture
    {
        private const string Pending = "UJam.EnemyVfxCapture";
        private const int Fps = 24;
        private const string DetailRequest = "Library/CaptureDetailedEnemyVfx.request";
        private static EnemyVfxReview review;
        private static RenderTexture render;
        private static Texture2D pixels;
        private static int index, frame, lastUnityFrame = -1, step;
        private static float started;
        private static string directory;
        private static Report report;
        private static string error;
        private static double deadline;
        private static bool sawBeam;
        private static bool sawSummonField;
        private static GameplayCamera gameplayCamera;
        private static readonly string[] Actions = { "Idle", "Move", "Attack", "Extra", "Death" };
        [Serializable] private sealed class Report { public string result = "running", error, graphicsDevice; public List<Entry> enemies = new List<Entry>(); }
        [Serializable] private sealed class Entry { public string enemy, action; public float seconds; public int hits, effects; }
        [Serializable] private sealed class SavedScene { public string path; public bool loaded, active; }
        [Serializable] private sealed class SavedScenes { public SavedScene[] scenes; }
        [Serializable] private sealed class GameplayCamera { public Vector3 offset; public Quaternion rotation; public float fov; public bool orthographic; }

        static EnemyVfxCapture()
        {
            EditorApplication.playModeStateChanged += ModeChanged;
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            { if (SessionState.GetBool(Pending, false) && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) error = message + "\n" + stack; };
        }

        public static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Art/EnemyVFX_Review"));

        [MenuItem("UJam/Enemies/Capture Eight VFX Reviews")]
        public static void Run()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isDirty) continue;
                if (string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save untitled scenes before capturing VFX.");
                File.Copy(scene.path, "Library/" + scene.name + ".BeforeVfxCapture.unity.txt", true);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not preserve " + scene.path);
            }
            SessionState.SetString(Pending + ".Scenes", JsonUtility.ToJson(new SavedScenes
            {
                scenes = EditorSceneManager.GetSceneManagerSetup().Select(scene => new SavedScene
                    { path = scene.path, loaded = scene.isLoaded, active = scene.isActive }).ToArray()
            }));
            SessionState.SetBool(Pending + ".Passed", false);
            SessionState.SetBool(Pending + ".Details", false);
            SessionState.SetBool(Pending + ".Grass", false);
            SessionState.SetInt(Pending + ".Start", 0);
            SessionState.SetInt(Pending + ".End", EnemyVfxBuild.Names.Length);
            EnemyVfxChecks.ValidateAssets();
            if (!File.Exists(EnemyVfxReviewSetup.ScenePath)) EnemyVfxReviewSetup.Build();
            File.WriteAllText("Library/EnemyVfxBuildReport.json", "{\"result\":\"passed\",\"enemies\":8,\"assetChecks\":\"passed\"}");
            SessionState.SetBool(Pending, true);
            EditorSceneManager.OpenScene(EnemyVfxReviewSetup.ScenePath);
            EditorApplication.EnterPlaymode();
        }

        public static void RetakeWitch()
        {
            const string path = "Assets/_Project/Prefabs/Enemy/Witch.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.GetComponent<UJam.Runtime.Enemy.EnemyVfx>().Bindings = Array.Empty<UJam.Runtime.Enemy.EnemyVfx.Binding>();
                WitchVfxSetup.Configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Run();
            SessionState.SetInt(Pending + ".Start", 4);
            SessionState.SetInt(Pending + ".End", 5);
        }

        public static void RetakeWitchOnGrass()
        {
            RetakeWitch();
            SessionState.SetBool(Pending + ".Grass", true);
        }

        public static void RetakeRevisions()
        {
            EnemyVfxBuild.ConfigureAll("Golem", "Eyebeast", "Sniper_Eyebeast");
            Run();
            SessionState.SetInt(Pending + ".End", 4);
        }

        public static void RetakeGolem()
        {
            EnemyVfxBuild.ConfigureAll("Golem");
            Run();
            SessionState.SetInt(Pending + ".End", 1);
        }

        public static void RetakeDetails()
        {
            EnemyAnimationViewer viewer = UnityEngine.Object.FindFirstObjectByType<EnemyAnimationViewer>(FindObjectsInactive.Include);
            if (viewer == null) throw new InvalidOperationException("Open PJS_Scene before the detailed VFX review.");
            Camera preview = new SerializedObject(viewer).FindProperty("_previewCamera").objectReferenceValue as Camera;
            Transform marker = viewer.transform.Find("Viewer Target/Target Marker");
            var gameplay = new GameplayCamera { offset = preview.transform.position - marker.position, rotation = preview.transform.rotation,
                fov = preview.fieldOfView, orthographic = preview.orthographic };
            AnimationClip slimeDeath = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/EnemyChar/Slime/Slime_Death.anim");
            AnimationUtility.SetAnimationEvents(slimeDeath, EnemyVfxBuild.Events("Slime", "Death", slimeDeath.length, 0).ToArray());
            EnemyVfxBuild.ConfigureAll("Golem", "Big_Golem", "Sniper_Eyebeast", "Slime", "Rollbug");
            Run();
            SessionState.SetBool(Pending + ".Details", true);
            SessionState.SetString(Pending + ".GameplayCamera", JsonUtility.ToJson(gameplay));
        }

        public static void RetakeCrystalRevisions()
        {
            AnimationClip summon = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/EnemyChar/Witch/Witch_Summon.anim");
            AnimationUtility.SetAnimationEvents(summon, EnemyVfxBuild.Events("Witch", "Summon", summon.length, 33).ToArray());
            EnemyVfxBuild.ConfigureAll("Big_Golem", "Witch");
            Run();
            SessionState.SetInt(Pending + ".Start", 2);
            SessionState.SetInt(Pending + ".End", 5);
        }

        private static void ModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (SessionState.GetBool(Pending + ".Grass", false))
                {
                    Material grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Prefabs/Map/MapGroundGrass.mat");
                    if (grass == null) throw new InvalidOperationException("Grass-floor review requires the gameplay grass material.");
                    GameObject.Find("Matte Review Stage").GetComponent<Renderer>().sharedMaterial = grass;
                }
                Time.captureDeltaTime = 1f / Fps;
                Time.timeScale = 1;
                gameplayCamera = SessionState.GetBool(Pending + ".Details", false)
                    ? JsonUtility.FromJson<GameplayCamera>(SessionState.GetString(Pending + ".GameplayCamera", "")) : null;
                if (gameplayCamera != null) GameObject.Find("Matte Review Stage").transform.localScale = new Vector3(120, .2f, 120);
                report = new Report { graphicsDevice = SystemInfo.graphicsDeviceName };
                render = new RenderTexture(1280, 960, 24, RenderTextureFormat.ARGB32);
                render.Create();
                pixels = new Texture2D(1280, 960, TextureFormat.RGB24, false);
                index = SessionState.GetInt(Pending + ".Start", 0);
                string previousReport = Path.Combine(Output, "UnityCaptureReport.json");
                int end = SessionState.GetInt(Pending + ".End", EnemyVfxBuild.Names.Length);
                if ((index > 0 || end < EnemyVfxBuild.Names.Length) && File.Exists(previousReport))
                {
                    report = JsonUtility.FromJson<Report>(File.ReadAllText(previousReport));
                    report.result = "running";
                    int first = index;
                    report.enemies.RemoveAll(entry => Selected(Array.IndexOf(EnemyVfxBuild.Names, entry.enemy), first, end));
                }
                else if (SessionState.GetBool(Pending + ".Details", false) && File.Exists(previousReport))
                {
                    report = JsonUtility.FromJson<Report>(File.ReadAllText(previousReport));
                    report.result = "running";
                    report.enemies.RemoveAll(entry => Selected(Array.IndexOf(EnemyVfxBuild.Names, entry.enemy), index, end));
                }
                frame = 0; step = 0; error = null;
                deadline = EditorApplication.timeSinceStartup + 600;
                Directory.CreateDirectory(Output);
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
                string saved = SessionState.GetString(Pending + ".Scenes", "");
                if (!string.IsNullOrEmpty(saved)) EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<SavedScenes>(saved).scenes
                    .Select(scene => new SceneSetup { path = scene.path, isLoaded = scene.loaded, isActive = scene.active }).ToArray());
                if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(Pending + ".Passed", false) ? 0 : 1);
            }
        }

        private static bool Selected(int candidate, int first, int end) => candidate >= first && candidate < end
            && (!SessionState.GetBool(Pending + ".Details", false) || candidate == 0 || candidate == 2 || candidate == 3 || candidate == 6 || candidate == 7);

        private static void StartEnemy()
        {
            review.Select(index);
            started = Time.time; frame = 0; step = 0;
            sawBeam = false;
            sawSummonField = false;
            directory = Path.Combine(Output, (index + 1).ToString("00") + "_" + EnemyVfxBuild.Names[index]);
            Directory.CreateDirectory(directory);
            foreach (string oldFrame in Directory.EnumerateFiles(directory, "frame_*.png")) File.Delete(oldFrame);
            foreach (string oldFrame in Directory.EnumerateFiles(directory, "top_*.png")) File.Delete(oldFrame);
            foreach (string oldFrame in Directory.EnumerateFiles(directory, "game_*.png")) File.Delete(oldFrame);
            string preview = Path.Combine(directory, "Preview.png");
            if (File.Exists(preview)) File.Delete(preview);
        }

        private static void Tick()
        {
            if (File.Exists(DetailRequest) && !SessionState.GetBool(Pending, false) && !EditorApplication.isCompiling
                && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                bool slimeOnly = File.ReadAllText(DetailRequest).Trim() == "slime";
                File.Delete(DetailRequest);
                try
                {
                    RetakeDetails();
                    if (slimeOnly) { SessionState.SetInt(Pending + ".Start", 6); SessionState.SetInt(Pending + ".End", 7); }
                }
                catch (Exception exception)
                {
                    Directory.CreateDirectory(Output);
                    File.WriteAllText(Path.Combine(Output, "UnityCaptureReport.json"), JsonUtility.ToJson(new Report { result = "failed", error = exception.ToString() }, true));
                    Debug.LogException(exception);
                }
                return;
            }
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || report == null) return;
            if (lastUnityFrame == Time.frameCount) return;
            lastUnityFrame = Time.frameCount;
            try
            {
                if (error != null) throw new InvalidOperationException(error);
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("VFX capture did not complete in ten minutes.");
                if (review == null)
                {
                    review = UnityEngine.Object.FindFirstObjectByType<EnemyVfxReview>();
                    if (review == null || review.Target == null) return;
                    StartEnemy();
                }
                float elapsed = Time.time - started;
                float extra = index == 4 ? 2f : index == 5 ? 1.8f : 0;
                if (step == 0 && elapsed >= .6f) { Record(); review.Play("Move"); step = 1; }
                if (step == 1 && elapsed >= 2f) { Record(); review.Play("Attack"); step = 2; }
                if (step == 2 && elapsed >= 4f)
                {
                    Record();
                    if (extra > 0) { review.Play(index == 4 ? "Summon" : "Spawn"); step = 3; }
                    else { review.Play("Death"); step = 4; }
                }
                if (step == 3 && elapsed >= 4f + extra) { Record(); review.Play("Death"); step = 4; }
                if (index == 3 && UnityEngine.Object.FindObjectsByType<UJam.Runtime.Enemy.Vfx.SniperBeam>(FindObjectsSortMode.None).Length > 0) sawBeam = true;
                if (index == 4 && review.Action == "Summon" && review.ActiveEnemy != null
                    && review.ActiveEnemy.transform.Find("WitchCasterFieldVfx").childCount > 0) sawSummonField = true;
                byte[] png = RenderFrame();
                File.WriteAllBytes(Path.Combine(directory, "frame_" + frame.ToString("0000") + ".png"), png);
                if (index == 0 || index == 2 || index == 4 || index == 6)
                {
                    review.SetTopDown(true);
                    byte[] top = RenderFrame();
                    review.SetTopDown(false);
                    File.WriteAllBytes(Path.Combine(directory, "top_" + frame.ToString("0000") + ".png"), top);
                    float topPrimary = index == 4 ? 4.5f : index == 6 ? 4.2f : 2.65f;
                    if (elapsed >= topPrimary && elapsed < topPrimary + 1f / Fps * 1.5f)
                        File.WriteAllBytes(Path.Combine(directory, "TopDown.png"), top);
                }
                float primary = index == 3 ? 2.55f : index == 4 ? 4.5f : index == 6 ? 4.2f : index == 7 ? 2.25f : 2.35f;
                if (elapsed >= primary && elapsed < primary + 1f / Fps * 1.5f)
                    File.WriteAllBytes(Path.Combine(directory, "Preview.png"), png);
                if (gameplayCamera != null)
                {
                    Camera camera = review.ReviewCamera;
                    Vector3 position = camera.transform.position;
                    Quaternion rotation = camera.transform.rotation;
                    bool orthographic = camera.orthographic;
                    float fov = camera.fieldOfView;
                    Vector3 center = review.Target.GetComponentInChildren<MeshRenderer>().bounds.center;
                    camera.transform.SetPositionAndRotation(center + gameplayCamera.offset, gameplayCamera.rotation);
                    camera.orthographic = gameplayCamera.orthographic;
                    camera.fieldOfView = gameplayCamera.fov;
                    byte[] game = RenderFrame();
                    File.WriteAllBytes(Path.Combine(directory, "game_" + frame.ToString("0000") + ".png"), game);
                    if (elapsed >= primary && elapsed < primary + 1f / Fps * 1.5f)
                        File.WriteAllBytes(Path.Combine(directory, "GameView.png"), game);
                    camera.transform.SetPositionAndRotation(position, rotation);
                    camera.orthographic = orthographic;
                    camera.fieldOfView = fov;
                }
                frame++;
                float deathDuration = index == 0 || index == 2 ? 5.5f : index == 4 ? 3.6f : 2.5f;
                if (elapsed >= 4f + extra + deathDuration)
                {
                    Record();
                    if (review.ActiveEnemy != null) throw new InvalidOperationException(EnemyVfxBuild.Names[index] + " did not finish its death.");
                    index++;
                    int end = SessionState.GetInt(Pending + ".End", EnemyVfxBuild.Names.Length);
                    while (index < end && !Selected(index, 0, end)) index++;
                    if (index == SessionState.GetInt(Pending + ".End", EnemyVfxBuild.Names.Length)) Finish(null);
                    else StartEnemy();
                }
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static void Record()
        {
            report.enemies.Add(new Entry { enemy = EnemyVfxBuild.Names[index], action = review.Action,
                seconds = Time.time - started, hits = review.Target.HitCount,
                effects = review.ActiveEnemy != null ? review.ActiveEnemy.GetComponent<UJam.Runtime.Enemy.EnemyVfx>().PlayedEffects : 0 });
            if (review.Action == "Attack" && review.Target.HitCount == 0)
                throw new InvalidOperationException(EnemyVfxBuild.Names[index] + " did not deliver its actual attack.");
            if (review.Action == "Attack" && index == 3 && !sawBeam)
                throw new InvalidOperationException("Sniper attack did not keep a lightning effect alive for rendering.");
            if (review.Action == "Summon" && index == 4 && !sawSummonField)
                throw new InvalidOperationException("Witch did not render its caster-centered summon field.");
        }

        private static byte[] RenderFrame()
        {
            RenderPipeline.SubmitRenderRequest(review.ReviewCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = render });
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = render;
            pixels.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0);
            pixels.Apply(false);
            RenderTexture.active = previous;
            return pixels.EncodeToPNG();
        }

        private static void Finish(Exception exception)
        {
            report.result = exception == null ? "passed" : "failed";
            SessionState.SetBool(Pending + ".Passed", exception == null);
            report.error = exception?.ToString();
            File.WriteAllText(Path.Combine(Output, "UnityCaptureReport.json"), JsonUtility.ToJson(report, true));
            Time.captureDeltaTime = 0;
            if (render != null) { render.Release(); UnityEngine.Object.DestroyImmediate(render); }
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            if (exception != null) Debug.LogException(exception);
            EditorApplication.ExitPlaymode();
        }
    }
}
