using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ujam.Runtime.Item;
using UJam.Runtime.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    [InitializeOnLoad]
    public static class SfxChecks
    {
        private const string Request = "Library/CheckSFX.request";
        private const string ReportPath = "Library/SfxChecks.json";
        private const string Running = "UJam.SfxChecks.Running";
        private const string CreatedFixture = "UJam.SfxChecks.CreatedFixture";
        private const string FixtureRoot = "Assets/__SfxChecks";
        private const string MainScene = "Assets/_Project/Scenes/_MainScene.unity";
        private static readonly FieldInfo ClipsField = typeof(ItemEffect).GetField("clips", BindingFlags.Instance | BindingFlags.NonPublic);
        private static IEnumerator routine;
        private static Report report;
        private static GameObject fixture;
        private static AudioClip tone;
        private static double deadline;
        private static bool previousBackground, previousPause;
        private static float previousVolume, previousTimeScale;
        private static bool settingsCaptured;

        [Serializable] private sealed class Report
        {
            public string result, error, overlapMeasurement;
            public float singleRms, doubleRms;
            public List<string> checks = new();
        }

        private sealed class ProbeEffect : ItemEffect
        {
            public ProbeEffect(string id) : base(id) { }
            public AudioClip[] Clips => clips;
            public AudioClip Find(string name) => GetClip(name);
            public override void Execute(ItemUseContext context) { }
        }

        static SfxChecks()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += PlayChanged;
        }

        [MenuItem("Tools/Audio/Check SFX")]
        public static void RunBatch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run SFX checks in Edit mode.");
            report = new Report { result = "starting" };
            try
            {
                CheckSceneAndItems();
                Require(!Directory.Exists(FixtureRoot), FixtureRoot + " already exists; refusing to overwrite it.");
                SessionState.SetBool(CreatedFixture, true);
                WriteWav(FixtureRoot + "/Resources/Audio/Item/__SfxChecks_Single/Only.wav");
                WriteWav(FixtureRoot + "/Resources/Audio/Item/__SfxChecks_Multi/Cast.wav");
                WriteWav(FixtureRoot + "/Resources/Audio/Item/__SfxChecks_Multi/Impact.wav");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                WriteReport();
                SessionState.SetBool(Running, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Finish(error); }
        }

        private static void CheckSceneAndItems()
        {
            Require(ClipsField != null, "ItemEffect must retain its loaded clips.");
            Require(ItemCatalog.IDs.Count() == 30, "Expected all 30 item definitions.");
            foreach (string id in ItemCatalog.IDs)
            {
                Item first = ItemCatalog.Create(id), second = ItemCatalog.Create(id);
                AudioClip[] expected = Resources.LoadAll<AudioClip>("Audio/Item/" + id);
                AudioClip[] actual = (AudioClip[])ClipsField.GetValue(first.Runtime.Effect);
                Require(expected.Length == 0 ? actual == null : actual != null && expected.Length == actual.Length && expected.All(actual.Contains),
                    id + ": constructor clips differ from its Resources folder, or an empty folder did not leave clips null.");
                Require(ReferenceEquals(first.Runtime.Effect, second.Runtime.Effect), id + ": item copies must share Effect clip data.");
            }
            Passed("All 30 item definitions retain expected clips (null when absent); item copies share their Effect.");

            Scene scene = SceneManager.GetSceneByPath(MainScene);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Additive);
            try
            {
                SFXManager[] managers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SFXManager>(true)).ToArray();
                Require(managers.Length == 1, "_MainScene must contain exactly one SFXManager.");
                Require(managers[0].GetComponentsInParent<Transform>(true).Any(parent => parent.name == "PlayerTurret"),
                    "The main-scene SFXManager must belong to PlayerTurret.");
                Require(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AudioListener>(true)).Count() == 1,
                    "The main scene must retain one AudioListener.");
                Passed("_MainScene has one SFXManager under PlayerTurret and one AudioListener.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void Update()
        {
            if (routine != null)
            {
                try
                {
                    if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("SFX checks exceeded 30 seconds.");
                    if (!routine.MoveNext()) Finish(null);
                }
                catch (Exception error) { Finish(error); }
                return;
            }
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            RunBatch();
        }

        private static void PlayChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Running, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                report = JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath));
                report.result = "running";
                deadline = EditorApplication.timeSinceStartup + 30;
                routine = CheckRuntime();
            }
            else if (state == PlayModeStateChange.EnteredEditMode) Finish(new InvalidOperationException("SFX checks stopped before completion."));
        }

        private static IEnumerator CheckRuntime()
        {
            previousBackground = Application.runInBackground;
            previousPause = AudioListener.pause;
            previousVolume = AudioListener.volume;
            previousTimeScale = Time.timeScale;
            settingsCaptured = true;
            Application.runInBackground = true;
            SFXManager[] previousManagers = Resources.FindObjectsOfTypeAll<SFXManager>()
                .Where(manager => manager.gameObject.scene.IsValid()).ToArray();
            foreach (SFXManager manager in previousManagers) Object.Destroy(manager);
            while (previousManagers.Any(manager => manager != null) || SFXManager.Instance != null) yield return null;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                foreach (GameObject root in SceneManager.GetSceneAt(i).GetRootGameObjects()) root.SetActive(false);
            SceneManager.SetActiveScene(SceneManager.CreateScene("SFX Checks"));
            Time.timeScale = 1;
            AudioListener.pause = false;
            AudioListener.volume = 1;
            fixture = new GameObject("SFX Check Player", typeof(AudioListener));
            SFXManager sfx = fixture.AddComponent<SFXManager>();
            Require(SFXManager.Instance == sfx, "SFXManager did not register its public Instance: current=" +
                (SFXManager.Instance != null ? SFXManager.Instance.name : "<null>") + ", fixtureActive=" +
                fixture.activeInHierarchy + ", isPlaying=" + Application.isPlaying + ".");
            AudioSource source = fixture.GetComponent<AudioSource>();
            Require(source != null && source.spatialBlend == 0 && !source.playOnAwake, "Play requires an inactive-on-awake 2D source.");

            var single = new ProbeEffect("__SfxChecks_Single");
            var multi = new ProbeEffect("__SfxChecks_Multi");
            var missing = new ProbeEffect("__SfxChecks_Missing");
            Require(single.Clips?.Length == 1 && single.Find("Only") != null, "Single-clip constructor loading failed.");
            Require(multi.Clips?.Length == 2 && multi.Find("Cast") != null && multi.Find("Impact") != null,
                "Multiple named clips were not retained and retrievable.");
            Require(missing.Clips == null && missing.Find("NoClip") == null && multi.Find("NoClip") == null,
                "Missing folders or names must return null safely.");
            Passed("Constructor loading handles one clip, multiple arbitrary names and missing folders/names.");

            var warnings = new List<string>();
            void Capture(string message, string trace, LogType type) { if (message.Contains("null")) warnings.Add(message); }
            int before = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length;
            Application.logMessageReceived += Capture;
            try { sfx.Play(null); sfx.PlayAt(null, Vector3.one); }
            finally { Application.logMessageReceived -= Capture; }
            Require(warnings.Count == 2 && warnings.All(message => message.Contains(nameof(SfxChecks)) && message.Contains(nameof(CheckRuntime))),
                "Null clips must log their originating caller for both APIs.");
            Require(before == Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length && !source.isPlaying,
                "Null requests allocated sources or started playback.");
            Passed("Null requests log caller metadata and allocate/play nothing.");

            tone = AudioClip.Create("SFX overlap probe", 44100 * 3, 1, 44100, false);
            var samples = new float[tone.samples];
            for (int i = 0; i < samples.Length; i++) samples[i] = .02f * Mathf.Sin(2 * Mathf.PI * 440 * i / 44100);
            tone.SetData(samples, 0);
            sfx.Play(tone);
            double until = EditorApplication.timeSinceStartup + 1;
            float baseline = Rms(source);
            while (baseline <= .00001f && EditorApplication.timeSinceStartup < until)
            {
                yield return null;
                baseline = Rms(source);
            }
            // GetOutputData can start its capture buffer on the first call; let a full buffer settle.
            until = EditorApplication.timeSinceStartup + .2;
            while (EditorApplication.timeSinceStartup < until) { Rms(source); yield return null; }
            report.singleRms = Rms(source);
            source.Stop();
            sfx.Play(tone); sfx.Play(tone);
            Rms(source);
            until = EditorApplication.timeSinceStartup + .3;
            while (EditorApplication.timeSinceStartup < until) { Rms(source); yield return null; }
            report.doubleRms = Rms(source);
            if (report.singleRms > .00001f)
            {
                float ratio = report.doubleRms / report.singleRms;
                Require(ratio > 1.7f && ratio < 2.3f, "Repeated identical Play calls did not overlap: RMS ratio=" + ratio);
                report.overlapMeasurement = "measured: two identical 2D calls sum to " + ratio.ToString("F2") + " times one call";
                Passed("Mixer output confirms two identical rapid Play calls overlap without replacement.");
            }
            else
            {
                Require(report.doubleRms <= .00001f,
                    "The single-call capture stayed empty while double-call output was audible; overlap measurement did not establish a baseline.");
                report.overlapMeasurement = "unavailable: this editor/audio environment returned zero mixer samples";
                Passed("2D calls completed; acoustic overlap could not be measured in this audio environment.");
            }
            source.Stop();

            var beforeShort = new HashSet<AudioSource>(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None));
            Time.timeScale = 0;
            sfx.PlayAt(single.Find("Only"), Vector3.zero);
            AudioSource shortVoice = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(value => !beforeShort.Contains(value));
            until = EditorApplication.timeSinceStartup + 2;
            while (shortVoice != null && EditorApplication.timeSinceStartup < until) yield return null;
            Require(shortVoice == null, "A completed short PlayAt voice was not cleaned up while Time.timeScale was zero.");
            Time.timeScale = 1;
            Passed("A completed short positional sound releases its source even while game time is paused.");

            var oldSources = new HashSet<AudioSource>(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None));
            Vector3 firstPosition = new Vector3(1, 0, 2), secondPosition = new Vector3(-3, 0, 4);
            sfx.PlayAt(tone, firstPosition); sfx.PlayAt(tone, secondPosition); sfx.PlayAt(tone, firstPosition);
            AudioSource[] spawned = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(value => !oldSources.Contains(value)).ToArray();
            Require(spawned.Length == 3 && spawned.Count(value => value.transform.position == firstPosition) == 2 &&
                spawned.Count(value => value.transform.position == secondPosition) == 1 && spawned.All(value => value.spatialBlend == 1 && value.clip == tone),
                "Repeated identical PlayAt calls require separate sources at their requested positions.");
            fixture.transform.position = Vector3.one * 20;
            Require(spawned.Count(value => value.transform.position == firstPosition) == 2 && spawned.Count(value => value.transform.position == secondPosition) == 1,
                "Moving PlayerTurret moved already playing world sounds.");
            Passed("Three identical PlayAt requests retain independent positions, including duplicate clips and locations.");
            Object.Destroy(fixture);
            while (fixture != null || SFXManager.Instance != null || spawned.Any(value => value != null)) yield return null;
            Require(SFXManager.Instance == null && spawned.All(value => value == null), "Manager destruction retained Instance or world sound sources.");
            fixture = new GameObject("Replacement SFX Manager");
            sfx = fixture.AddComponent<SFXManager>();
            Require(SFXManager.Instance == sfx, "A destroyed manager prevented replacement.");
            Passed("Manager destruction clears its singleton and positional voices; a replacement can register.");
        }

        private static float Rms(AudioSource source)
        {
            var output = new float[4096];
            source.GetOutputData(output, 0);
            return Mathf.Sqrt(output.Sum(sample => sample * sample) / output.Length);
        }

        private static void WriteWav(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            const int sampleCount = 2205;
            using var writer = new BinaryWriter(File.Create(path));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + sampleCount * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(22050); writer.Write(44100);
            writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(sampleCount * 2);
            for (int i = 0; i < sampleCount; i++) writer.Write((short)0);
        }

        private static void Passed(string text) { report.checks.Add(text); WriteReport(); }
        private static void WriteReport() => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Finish(Exception error)
        {
            routine = null;
            SessionState.SetBool(Running, false);
            if (fixture != null) Object.DestroyImmediate(fixture);
            if (tone != null) Object.DestroyImmediate(tone);
            if (EditorApplication.isPlaying && settingsCaptured)
            {
                Application.runInBackground = previousBackground;
                AudioListener.pause = previousPause;
                AudioListener.volume = previousVolume;
                Time.timeScale = previousTimeScale;
            }
            settingsCaptured = false;
            if (SessionState.GetBool(CreatedFixture, false)) { AssetDatabase.DeleteAsset(FixtureRoot); SessionState.SetBool(CreatedFixture, false); }
            report ??= new Report(); report.result = error == null ? "passed" : "failed"; report.error = error?.ToString() ?? ""; WriteReport();
            if (error == null) Debug.Log("[SfxChecks] PASS: " + report.overlapMeasurement);
            else Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}
