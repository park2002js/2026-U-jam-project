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
    public static class PassiveItemVfxChecks
    {
        private const string Request = "Library/CheckPassiveItemVFX.request";
        private const string ReportPath = "Library/PassiveItemVfxChecks.json";
        private const string Running = "UJam.PassiveItemVfxChecks.Running";
        private static readonly List<Item> equipped = new();
        private static readonly object frozen = new();
        private static double deadline;
        private static bool previousBackground;
        private static Report report;

        [Serializable] private sealed class Observation
        {
            public string id;
            public bool threshold, damage, position, resetOrDeduplication, expired, cleanedOnUnequip;
            public float diameter, observedLifetime;
        }
        [Serializable] private sealed class Report
        {
            public string result, error;
            public List<Observation> items = new();
        }

        static PassiveItemVfxChecks()
        {
            EditorApplication.update += RequestedRun;
            EditorApplication.playModeStateChanged += PlayChanged;
        }

        [MenuItem("Tools/Items/Check Passive Item VFX Assets")]
        public static void CheckAssets()
        {
            string[] ids = { "16", "22", "24" };
            string[] names = { "SilverArrow_VFX", "Overload_VFX", "CorpseExplosion_VFX" };
            for (int i = 0; i < ids.Length; i++)
            {
                ItemMeta meta = ItemCatalog.GetMeta(ids[i]);
                Require(meta.VfxPath == "ItemFX/" + names[i] && meta.Kind == ItemKind.Passive,
                    ids[i] + " passive VFX mapping is incorrect.");
                GameObject root = Resources.Load<GameObject>(meta.VfxPath);
                Require(root != null && root.GetComponent<ItemVfx>() != null, ids[i] + " requires ItemVfx on its root.");
                Require(root.GetComponent<ItemVfx>().ReferenceDiameter > 0 &&
                        root.GetComponentsInChildren<ParticleSystem>(true).Length > 0, ids[i] + " has no calibrated particles.");
                Require(root.GetComponentsInChildren<AudioSource>(true).Length == 0 &&
                        !root.GetComponentsInChildren<MonoBehaviour>(true).Any(behaviour => behaviour != null && behaviour.GetType().Name == "PolygonSoundSpawn"),
                    ids[i] + " must not contain package sound playback.");
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                        ids[i] + " contains a missing script.");
            }
        }

        private static void RequestedRun()
        {
            if (SessionState.GetBool(Running, false) && deadline > 0 && EditorApplication.timeSinceStartup > deadline)
            { Finish(new TimeoutException("Passive VFX checks exceeded 60 seconds.")); return; }
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            report = new Report { result = "starting" };
            try { CheckAssets(); SessionState.SetBool(Running, true); EditorApplication.isPlaying = true; }
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
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        foreach (GameObject root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                            if (root != timer.transform.root.gameObject) root.SetActive(false);
                    foreach (Transform child in timer.transform) child.gameObject.SetActive(false);
                    foreach (var behaviour in timer.GetComponents<MonoBehaviour>()) if (behaviour != timer) behaviour.enabled = false;
                    SceneManager.SetActiveScene(SceneManager.CreateScene("Passive Item VFX Checks"));
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
                Finish(new InvalidOperationException("Play mode stopped before passive VFX checks finished."));
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
            var host = new GameObject("Passive VFX Grid");
            host.SetActive(false);
            var grid = host.AddComponent<GridSystem>();
            grid.CellWidth = grid.CellHeight = 2;
            grid.ColumnCount = grid.RowCount = 32;
            typeof(GridSystem).GetProperty(nameof(GridSystem.Instance)).SetValue(null, null);
            host.SetActive(true);
            Require(grid.IsInitialized, "Test grid failed to initialize.");
            var player = new GameObject("Passive VFX Player").AddComponent<PlayerStatus>();
            yield return CheckFrontPosition(player);
            foreach (string id in new[] { "16", "22", "24" })
                yield return ItemAssets.LoadAsync(ItemCatalog.GetMeta(id).VfxPath, _ => { });
            yield return CheckSilver(player);
            yield return CheckOverload(player);
            yield return CheckCorpse(player);
        }

        private static IEnumerator CheckFrontPosition(PlayerStatus player)
        {
            var target = Enemy(Vector3.zero);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(target.transform, false);
            body.transform.localPosition = Vector3.up * 2;
            body.transform.localScale = new Vector3(2, 4, 2);
            body.GetComponent<Collider>().enabled = false;
            var renderer = body.GetComponent<Renderer>();
            var main = new GameObject("Main hit VFX camera").AddComponent<Camera>();
            main.tag = "MainCamera";
            main.transform.position = new Vector3(0, 15, 15);
            main.transform.LookAt(renderer.bounds.center);
            var owner = new GameObject("Owner hit VFX camera").AddComponent<Camera>();
            owner.transform.position = new Vector3(10, 8, -15);
            owner.transform.LookAt(renderer.bounds.center);
            var combat = player.gameObject.AddComponent<PlayerCombatManager>();
            var settings = new SerializedObject(combat);
            var context = new ItemUseContext(ItemCatalog.Create("16"), new ItemEvent(ItemTrigger.Shooting, player));
            try
            {
                CheckFront(context, target, renderer.bounds, main);
                settings.FindProperty("_aimCamera").objectReferenceValue = owner;
                settings.ApplyModifiedPropertiesWithoutUndo();
                CheckFront(context, target, renderer.bounds, owner);
                owner.orthographic = true;
                CheckFront(context, target, renderer.bounds, owner);
                body.transform.localScale *= 3;
                CheckFront(context, target, renderer.bounds, owner);
                renderer.enabled = false;
                CheckFront(context, target, target.GetComponent<Collider>().bounds, owner);
                owner.orthographic = false;
                owner.transform.position = target.transform.position + Vector3.back * .4f;
                owner.transform.LookAt(target.transform.position);
                Vector3 clipped = ItemVfx.HitPosition(context, target);
                Require(Vector3.Dot(clipped - owner.transform.position, owner.transform.forward) >= owner.nearClipPlane + .04f,
                    "Close-camera hit VFX crossed the near clipping plane.");
                settings.FindProperty("_aimCamera").objectReferenceValue = null;
                settings.ApplyModifiedPropertiesWithoutUndo();
                main.gameObject.SetActive(false); owner.gameObject.SetActive(false);
                Require(Vector3.Distance(ItemVfx.HitPosition(context, target), target.transform.position) < .001f,
                    "A missing camera must preserve the enemy position snapshot.");
            }
            finally
            {
                Object.Destroy(target.gameObject); Object.Destroy(combat);
                Object.Destroy(main.gameObject); Object.Destroy(owner.gameObject);
            }
            yield return null;
        }

        private static void CheckFront(ItemUseContext context, EnemyBase target, Bounds bounds, Camera camera)
        {
            Vector3 point = ItemVfx.HitPosition(context, target);
            Vector3 toward = camera.orthographic ? -camera.transform.forward : (camera.transform.position - bounds.center).normalized;
            float surface = Vector3.Dot(new Vector3(Mathf.Abs(toward.x), Mathf.Abs(toward.y), Mathf.Abs(toward.z)), bounds.extents);
            Require(Vector3.Dot(point - bounds.center, toward) >= surface + .15f,
                "Hit VFX is not in front of the visible body for the selected camera.");
            Vector2 centerScreen = camera.WorldToViewportPoint(bounds.center), pointScreen = camera.WorldToViewportPoint(point);
            Require(Vector2.Distance(centerScreen, pointScreen) < .001f,
                "Moving hit VFX toward the camera changed its screen position.");
        }

        private static IEnumerator CheckSilver(PlayerStatus player)
        {
            Item item = Equip("16", player);
            var entry = new Observation { id = "16", diameter = 3.5f };
            report.items.Add(entry);
            MeleeEnemy target = Enemy(Vector3.zero), other = Enemy(Vector3.right * 6);
            float hp = target.Status.HP;
            for (int i = 0; i < 4; i++) Shoot(player, target);
            yield return null;
            Require(Live(item).Length == 0 && Near(target.Status.HP, hp), "SilverArrow activated before the fifth consecutive hit.");
            Shoot(player, target);
            Require(Near(hp - target.Status.HP, player.AttackDamage), "SilverArrow bonus damage was not applied immediately.");
            yield return WaitVisuals(item, 1);
            CheckVisuals(item, 3.5f, Vector3.zero);
            entry.threshold = entry.damage = entry.position = true;
            yield return CheckExpiry(item, entry);

            hp = target.Status.HP;
            for (int i = 0; i < 4; i++) Shoot(player, target);
            Shoot(player, null);
            Shoot(player, target);
            yield return null;
            Require(Live(item).Length == 0 && Near(target.Status.HP, hp), "SilverArrow did not reset on a miss.");
            for (int i = 0; i < 3; i++) Shoot(player, target);
            float otherHp = other.Status.HP;
            Shoot(player, other);
            Shoot(player, target);
            yield return null;
            Require(Live(item).Length == 0 && Near(target.Status.HP, hp) && Near(other.Status.HP, otherHp),
                "SilverArrow did not reset on a target change.");
            entry.resetOrDeduplication = true;
            item.Unequip();

            Item lethal = Equip("16", player);
            var victim = Enemy(new Vector3(10, 0, 0), 1);
            Vector3 deathPoint = victim.transform.position;
            for (int i = 0; i < 5; i++) Shoot(player, victim);
            Require(victim.Status.HP <= 0, "SilverArrow lethal bonus did not kill its target.");
            yield return WaitVisuals(lethal, 1);
            yield return new WaitForSeconds(.6f);
            CheckVisuals(lethal, 3.5f, deathPoint);
            lethal.Unequip();
            yield return null;
            Require(Live(lethal).Length == 0 && Objects(lethal).All(go => go == null), "SilverArrow VFX survived Unequip.");
            entry.cleanedOnUnequip = true;
            Object.Destroy(target.gameObject); Object.Destroy(other.gameObject);
            WriteReport();
        }

        private static IEnumerator CheckOverload(PlayerStatus player)
        {
            Item item = Equip("22", player), active = ItemCatalog.Create("7");
            var entry = new Observation { id = "22", diameter = 3.5f };
            report.items.Add(entry);
            MeleeEnemy a = Enemy(new Vector3(20, 0, 0)), b = Enemy(new Vector3(24, 0, 0));
            float hp = a.Status.HP, otherHp = b.Status.HP;
            for (int i = 0; i < 4; i++) HitSkill(player, active, player.BeginSkill(active), a, b);
            yield return null;
            Require(Live(item).Length == 0 && Near(a.Status.HP, hp), "Overload activated before the fifth skill use.");
            SkillCast cast = player.BeginSkill(active);
            HitSkill(player, active, cast, a, b);
            Require(Near(hp - a.Status.HP, player.AttackDamage) && Near(otherHp - b.Status.HP, player.AttackDamage),
                "Overload bonus damage was not applied per enemy.");
            Require(ElementManager.Instance.TryGet(a, ElementType.Shock, out _) && ElementManager.Instance.TryGet(b, ElementType.Shock, out _),
                "Overload did not apply Shock.");
            yield return WaitVisuals(item, 2);
            CheckVisuals(item, 3.5f, a.transform.position, b.transform.position);
            entry.threshold = entry.damage = entry.position = true;
            hp = a.Status.HP; otherHp = b.Status.HP;
            HitSkill(player, active, cast, a, b);
            yield return null;
            Require(Live(item).Length == 2 && Near(a.Status.HP, hp) && Near(b.Status.HP, otherHp),
                "Repeated SkillHit created duplicate Overload damage or VFX for the same cast.");
            entry.resetOrDeduplication = true;
            yield return CheckExpiry(item, entry);
            for (int i = 0; i < 5; i++) HitSkill(player, active, player.BeginSkill(active), a, b);
            yield return WaitVisuals(item, 2);
            item.Unequip();
            yield return null;
            Require(Live(item).Length == 0 && Objects(item).All(go => go == null), "Overload VFX survived Unequip.");
            entry.cleanedOnUnequip = true;
            Object.Destroy(a.gameObject); Object.Destroy(b.gameObject);
            WriteReport();
        }

        private static IEnumerator CheckCorpse(PlayerStatus player)
        {
            Item item = Equip("24", player);
            var entry = new Observation { id = "24", diameter = 4 };
            report.items.Add(entry);
            Vector3 point = new Vector3(40, 0, 0);
            MeleeEnemy inside = Enemy(point + Vector3.right), outside = Enemy(point + Vector3.right * 5);
            float hp = inside.Status.HP, outsideHp = outside.Status.HP;
            for (int i = 0; i < 3; i++) Kill(player, new Vector3(-60 - i * 5, 0, 0));
            yield return null;
            Require(player.GetCount(item) == 3 && Live(item).Length == 0, "CorpseExplosion activated before the fourth real kill.");
            Kill(player, point);
            Require(player.GetCount(item) == 0 && Near(hp - inside.Status.HP, player.AttackDamage) && Near(outside.Status.HP, outsideHp),
                "CorpseExplosion counter, damage or AoE boundary failed.");
            yield return WaitVisuals(item, 1);
            CheckVisuals(item, 4, point);
            entry.threshold = entry.damage = entry.position = entry.resetOrDeduplication = true;
            yield return CheckExpiry(item, entry);
            for (int i = 0; i < 3; i++) Kill(player, new Vector3(-90 - i * 5, 0, 0));
            Kill(player, point);
            yield return WaitVisuals(item, 1);
            item.Unequip();
            yield return null;
            Require(Live(item).Length == 0 && Objects(item).All(go => go == null), "CorpseExplosion VFX survived Unequip.");
            entry.cleanedOnUnequip = true;
            Object.Destroy(inside.gameObject); Object.Destroy(outside.gameObject);
            WriteReport();
        }

        private static IEnumerator CheckExpiry(Item item, Observation entry)
        {
            float lifetime = item.ID == "24" ? 4.5f : 1.5f;
            float born = Time.time;
            while (Live(item).Length > 0 && Time.time - born < lifetime + .7f) yield return null;
            entry.observedLifetime = Time.time - born;
            Require(Live(item).Length == 0 && Mathf.Abs(entry.observedLifetime - lifetime) < .3f,
                item.ID + " VFX did not expire at its configured lifetime.");
            entry.expired = true;
        }

        private static IEnumerator WaitVisuals(Item item, int count)
        {
            float limit = Time.time + 2;
            while (Live(item).Length < count && Time.time < limit) yield return null;
            Require(Live(item).Length == count, item.ID + " produced the wrong number of conditional VFX.");
        }

        private static void CheckVisuals(Item item, float diameter, params Vector3[] positions)
        {
            ItemVfx[] visuals = Live(item);
            Require(visuals.Length == positions.Length, item.ID + " has unexpected VFX instances.");
            foreach (Vector3 point in positions)
                Require(visuals.Any(visual => Vector3.Distance(visual.transform.position, point + Vector3.up * .03f) < .05f),
                    item.ID + " VFX followed the cursor/corpse instead of its captured effect position.");
            foreach (ItemVfx visual in visuals)
                Require(Near(visual.ReferenceDiameter * visual.transform.lossyScale.x, diameter) &&
                        visual.GetComponentsInChildren<ParticleSystem>(true).All(particle => particle.main.scalingMode == ParticleSystemScalingMode.Hierarchy),
                    item.ID + " VFX did not use its configured diameter.");
        }

        private static Item Equip(string id, PlayerStatus player)
        { Item item = ItemCatalog.Create(id); equipped.Add(item); item.Equip(player, 1 << LayerMask.NameToLayer("Enemy")); return item; }
        private static void Shoot(PlayerStatus player, EnemyBase target) => EventManager.Instance.Publish(
            new ItemEvent(ItemTrigger.Shooting, player, new Vector3(300, 0, 300), target != null ? new[] { target } : null));
        private static void HitSkill(PlayerStatus player, Item active, SkillCast cast, params EnemyBase[] enemies) => EventManager.Instance.Publish(
            new ItemEvent(ItemTrigger.SkillHit, player, new Vector3(300, 0, 300), enemies, active, cast));
        private static void Kill(PlayerStatus player, Vector3 point) => player.DealDamage(Enemy(point, 1), 100, "Passive VFX check");
        private static MeleeEnemy Enemy(Vector3 position, float health = 1000)
        {
            var host = new GameObject("Passive VFX Target");
            host.SetActive(false); host.layer = LayerMask.NameToLayer("Enemy"); host.transform.position = position;
            host.AddComponent<EnemyStatus>().ConfigureSpawnStats(health, 0, 0);
            host.AddComponent<SphereCollider>().radius = .25f;
            var enemy = host.AddComponent<MeleeEnemy>();
            host.SetActive(true); enemy.PrepareForTesting(null);
            BuffManager.Instance.SetCondition(enemy.Status, frozen, BuffStat.Stun, 100);
            return enemy;
        }
        private static List<GameObject> Objects(Item item) => (List<GameObject>)typeof(ItemRuntime)
            .GetField("objects", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(item.Runtime);
        private static ItemVfx[] Live(Item item) => Objects(item).Where(go => go != null && go.activeInHierarchy)
            .Select(go => go.GetComponent<ItemVfx>()).Where(visual => visual != null).ToArray();
        private static void Finish(Exception error)
        {
            SessionState.SetBool(Running, false); deadline = 0;
            foreach (Item item in equipped) item.Unequip();
            equipped.Clear();
            report ??= new Report(); report.result = error == null ? "passed" : "failed"; report.error = error?.ToString() ?? "";
            WriteReport();
            if (EditorApplication.isPlaying) Application.runInBackground = previousBackground;
            if (error == null) Debug.Log("[PassiveItemVfxChecks] PASS: conditional counters, damage, per-enemy/death positions, Overload deduplication/Shock, Corpse AoE, lifetime and Unequip cleanup.");
            else Debug.LogException(error);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }
        private static void WriteReport() => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        private static bool Near(float a, float b) => Mathf.Abs(a - b) < .02f;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
