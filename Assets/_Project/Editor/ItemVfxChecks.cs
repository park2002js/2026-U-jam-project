using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ujam.Runtime.Item;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    [InitializeOnLoad]
    public static class ItemVfxChecks
    {
        private const string Request = "Library/CheckItemVFX.request";
        private const string ReportPath = "Library/ItemVfxChecks.json";
        private const string Running = "UJam.ItemVfxChecks.Running";
        private static readonly string[] IDs = { "1", "3", "4", "5", "7", "12" };
        private static readonly string[] Names = { "FocusedFire_VFX", "Weaken_VFX", "Fog_VFX", "GravityField_VFX", "Cooling_VFX", "Gust_VFX" };
        private static readonly float[] Diameters = { 2, 8, 8, 8, 4, 8 };
        private static readonly float[] ReferenceDiameters = { 1, 4, 4, 4, 2, 2 };
        private static readonly float[] Durations = { 1, 5, 5, 5, 1, 5 };

        [Serializable]
        private sealed class Observation
        {
            public string id, prefab;
            public float diameter, referenceDiameter, observedDiameter, duration, observedLifetime;
            public bool effectApplied, outsideUnaffected, cleanedOnUnequip;
            public int impactParticlePeak;
        }

        [Serializable]
        private sealed class Report
        {
            public string result, error;
            public List<Observation> items = new List<Observation>();
        }

        private static Report report;
        private static bool previousRunInBackground;

        static ItemVfxChecks()
        {
            EditorApplication.update += RequestedRun;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Tools/Items/Check Item VFX Assets")]
        public static void Run()
        {
            Require(ItemCatalog.IDs.Count() == 30, "All thirty items must remain available.");
            for (int id = 1; id <= 30; id++)
            {
                string icon = "Item_" + id.ToString("D2");
                Sprite sprite = Resources.Load<Sprite>("Art/Item/" + icon);
                Require(sprite != null && sprite.texture.width == 128 && sprite.texture.height == 128,
                    icon + " must be a 128x128 item Sprite.");
                Require(ItemCatalog.GetMeta(id.ToString()).ItemSprite == sprite,
                    "Catalog is still using a fallback icon for item " + id);
            }
            for (int i = 0; i < IDs.Length; i++)
            {
                ItemMeta meta = ItemCatalog.GetMeta(IDs[i]);
                Require(meta.Preview.Type == ItemPreviewType.Circle && Near(meta.Preview.Scale.x, Diameters[i]) &&
                        Near(meta.Preview.Scale.y, Diameters[i]), "Item " + IDs[i] + " Preview does not match its diameter.");
                Require(meta.VfxPath == "ItemFX/" + Names[i], "Item " + IDs[i] + " VFX mapping is incorrect.");
                GameObject prefab = Resources.Load<GameObject>(meta.VfxPath);
                Require(prefab != null, "Resources/" + meta.VfxPath + " is missing.");
                ItemVfx visual = prefab.GetComponent<ItemVfx>();
                Require(visual != null && Near(visual.ReferenceDiameter, ReferenceDiameters[i]), Names[i] + " calibration is missing.");
                Require(prefab.GetComponentsInChildren<ParticleSystem>(true).Length > 0, Names[i] + " has no particle systems.");
            }
            Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/ItemEffects/Item_001_HorizontalCylinder.prefab") == null,
                "The retired HorizontalCylinder prefab still exists.");
            Debug.Log("[ItemVfxChecks] Thirty item icons, six Circle previews, VFX resource mappings and reference diameters passed.");
        }

        private static void RequestedRun()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try
            {
                Run();
                SkillPreviewChecks.Run();
                SessionState.SetBool(Running, true);
                EditorApplication.isPlaying = true;
            }
            catch (Exception error) { Finish(error); }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Running, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    report = new Report { result = "running" };
                    File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                    var timer = RuntimeTimer.Ensure();
                    foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                        if (root != timer.transform.root.gameObject) root.SetActive(false);
                    foreach (Transform child in timer.transform) child.gameObject.SetActive(false);
                    foreach (var behaviour in timer.GetComponents<MonoBehaviour>())
                        if (behaviour != timer) behaviour.enabled = false;
                    Scene scene = SceneManager.CreateScene("Item VFX Runtime Checks");
                    SceneManager.SetActiveScene(scene);
                    previousRunInBackground = Application.runInBackground;
                    Application.runInBackground = true;
                    Time.timeScale = 1;
                    EditorApplication.isPaused = false;
                    timer.StartCoroutine(Guard(CheckRuntime()));
                }
                catch (Exception error) { Finish(error); }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
                Finish(new InvalidOperationException("Play mode stopped before Item VFX checks finished."));
        }

        private static IEnumerator Guard(IEnumerator checks)
        {
            while (true)
            {
                bool more;
                try { more = checks.MoveNext(); }
                catch (Exception error) { Finish(error); yield break; }
                if (!more) break;
                yield return checks.Current;
            }
            Finish(null);
        }

        private static IEnumerator CheckRuntime()
        {
            var gridHost = new GameObject("ItemVFX Grid");
            gridHost.SetActive(false);
            var grid = gridHost.AddComponent<GridSystem>();
            grid.CellWidth = grid.CellHeight = 2;
            grid.RowCount = grid.ColumnCount = 32;
            typeof(GridSystem).GetProperty(nameof(GridSystem.Instance)).SetValue(null, null);
            gridHost.SetActive(true);
            Require(grid.IsInitialized && Near(ItemWorld.CellWidth, 2), "Runtime test grid failed to initialize.");
            var ground = new GameObject("ItemVFX Ground");
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.position = new Vector3(50, -.5f, 0);
            ground.AddComponent<BoxCollider>().size = new Vector3(140, 1, 20);
            var player = new GameObject("ItemVFX Player").AddComponent<PlayerStatus>();
            for (int i = 0; i < IDs.Length; i++)
            {
                string id = IDs[i];
                Vector3 point = new Vector3(i * 20, 0, 0);
                MeleeEnemy inside = Enemy(point);
                MeleeEnemy outside = Enemy(point + Vector3.right * (Diameters[i] / 2 + 2));
                float hp = inside.Status.HP;
                float outsideHp = outside.Status.HP;
                Item item = ItemCatalog.Create(id);
                item.Equip(player, 1 << LayerMask.NameToLayer("Enemy"));
                Cast(item, player, point);
                float deadline = Time.time + 5;
                ItemVfx visual;
                while ((visual = Visual(item)) == null && Time.time < deadline) yield return null;
                Require(visual != null, "Item " + id + " did not spawn its VFX.");
                float born = Time.time;
                var entry = new Observation
                {
                    id = id, prefab = item.Meta.VfxPath, diameter = Diameters[i],
                    referenceDiameter = visual.ReferenceDiameter, duration = Durations[i],
                    observedDiameter = visual.transform.lossyScale.x * visual.ReferenceDiameter
                };
                report.items.Add(entry);
                Require(Near(entry.observedDiameter, entry.diameter) && Near(visual.transform.lossyScale.x, visual.transform.lossyScale.z),
                    "Item " + id + " VFX world diameter differs from its Preview or was scaled twice by its parent.");
                Require(visual.GetComponentsInChildren<ParticleSystem>(true).All(p => p.main.scalingMode == ParticleSystemScalingMode.Hierarchy),
                    "Item " + id + " particles do not follow the VFX root scale.");
                yield return new WaitForSeconds(.1f);
                CheckEffect(id, inside, player, hp, true);
                CheckEffect(id, outside, player, outsideHp, false);
                entry.effectApplied = entry.outsideUnaffected = true;
                deadline = born + entry.duration + 1;
                while (visual != null && Time.time < deadline)
                {
                    if (id == "1") entry.impactParticlePeak = Math.Max(entry.impactParticlePeak,
                        visual.GetComponentsInChildren<ParticleSystem>(true)
                            .Where(p => p.name == "Sparks" || p.name == "Glow" || p.name == "Stones" || p.name == "Flash")
                            .Sum(p => p.particleCount));
                    yield return null;
                }
                entry.observedLifetime = Time.time - born;
                Require(visual == null && Mathf.Abs(entry.observedLifetime - entry.duration) < .7f,
                    "Item " + id + " VFX did not expire at its configured duration.");
                if (id == "1") Require(entry.impactParticlePeak > 0, "FocusedFire ground impact sub-emitters produced no particles.");
                yield return new WaitForSeconds(.1f);
                if (id == "3" || id == "4" || id == "5")
                    Require(Near(BuffManager.Instance.Percent(inside.Status, Stat(id)), 0), "Item " + id + " left a field buff after expiry.");

                Cast(item, player, point);
                deadline = Time.time + 5;
                while ((visual = Visual(item)) == null && Time.time < deadline) yield return null;
                Require(visual != null, "Item " + id + " did not respawn its VFX.");
                item.Unequip();
                yield return null;
                Require(visual == null && Visual(item) == null, "Item " + id + " VFX survived Unequip.");
                entry.cleanedOnUnequip = true;
                Debug.Log("[ItemVfxChecks] Item " + id + " passed: diameter " + entry.observedDiameter +
                    ", duration " + entry.observedLifetime.ToString("F2") + "s, effect/range and Unequip cleanup.");
                Object.Destroy(inside.gameObject);
                Object.Destroy(outside.gameObject);
                yield return null;
            }
        }

        private static MeleeEnemy Enemy(Vector3 position)
        {
            var host = new GameObject("ItemVFX Target");
            host.SetActive(false);
            host.layer = LayerMask.NameToLayer("Enemy");
            host.transform.position = position;
            host.AddComponent<EnemyStatus>().ConfigureSpawnStats(1000, 100, 0);
            host.AddComponent<SphereCollider>().radius = .25f;
            var enemy = host.AddComponent<MeleeEnemy>();
            host.SetActive(true);
            enemy.PrepareForTesting(null);
            return enemy;
        }

        private static void Cast(Item item, PlayerStatus player, Vector3 position) =>
            item.Runtime.Execute(new ItemEvent(ItemTrigger.SkillUse, player, position, skillItem: item, cast: new SkillCast()));

        private static ItemVfx Visual(Item item) =>
            ((List<GameObject>)typeof(ItemRuntime).GetField("objects", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item.Runtime))
                .Where(go => go != null).SelectMany(go => go.GetComponentsInChildren<ItemVfx>(true)).FirstOrDefault();

        private static BuffStat Stat(string id) => id == "3" ? BuffStat.AttackDamage : id == "4" ? BuffStat.DamageTaken : BuffStat.MovementSpeed;

        private static void CheckEffect(string id, MeleeEnemy enemy, PlayerStatus player, float hp, bool inside)
        {
            if (id == "1") Require(Near(hp - enemy.Status.HP, inside ? player.AttackDamage * 5 : 0), "FocusedFire damage/range changed.");
            else if (id == "3" || id == "4" || id == "5")
                Require(Near(BuffManager.Instance.Percent(enemy.Status, Stat(id)), inside ? (id == "3" ? -10 : id == "4" ? 20 : -20) : 0),
                    "Item " + id + " field effect/range changed.");
            else
            {
                bool applied = ElementManager.Instance.TryGet(enemy, out ElementState state);
                Require(applied == inside && (!inside || state.Type == (id == "7" ? ElementType.Freeze : ElementType.Wind)),
                    "Item " + id + " element/range changed.");
                if (id == "7") Require(enemy.Status.IsStunned == inside, "Cooling stun/range changed.");
            }
        }

        private static void Finish(Exception error)
        {
            if (report == null) report = new Report();
            report.result = error == null ? "passed" : "failed";
            report.error = error?.ToString() ?? "";
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            SessionState.SetBool(Running, false);
            if (EditorApplication.isPlaying) Application.runInBackground = previousRunInBackground;
            if (error == null) Debug.Log("[ItemVfxChecks] PASS: all six item VFX sizes, lifetimes, effects, ranges and cleanup.");
            else Debug.LogException(error);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < .01f;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
