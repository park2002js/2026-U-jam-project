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
    public static class ItemBombardmentChecks
    {
        private const string Request = "Library/CheckItemBombardment.request";
        private const string ReportPath = "Library/ItemBombardmentChecks.json";
        private const string Running = "UJam.ItemBombardmentChecks.Running";
        private static readonly List<Item> equipped = new();
        private static readonly object frozen = new();
        private static Report report;
        private static double deadline;
        private static bool previousBackground;
        private static Camera viewCamera;

        [Serializable] private sealed class Observation
        {
            public string item, scenario;
            public float impactAt, horizontalFirstAt = -1, verticalFirstAt = -1;
            public int bombs, impacts;
            public bool descended, damageCorrect, outsideUnaffected, cleanedOnUnequip;
        }
        [Serializable] private sealed class Report
        {
            public string result, error;
            public List<Observation> cases = new();
        }

        static ItemBombardmentChecks()
        {
            EditorApplication.update += RequestedRun;
            EditorApplication.playModeStateChanged += PlayChanged;
        }

        [MenuItem("Tools/Items/Check Bombardment VFX Assets")]
        public static void CheckAssets()
        {
            foreach (string id in new[] { "8", "9" })
            {
                string path = ItemCatalog.GetMeta(id).VfxPath;
                Require(!string.IsNullOrWhiteSpace(path), id + " has no top-level VFX path.");
                var root = Resources.Load<GameObject>(path);
                Require(root != null && root.GetComponent<ItemBombardmentVfx>() != null,
                    id + " requires one replaceable ItemBombardmentVfx prefab.");
                var settings = new SerializedObject(root.GetComponent<ItemBombardmentVfx>());
                foreach (string field in new[] { "bombPrefab", "impactPrefab" })
                {
                    var stage = settings.FindProperty(field).objectReferenceValue as GameObject;
                    Require(stage != null && PrefabUtility.IsPartOfPrefabAsset(stage), id + "/" + field + " is not a stage prefab.");
                    Require(AssetDatabase.GetAssetPath(stage).StartsWith("Assets/_Project/Prefabs/VFX/ItemFX/", StringComparison.Ordinal),
                        id + "/" + field + " must reference a project-owned copy.");
                    Require(stage.GetComponentsInChildren<Renderer>(true).Length > 0, id + "/" + field + " has no visible renderer.");
                    foreach (Transform child in stage.GetComponentsInChildren<Transform>(true))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                            id + "/" + field + " contains a missing script.");
                    foreach (var renderer in stage.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    {
                        Require(renderer.sharedMaterial != null && !ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader),
                            id + "/" + renderer.name + " has a missing or broken particle material.");
                        if (renderer.renderMode != ParticleSystemRenderMode.Mesh || !renderer.enableGPUInstancing ||
                            !renderer.supportsMeshInstancing || !ShaderUtil.HasProceduralInstancing(renderer.sharedMaterial.shader)) continue;
                        var streams = new List<ParticleSystemVertexStream>();
                        renderer.GetActiveVertexStreams(streams);
                        Require(streams.Contains(ParticleSystemVertexStream.AnimFrame),
                            id + "/" + renderer.name + " requires AnimFrame for the URP instanced particle layout.");
                    }
                }
            }
        }

        private static void RequestedRun()
        {
            if (SessionState.GetBool(Running, false) && deadline > 0 && EditorApplication.timeSinceStartup > deadline)
            { Finish(new TimeoutException("Bombardment checks exceeded 60 seconds.")); return; }
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            report = new Report { result = "starting" };
            try
            {
                CheckAssets();
                SessionState.SetBool(Running, true);
                EditorApplication.isPlaying = true;
            }
            catch (Exception error) { Finish(error); }
        }

        private static void PlayChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Running, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    report = new Report { result = "running" };
                    WriteReport();
                    var timer = RuntimeTimer.Ensure();
                    foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                        if (root != timer.transform.root.gameObject) root.SetActive(false);
                    foreach (Transform child in timer.transform) child.gameObject.SetActive(false);
                    foreach (var behaviour in timer.GetComponents<MonoBehaviour>())
                        if (behaviour != timer) behaviour.enabled = false;
                    SceneManager.SetActiveScene(SceneManager.CreateScene("Bombardment Runtime Checks"));
                    previousBackground = Application.runInBackground;
                    Application.runInBackground = true;
                    Time.timeScale = 1;
                    EditorApplication.isPaused = false;
                    deadline = EditorApplication.timeSinceStartup + 60;
                    timer.StartCoroutine(Guard(CheckRuntime()));
                }
                catch (Exception error) { Finish(error); }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
                Finish(new InvalidOperationException("Play mode stopped before bombardment checks finished."));
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
            var gridHost = new GameObject("Bombardment Grid");
            gridHost.SetActive(false);
            var grid = gridHost.AddComponent<GridSystem>();
            grid.CellWidth = grid.CellHeight = 2;
            grid.ColumnCount = 16; grid.RowCount = 24;
            grid.Origin = new Vector3(-15, 0, -23);
            typeof(GridSystem).GetProperty(nameof(GridSystem.Instance)).SetValue(null, null);
            gridHost.SetActive(true);
            Require(grid.IsInitialized, "Test GridSystem did not initialize.");
            viewCamera = new GameObject("PJS Bombardment Camera").AddComponent<Camera>();
            viewCamera.tag = "MainCamera";
            viewCamera.transform.position = new Vector3(0, 30, 17.320508f);
            viewCamera.transform.rotation = new Quaternion(0, -.8660254f, .5f, 0);
            var player = new GameObject("Bombardment Player").AddComponent<PlayerStatus>();
            foreach (string id in new[] { "8", "9" })
                yield return ItemAssets.LoadAsync(ItemCatalog.GetMeta(id).VfxPath, _ => { });

            yield return CheckStrike(ItemCatalog.Create("8"), player, new Vector3(5, 0, -10),
                "catalog full-field horizontal", false, 32, 6, Vector3.forward, 2, 150, 1, 1, 1.5f);
            yield return CheckStrike(ItemCatalog.Create("9"), player, new Vector3(2, 0, 10),
                "catalog finite cross", true, 20, 6, Vector3.forward, 2, 120, 1, 1, 1.5f);
            var custom = new Item("9", new ItemMeta("Bombardment Check", "", 0, 0, 1, ItemKind.Active,
                ItemTarget.Enemies, preview: new ItemPreview(ItemPreviewType.None, direction: Vector3.right),
                vfxPath: ItemCatalog.GetMeta("9").VfxPath),
                new ItemRuntime(ItemTrigger.SkillUse, new CrossBombardmentEffect(thickness: 2, delay: .9f, damagePercent: 120,
                    length: 6, fallDuration: .2f, sweepDuration: .15f, incomingSpeed: 1.6f, impactSpeed: 1.3f, impactLifetime: .45f, spawnHeight: 6)));
            Require(Vector3.Dot(custom.Meta.Preview.Direction, Vector3.right) > .999f, "None discarded configured bombardment direction.");
            yield return CheckStrike(custom, player, new Vector3(0, 0, 30),
                "custom size, rotated direction, speed and lifetime", true, 12, 4, Vector3.right, .9f, 120, 1.6f, 1.3f, .45f);
            yield return CheckCancellation(player);
        }

        private static IEnumerator CheckStrike(Item item, PlayerStatus player, Vector3 cursor, string scenario,
            bool cross, float length, float width, Vector3 direction, float delay, float percent, float incomingSpeed, float impactSpeed, float lifetime)
        {
            var entry = new Observation { item = item.ID, scenario = scenario };
            report.cases.Add(entry);
            Quaternion rotation = Quaternion.LookRotation(direction);
            float horizontalSign = Vector3.Dot(rotation * Vector3.right, viewCamera.transform.right) < -.001f ? -1 : 1;
            float verticalSign = Vector3.Dot(rotation * Vector3.forward, viewCamera.transform.up) < -.001f ? -1 : 1;
            Vector3 center = cross ? cursor : new Vector3(0, cursor.y, cursor.z);
            MeleeEnemy middle = Enemy(center), horizontal = Enemy(center + rotation * (Vector3.right * length * .35f));
            MeleeEnemy vertical = Enemy(center + rotation * (Vector3.forward * length * .35f));
            MeleeEnemy outside = Enemy(center + rotation * new Vector3(length * .6f, 0, width + 2));
            middle.gameObject.AddComponent<BoxCollider>().size = Vector3.one * .3f;
            var targets = new[] { middle, horizontal, vertical, outside };
            float[] hp = targets.Select(enemy => enemy.Status.HP).ToArray();
            Equip(item, player);
            float castAt = Time.time;
            item.Runtime.Execute(new ItemEvent(ItemTrigger.SkillUse, player, cursor, skillItem: item, cast: new SkillCast()));
            float limit = Time.time + delay + 3;
            ItemBombardmentVfx visual;
            while ((visual = Visual(item)) == null && Time.time < limit) yield return null;
            Require(visual != null, item.ID + " did not create its top-level VFX.");
            Require(visual.Areas.Length == (cross ? 2 : 1), item.ID + " has the wrong number of bombardment lines.");
            ItemBombardmentArea first = visual.Areas[0];
            Require(Vector3.Distance(first.Center, center) < .05f && Near(first.Size.x, length) && Near(first.Size.z, width),
                item.ID + " horizontal VFX geometry does not match actual world range.");
            Require(Quaternion.Angle(first.Rotation, rotation) < .05f, item.ID + " VFX ignored configured heading.");
            if (cross)
            {
                ItemBombardmentArea second = visual.Areas[1];
                Require(Vector3.Distance(second.Center, center) < .05f && Near(second.Size.x, width) && Near(second.Size.z, length),
                    item.ID + " vertical VFX geometry does not match actual world range.");
            }
            var seen = new HashSet<GameObject>();
            var previousY = new Dictionary<GameObject, float>();
            float lastHorizontal = float.NegativeInfinity, lastVertical = float.PositiveInfinity;
            while (visual != null && visual.Impacts.Count == 0 && Time.time < limit)
            {
                var newHorizontal = new List<float>();
                var newVertical = new List<float>();
                for (int i = 0; i < targets.Length; i++)
                    Require(Near(targets[i].Status.HP, hp[i]), item.ID + " dealt damage before its simultaneous blast.");
                foreach (GameObject bomb in visual.Bombs.Where(go => go != null))
                {
                    float y = bomb.transform.position.y;
                    if (previousY.TryGetValue(bomb, out float previous)) entry.descended |= y < previous - .001f;
                    previousY[bomb] = y;
                    if (!seen.Add(bomb)) continue;
                    Vector3 local = Quaternion.Inverse(rotation) * (bomb.transform.position - center);
                    if (Mathf.Abs(local.z) <= width / 2 + .1f && (!cross || Mathf.Abs(local.x) > width / 2 + .1f))
                    {
                        float horizontalProgress = local.x * horizontalSign;
                        if (entry.horizontalFirstAt < 0)
                        {
                            entry.horizontalFirstAt = Time.time - castAt;
                            Require(horizontalProgress < 0, item.ID + " horizontal bombs did not start at the camera's left side.");
                        }
                        newHorizontal.Add(horizontalProgress);
                    }
                    else if (cross && Mathf.Abs(local.x) <= width / 2 + .1f && Mathf.Abs(local.z) > width / 2 + .1f)
                    {
                        float verticalProgress = local.z * verticalSign;
                        if (entry.verticalFirstAt < 0)
                        {
                            entry.verticalFirstAt = Time.time - castAt;
                            Require(verticalProgress > 0, item.ID + " vertical bombs did not start at the camera's top side.");
                        }
                        newVertical.Add(verticalProgress);
                    }
                    CheckSpeed(item, "bombPrefab", bomb, incomingSpeed);
                }
                if (newHorizontal.Count > 0)
                {
                    Require(newHorizontal.Min() >= lastHorizontal - .1f, item.ID + " horizontal bombs did not sweep left to right.");
                    lastHorizontal = newHorizontal.Max();
                }
                if (newVertical.Count > 0)
                {
                    Require(newVertical.Max() <= lastVertical + .1f, item.ID + " vertical bombs did not sweep top to bottom.");
                    lastVertical = newVertical.Min();
                }
                yield return null;
            }
            Require(visual != null && visual.Impacts.Count > 0, item.ID + " did not explode.");
            entry.impactAt = Time.time - castAt;
            entry.bombs = seen.Count; entry.impacts = visual.Impacts.Count;
            Require(Mathf.Abs(entry.impactAt - delay) < .2f, item.ID + " did not impact at configured T.");
            Require(entry.descended && entry.horizontalFirstAt >= 0, item.ID + " lacks a falling horizontal sweep.");
            if (cross) Require(entry.verticalFirstAt >= 0 && Mathf.Abs(entry.horizontalFirstAt - entry.verticalFirstAt) < .1f,
                item.ID + " cross sweeps did not begin concurrently.");
            Require(entry.bombs > 2 && entry.impacts == entry.bombs && Time.frameCount - visual.ImpactFrame <= 1,
                item.ID + " bombs did not all explode together.");
            CheckSpeed(item, "impactPrefab", visual.Impacts[0], impactSpeed);
            yield return null;
            float damage = player.AttackDamage * percent / 100;
            Require(Near(hp[0] - middle.Status.HP, damage * (cross ? 2 : 1)), item.ID + " center damage/duplicate Collider handling failed.");
            Require(Near(hp[1] - horizontal.Status.HP, damage), item.ID + " horizontal-only target damage failed.");
            Require(Near(hp[2] - vertical.Status.HP, cross ? damage : 0), item.ID + " vertical target damage failed.");
            Require(Near(outside.Status.HP, hp[3]), item.ID + " damaged an enemy outside its boxes.");
            entry.damageCorrect = entry.outsideUnaffected = true;
            float impactedAt = castAt + entry.impactAt;
            while (visual != null && Time.time - impactedAt < lifetime + .5f) yield return null;
            Require(visual == null && Mathf.Abs(Time.time - impactedAt - lifetime) < .2f,
                item.ID + " VFX lifetime did not follow Effect settings.");
            item.Unequip();
            yield return null;
            Require(Objects(item).All(go => go == null), item.ID + " left tracked objects after Unequip.");
            entry.cleanedOnUnequip = true;
            foreach (EnemyBase enemy in targets) Object.Destroy(enemy.gameObject);
            WriteReport();
        }

        private static IEnumerator CheckCancellation(PlayerStatus player)
        {
            Item item = ItemCatalog.Create("8");
            var entry = new Observation { item = "8", scenario = "cancel during incoming phase" };
            report.cases.Add(entry);
            var target = Enemy(new Vector3(0, 0, 50));
            float hp = target.Status.HP;
            Equip(item, player);
            item.Runtime.Execute(new ItemEvent(ItemTrigger.SkillUse, player, target.transform.position, skillItem: item, cast: new SkillCast()));
            yield return new WaitForSeconds(.15f);
            var visual = Visual(item);
            Require(visual != null && visual.Bombs.Count > 0 && visual.Impacts.Count == 0, "Cancellation did not reach the incoming phase.");
            item.Unequip();
            yield return new WaitForSeconds(2.1f);
            Require(visual == null && Objects(item).All(go => go == null) && Near(target.Status.HP, hp),
                "Unequipped bombardment left VFX or applied delayed damage.");
            entry.cleanedOnUnequip = entry.outsideUnaffected = true;
            Object.Destroy(target.gameObject);
            WriteReport();
        }

        private static void Equip(Item item, PlayerStatus player)
        { equipped.Add(item); item.Equip(player, 1 << LayerMask.NameToLayer("Enemy")); }

        private static MeleeEnemy Enemy(Vector3 position)
        {
            var host = new GameObject("Bombardment Target");
            host.SetActive(false);
            host.layer = LayerMask.NameToLayer("Enemy");
            host.transform.position = position;
            host.AddComponent<EnemyStatus>().ConfigureSpawnStats(1000, 0, 0);
            host.AddComponent<SphereCollider>().radius = .25f;
            var enemy = host.AddComponent<MeleeEnemy>();
            host.SetActive(true);
            enemy.PrepareForTesting(null);
            BuffManager.Instance.SetCondition(enemy.Status, frozen, BuffStat.Stun, 100);
            return enemy;
        }

        private static List<GameObject> Objects(Item item) =>
            (List<GameObject>)typeof(ItemRuntime).GetField("objects", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(item.Runtime);
        private static ItemBombardmentVfx Visual(Item item) => Objects(item).Where(go => go != null)
            .Select(go => go.GetComponent<ItemBombardmentVfx>()).FirstOrDefault(go => go != null);

        private static void CheckSpeed(Item item, string field, GameObject instance, float speed)
        {
            var root = Resources.Load<GameObject>(item.Meta.VfxPath).GetComponent<ItemBombardmentVfx>();
            var stage = new SerializedObject(root).FindProperty(field).objectReferenceValue as GameObject;
            ParticleSystem[] expected = stage.GetComponentsInChildren<ParticleSystem>(true);
            ParticleSystem[] actual = instance.GetComponentsInChildren<ParticleSystem>(true);
            Require(actual.Length == expected.Length, item.ID + " stage particle hierarchy changed.");
            for (int i = 0; i < expected.Length; i++)
                Require(Near(actual[i].main.simulationSpeed, expected[i].main.simulationSpeed * speed),
                    item.ID + " stage playback speed ignored Effect settings.");
        }

        private static void Finish(Exception error)
        {
            SessionState.SetBool(Running, false);
            deadline = 0;
            foreach (Item item in equipped) item.Unequip();
            equipped.Clear();
            if (report == null) report = new Report();
            report.result = error == null ? "passed" : "failed";
            report.error = error?.ToString() ?? "";
            WriteReport();
            if (EditorApplication.isPlaying) Application.runInBackground = previousBackground;
            if (error == null) Debug.Log("[ItemBombardmentChecks] PASS: range/direction, ordered concurrent sweeps, simultaneous impact, delayed OverlapBox damage, double center hit, playback/lifetime and cancellation.");
            else Debug.LogException(error);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        private static void WriteReport() => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        private static bool Near(float a, float b) => Mathf.Abs(a - b) < .02f;
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
