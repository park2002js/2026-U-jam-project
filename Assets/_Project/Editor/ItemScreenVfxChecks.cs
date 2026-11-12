using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ujam.Runtime.Item;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UJam.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    [InitializeOnLoad]
    public static class ItemScreenVfxChecks
    {
        private const string Request = "Library/CheckItemScreenVFX.request";
        private const string ReportPath = "Library/ItemScreenVfxChecks.json";
        private const string Running = "UJam.ItemScreenVfxChecks.Running";
        private const string PrefabPath = "Assets/_Project/Prefabs/VFX/ScreenEffect/ItemScreenVFX.prefab";
        private static readonly string[] IDs = { "13", "14", "15", "17", "25", "27" };
        private static GameObject fixture, screenRoot;
        private static ItemScreenVfxCanvas screenVfx;
        private static int canvasCount;
        private static Report report;
        private static bool previousBackground;
        private static double deadline;

        [Serializable] private sealed class Report
        {
            public string result, error;
            public List<string> checks = new();
        }

        static ItemScreenVfxChecks()
        {
            EditorApplication.update += RequestedRun;
            EditorApplication.playModeStateChanged += PlayChanged;
        }

        [MenuItem("Tools/Items/Check Screen VFX Assets")]
        public static void CheckAssets()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Require(root != null && root.GetComponent<RectTransform>() != null && root.GetComponent<ItemScreenVfxCanvas>() != null,
                "The common ItemScreenVFX prefab requires its RectTransform and Canvas controller.");
            var layers = root.GetComponentsInChildren<ItemScreenVfx>(true);
            Require(layers.Length == 6 && IDs.All(id => layers.Count(layer => layer.ItemId == id) == 1),
                "The common screen prefab requires one layer per screen item ID.");
            var references = new SerializedObject(root.GetComponent<ItemScreenVfxCanvas>()).FindProperty("effects");
            Require(references != null && references.arraySize == 6, "The Canvas controller must reference all six layers.");
            var referenced = new HashSet<ItemScreenVfx>();
            for (int i = 0; i < references.arraySize; i++)
            {
                var layer = references.GetArrayElementAtIndex(i).objectReferenceValue as ItemScreenVfx;
                Require(layers.Contains(layer) && referenced.Add(layer), "The Canvas controller must reference each of its own layers exactly once.");
            }
            Require(root.GetComponentsInChildren<GraphicRaycaster>(true).Length == 0 &&
                root.GetComponentsInChildren<Image>(true).All(image => !image.raycastTarget), "Screen layers must not intercept player input.");
            Require(root.GetComponentsInChildren<AudioSource>(true).Length == 0, "Screen layers must not add package audio.");
            foreach (string id in IDs)
            {
                Require(string.IsNullOrEmpty(ItemCatalog.GetMeta(id).VfxPath), id + " screen effect must use the authored Canvas instead of Resources loading.");
                var layer = layers.Single(view => view.ItemId == id);
                var visuals = layer.GetComponentInChildren<ScreenEffectManager>(true);
                Require(visuals != null && !new SerializedObject(visuals).FindProperty("_registerAsInstance").boolValue,
                    id + " must not replace the LCH tester singleton.");
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, PrefabPath + " has a missing script.");
            foreach (string id in new[] { "13", "14", "15" })
                Require(ItemCatalog.GetMeta(id).CastType == ItemCastType.Instant && ItemCatalog.GetMeta(id).Target == ItemTarget.Player,
                    id + " must cast immediately without a cursor target.");
        }

        private static void RequestedRun()
        {
            if (SessionState.GetBool(Running, false) && deadline > 0 && EditorApplication.timeSinceStartup > deadline)
            { Finish(new TimeoutException("Screen VFX checks exceeded 60 seconds.")); return; }
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
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
                    report = new Report { result = "running" }; WriteReport();
                    var timer = RuntimeTimer.Ensure();
                    timer.transform.SetParent(null, true);
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        foreach (GameObject root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                            if (root != timer.transform.root.gameObject) root.SetActive(false);
                    foreach (Transform child in timer.transform) child.gameObject.SetActive(false);
                    foreach (var behaviour in timer.GetComponents<MonoBehaviour>()) if (behaviour != timer) behaviour.enabled = false;
                    SceneManager.SetActiveScene(SceneManager.CreateScene("Item Screen VFX Checks"));
                    previousBackground = Application.runInBackground; Application.runInBackground = true;
                    Time.timeScale = 1; EditorApplication.isPaused = false;
                    deadline = EditorApplication.timeSinceStartup + 60;
                    timer.StartCoroutine(Guard(CheckRuntime()));
                }
                catch (Exception error) { Finish(error); }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
                Finish(new InvalidOperationException("Play mode stopped before screen VFX checks finished."));
        }

        private static IEnumerator Guard(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
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
            fixture = new GameObject("Screen VFX Player"); fixture.SetActive(false);
            var player = fixture.AddComponent<PlayerStatus>();
            var skills = fixture.AddComponent<PlayerSkillManager>();
            var inventory = fixture.AddComponent<PlayerInventory>();
            var settings = new SerializedObject(inventory);
            settings.FindProperty("_playerStatus").objectReferenceValue = player;
            settings.FindProperty("_skillManager").objectReferenceValue = skills;
            settings.ApplyModifiedPropertiesWithoutUndo();
            fixture.SetActive(true);
            screenRoot = new GameObject("Screen VFX Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            screenRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var screenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            screenVfx = Object.Instantiate(screenPrefab, screenRoot.transform, false).GetComponent<ItemScreenVfxCanvas>();
            screenVfx.Bind(player);
            canvasCount = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length;

            Require(inventory.TryAdd("13"), "Could not equip Adrenaline.");
            Item adrenaline = Equipped(inventory, "13");
            skills.TryUse(Slot(skills, adrenaline));
            Require(!skills.IsAiming && Near(BuffManager.Instance.Percent(player, BuffStat.AttackDamage), 10) &&
                    Near(BuffManager.Instance.Percent(player, BuffStat.AttackSpeed), 10), "Adrenaline did not immediately apply its buffs.");
            CheckCanvas(adrenaline);
            inventory.TryRemove("13"); yield return null;
            Require(ItemScreenVfx.GetActive(adrenaline) == null, "Unequipped Adrenaline left its border active.");
            Passed("13: immediate self-cast, attack/speed buffs, Canvas and Unequip cleanup");

            player.TakeDamage(new DamageInfo(30, "Screen check", ignoreDamageTaken: true));
            Require(inventory.TryAdd("14"), "Could not equip Emergency Repair.");
            Item repair = Equipped(inventory, "14"); int repairSlot = Slot(skills, repair);
            float before = player.CurrentHealth;
            skills.TryUse(repairSlot);
            Require(Near(player.CurrentHealth, before + player.MaxHealth * .1f) && inventory.GetCount("14") == 0 &&
                    skills.GetSkill(repairSlot) == null && !repair.IsEquipped && !skills.IsAiming,
                "Repair must heal and remove itself from inventory and skill slot in the same key-use call.");
            var repairFx = CheckCanvas(repair);
            yield return new WaitForSeconds(.85f);
            Require(ItemScreenVfx.GetActive(repair) == repairFx, "Consumed Repair cut off the sparkle tail before 1.05 seconds.");
            yield return new WaitForSeconds(.4f);
            Require(repairFx != null && ItemScreenVfx.GetActive(repair) == null, "Repair must expire without destroying its reusable layer.");
            Passed("14: immediate heal, one-use inventory/slot removal, consumed-item FX survives and expires");

            Require(inventory.TryAdd("15"), "Could not equip Life Conversion.");
            Item conversion = Equipped(inventory, "15"); before = player.CurrentHealth;
            skills.TryUse(Slot(skills, conversion));
            Require(!skills.IsAiming && Near(player.CurrentHealth, before - player.MaxHealth * .05f) &&
                    Near(BuffManager.Instance.Percent(player, BuffStat.Damage), 10), "Life Conversion did not immediately pay health and apply its buff.");
            var conversionFx = CheckCanvas(conversion);
            yield return new WaitForSeconds(1.2f);
            Require(ItemScreenVfx.GetActive(conversion) == conversionFx, "Life Conversion cut off its 1.4-second center icon.");
            yield return new WaitForSeconds(.4f);
            Require(conversionFx != null && ItemScreenVfx.GetActive(conversion) == null && Near(BuffManager.Instance.Percent(player, BuffStat.Damage), 10),
                "Life Conversion feedback and its twenty-second buff have different lifetimes.");
            inventory.TryRemove("15");
            Passed("15: immediate health cost/buff, complete center icon, independent buff duration");

            Require(inventory.TryAdd("17"), "Could not equip Tenacity.");
            Item tenacity = Equipped(inventory, "17");
            var enemyHost = new GameObject("Tenacity target"); enemyHost.SetActive(false);
            enemyHost.AddComponent<EnemyStatus>().ConfigureSpawnStats(1000, 0, 0);
            var enemy = enemyHost.AddComponent<MeleeEnemy>(); enemyHost.SetActive(true); enemy.PrepareForTesting(null);
            for (int i = 0; i < 4; i++) Shoot(player, enemy);
            Require(ItemScreenVfx.GetActive(tenacity) == null, "Tenacity played before its fifth hit.");
            Shoot(player, enemy); var tenacityFx = CheckCanvas(tenacity);
            Require(Near(BuffManager.Instance.Percent(player, BuffStat.AttackSpeed), 10), "Tenacity did not grant attack speed.");
            yield return new WaitForSeconds(.4f);
            for (int i = 0; i < 5; i++) Shoot(player, enemy);
            Require(ItemScreenVfx.GetActive(tenacity) == tenacityFx, "Tenacity should refresh its own presentation rather than multiply Canvases.");
            yield return new WaitForSeconds(1.7f);
            Require(ItemScreenVfx.GetActive(tenacity) == tenacityFx && Near(BuffManager.Instance.Percent(player, BuffStat.AttackSpeed), 10), "Tenacity refresh did not renew its lifetime.");
            yield return new WaitForSeconds(.4f);
            Require(tenacityFx != null && ItemScreenVfx.GetActive(tenacity) == null, "Tenacity must expire and retain its layer after its refreshed duration.");
            for (int i = 0; i < 4; i++) Shoot(player, enemy);
            Shoot(player, null); Shoot(player, enemy);
            Require(ItemScreenVfx.GetActive(tenacity) == null, "Tenacity miss did not reset its streak.");
            inventory.TryRemove("17"); Object.Destroy(enemyHost);
            Passed("17: conditional five-hit proc, miss reset, same-source refresh and expiration");

            Require(inventory.TryAdd("25"), "Could not equip Resurrection.");
            Item resurrection = Equipped(inventory, "25");
            player.TakeDamage(new DamageInfo(player.MaxHealth * 2, "Screen check", ignoreDamageTaken: true));
            Require(Near(player.CurrentHealth, player.MaxHealth * .5f) && inventory.GetCount("25") == 0 && !resurrection.IsEquipped,
                "Resurrection did not prevent death, restore fifty-percent health and consume itself.");
            var resurrectionFx = CheckCanvas(resurrection);
            yield return new WaitForSeconds(1.3f);
            Require(ItemScreenVfx.GetActive(resurrection) == resurrectionFx, "Consumed Resurrection cut off its 1.58-second border.");
            yield return new WaitForSeconds(.45f);
            Require(resurrectionFx != null && ItemScreenVfx.GetActive(resurrection) == null, "Resurrection must expire without destroying its layer.");
            Passed("25: real lethal damage, revival, consumption and complete screen presentation");

            player.Heal(player.MaxHealth);
            Require(inventory.TryAdd("27"), "Could not equip Berserker Rage.");
            Item rage = Equipped(inventory, "27");
            Require(ItemScreenVfx.GetActive(rage) == null, "Full-health Rage should not create a border.");
            ItemScreenVfx rageFx = null;
            foreach (int hp in new[] { 81, 80, 60, 40, 20 })
            {
                player.TakeDamage(new DamageInfo(player.CurrentHealth - hp, "Screen check", ignoreDamageTaken: true));
                int step = hp == 81 ? 0 : hp == 80 ? 1 : hp == 60 ? 2 : hp == 40 ? 3 : 4;
                var current = ItemScreenVfx.GetActive(rage);
                Require(step == 0 ? current == null : current != null && current.RageStep == step,
                    "Rage stage does not match health threshold " + hp);
                Require(Near(BuffManager.Instance.Percent(player, BuffStat.AttackDamage), step * 4), "Rage stats do not match its stage.");
                if (current == null) continue;
                if (rageFx != null) Require(current == rageFx, "Health changes created additional Rage Canvases.");
                rageFx = current; player.RefreshHealth();
                Require(ItemScreenVfx.GetActive(rage) == rageFx, "Repeated health notification restarted Rage.");
                yield return new WaitForSeconds(.15f);
                var rageImage = (Image)new SerializedObject(current.GetComponentInChildren<ScreenEffectManager>())
                    .FindProperty("_rageVignette").objectReferenceValue;
                Require(Near(rageImage.color.a, step * .125f), "Rage border alpha does not match the LCH stage.");
            }
            player.Heal(player.MaxHealth);
            Require(rageFx != null && rageFx.RageStep == 0, "Recovery did not fade Rage toward zero.");
            yield return new WaitForSeconds(.5f);
            Require(ItemScreenVfx.GetActive(rage) == null, "Recovered Rage did not finish fading out.");
            player.TakeDamage(new DamageInfo(40, "Screen check", ignoreDamageTaken: true));
            rageFx = CheckCanvas(rage);
            Require(inventory.TryAdd("13"), "Could not equip concurrent Adrenaline.");
            adrenaline = Equipped(inventory, "13"); skills.TryUse(Slot(skills, adrenaline));
            var otherFx = CheckCanvas(adrenaline);
            inventory.TryRemove("27"); yield return null;
            Require(rageFx != null && ItemScreenVfx.GetActive(rage) == null && ItemScreenVfx.GetActive(adrenaline) == otherFx,
                "Removing Rage must clear only its own source and retain both layers.");
            Passed("27: health stages, idempotent updates, recovery fade, isolated Unequip cleanup");

            Require(inventory.TryAdd("14"), "Could not equip final owner-cleanup probe.");
            repair = Equipped(inventory, "14"); skills.TryUse(Slot(skills, repair));
            repairFx = CheckCanvas(repair);
            Object.Destroy(fixture); yield return null; yield return null;
            Require(repairFx != null && ItemScreenVfx.GetActive(repair) == null, "The consumed-item layer retained its source after its player was destroyed.");
            Require(screenVfx.GetComponentsInChildren<ItemScreenVfx>(true).Length == 6 &&
                Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length == canvasCount,
                "Item use must reuse the six authored layers without creating Canvases.");
            Passed("owner destruction clears consumed-item screen effects");
            Passed("one preplaced Canvas and six reusable layers serve all screen item activations");
        }

        private static ItemScreenVfx CheckCanvas(Item item)
        {
            var effect = ItemScreenVfx.GetActive(item);
            Require(effect != null && effect.GetComponentInParent<Canvas>() != null && effect == screenVfx.GetView(item.ID),
                item.ID + " did not use its preplaced Canvas layer immediately.");
            Require(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length == canvasCount,
                item.ID + " created an additional Canvas during use.");
            Require(effect.GetComponentsInChildren<Image>(true).All(image => !image.raycastTarget), item.ID + " screen Images block input.");
            return effect;
        }
        private static Item Equipped(PlayerInventory inventory, string id)
        {
            var equipment = typeof(PlayerInventory).GetField("equipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inventory);
            var items = (Dictionary<int, Item>)equipment.GetType().GetField("equipped", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(equipment);
            return items.Values.Single(item => item.ID == id);
        }
        private static int Slot(PlayerSkillManager skills, Item item) => skills.GetSkill(0) == item ? 0 : skills.GetSkill(1) == item ? 1 : -1;
        private static void Shoot(PlayerStatus player, EnemyBase enemy) => EventManager.Instance.Publish(
            new ItemEvent(ItemTrigger.Shooting, player, enemies: enemy != null ? new[] { enemy } : null));
        private static void Passed(string text) { report.checks.Add(text); WriteReport(); }
        private static void Finish(Exception error)
        {
            SessionState.SetBool(Running, false); deadline = 0;
            if (fixture != null) Object.Destroy(fixture);
            if (screenRoot != null) Object.Destroy(screenRoot);
            report ??= new Report(); report.result = error == null ? "passed" : "failed"; report.error = error?.ToString() ?? ""; WriteReport();
            if (error == null) Debug.Log("[ItemScreenVfxChecks] PASS: instant casting, one-use inventory removal, conditional procs, Canvas lifetimes and Rage ownership.");
            else Debug.LogException(error);
            if (EditorApplication.isPlaying) { Application.runInBackground = previousBackground; EditorApplication.isPlaying = false; }
        }
        private static void WriteReport() => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        private static bool Near(float a, float b) => Mathf.Abs(a - b) < .02f;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
