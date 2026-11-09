using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ujam.Runtime.Item;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    [InitializeOnLoad]
    public static class ItemCastVfxChecks
    {
        private const string Request = "Library/CheckItemCastVFX.request";
        private const string ReportPath = "Library/ItemCastVfxChecks.json";
        private const string Captures = "Library/ItemCastVfxCaptures";
        private const string Running = "UJam.ItemCastVfxChecks.Running";
        private static readonly string[] IDs = { "2", "6", "11" };
        private static readonly string[] Names = { "Fireball", "DecoyTrap", "Lightning" };
        private static readonly List<Item> equippedItems = new List<Item>();
        private static readonly object frozenTargetSource = new object();
        private static Report report;
        private static bool previousRunInBackground;
        private static double deadline;
        private static Camera captureCamera;
        private static RenderTexture captureTarget;

        [Serializable]
        private sealed class Observation
        {
            public string item, scenario;
            public float incomingAt, impactAt, observedDiameter, incomingSpeed, impactSpeed, impactLifetime, settledLifetime;
            public bool movedDown, effectApplied, outsideUnaffected, cleanedOnUnequip, tauntReleased;
            public bool startBehindUpperLeft, enteredUpperLeft, lightningAboveScreen, lightningConnected;
            public float nearestArrival, firepitDiameter, lightningOuterWidth;
        }

        [Serializable]
        private sealed class Report
        {
            public string result, error;
            public List<Observation> cases = new List<Observation>();
        }

        static ItemCastVfxChecks()
        {
            EditorApplication.update += RequestedRun;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Tools/Items/Check Staged Item VFX Assets")]
        public static void Run()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Run asset checks in Edit mode.");
            for (int i = 0; i < IDs.Length; i++)
            {
                ItemMeta meta = ItemCatalog.GetMeta(IDs[i]);
                Require(meta.VfxPath == "ItemFX/" + Names[i] + "_VFX", Names[i] + " top-level VFX path is incorrect.");
                GameObject prefab = Resources.Load<GameObject>(meta.VfxPath);
                Require(prefab != null && prefab.GetComponent<ItemCastVfx>() != null, Names[i] + " requires a top-level ItemCastVfx prefab.");
                ItemIncomingMotion expectedMotion = IDs[i] == "2" ? ItemIncomingMotion.CameraProjectile :
                    IDs[i] == "11" ? ItemIncomingMotion.SkyLightning : ItemIncomingMotion.Fall;
                Require(prefab.GetComponent<ItemCastVfx>().Motion == expectedMotion, Names[i] + " incoming motion is incorrect.");
                var settings = new SerializedObject(prefab.GetComponent<ItemCastVfx>());
                foreach (string field in new[] { "incomingPrefab", "impactPrefab", "settledPrefab" })
                {
                    var stage = settings.FindProperty(field).objectReferenceValue as GameObject;
                    if (field == "settledPrefab" && IDs[i] != "6") continue;
                    Require(stage != null && PrefabUtility.IsPartOfPrefabAsset(stage), Names[i] + "/" + field + " must reference a separate prefab asset.");
                    string stagePath = AssetDatabase.GetAssetPath(stage);
                    Require(stagePath.StartsWith("Assets/_Project/Prefabs/VFX/ItemFX/", StringComparison.Ordinal),
                        stagePath + " must be a project-owned copy.");
                    foreach (Transform child in stage.GetComponentsInChildren<Transform>(true))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, stagePath + " has a missing script.");
                    foreach (Renderer renderer in stage.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer is ParticleSystemRenderer particle)
                        {
                            if (particle.renderMode == ParticleSystemRenderMode.None) continue;
                            Require(particle.sharedMaterial != null && (!particle.GetComponent<ParticleSystem>().trails.enabled || particle.trailMaterial != null),
                                stagePath + "/" + renderer.name + " has missing particle or active trail materials.");
                        }
                        else Require(renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials.All(material => material != null),
                            stagePath + "/" + renderer.name + " has missing mesh materials.");
                        foreach (Material material in renderer.sharedMaterials.Where(value => value != null))
                            Require(material.shader != null && !ShaderUtil.ShaderHasError(material.shader) &&
                                    material.GetTag("RenderPipeline", false) == "UniversalPipeline",
                                stagePath + "/" + material.name + " has no working URP shader.");
                    }
                    if (field == "settledPrefab")
                        Require(stage.GetComponentsInChildren<Renderer>(true).Any(renderer => renderer.sharedMaterials.Any(material =>
                            material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)),
                            "Decoy settled model has no supplied texture material.");
                }
            }
            Debug.Log("[ItemCastVfxChecks] Three top-level VFX prefabs, project stage copies, prefab references and URP materials passed.");
        }

        private static void RequestedRun()
        {
            if (SessionState.GetBool(Running, false) && deadline > 0 && EditorApplication.timeSinceStartup > deadline)
            {
                Finish(new TimeoutException("Staged VFX checks exceeded their real-time limit."));
                return;
            }
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            report = new Report { result = "starting" };
            try
            {
                Run();
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
                    WriteReport();
                    var timer = RuntimeTimer.Ensure();
                    foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                        if (root != timer.transform.root.gameObject) root.SetActive(false);
                    foreach (Transform child in timer.transform) child.gameObject.SetActive(false);
                    foreach (var behaviour in timer.GetComponents<MonoBehaviour>())
                        if (behaviour != timer) behaviour.enabled = false;
                    SceneManager.SetActiveScene(SceneManager.CreateScene("Staged Item VFX Runtime Checks"));
                    previousRunInBackground = Application.runInBackground;
                    Application.runInBackground = true;
                    Time.timeScale = 1;
                    EditorApplication.isPaused = false;
                    deadline = EditorApplication.timeSinceStartup + 100;
                    timer.StartCoroutine(Guard(CheckRuntime()));
                }
                catch (Exception error) { Finish(error); }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
                Finish(new InvalidOperationException("Play mode stopped before staged VFX checks finished."));
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
            var gridHost = new GameObject("CastVFX Grid");
            gridHost.SetActive(false);
            var grid = gridHost.AddComponent<GridSystem>();
            grid.CellWidth = grid.CellHeight = 2;
            grid.RowCount = grid.ColumnCount = 32;
            typeof(GridSystem).GetProperty(nameof(GridSystem.Instance)).SetValue(null, null);
            gridHost.SetActive(true);
            Require(grid.IsInitialized && Near(ItemWorld.CellWidth, 2), "Test grid failed to initialize.");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "CastVFX Ground";
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.position = new Vector3(30, -.5f, 0);
            ground.transform.localScale = new Vector3(120, 1, 60);
            ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            { color = new Color(.12f, .16f, .13f) };
            var light = new GameObject("CastVFX Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(55, -30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .5f, .5f);
            captureCamera = new GameObject("CastVFX Capture Camera").AddComponent<Camera>();
            captureCamera.tag = "MainCamera";
            captureCamera.orthographic = false;
            captureCamera.fieldOfView = 60;
            captureCamera.aspect = 16f / 9f;
            captureCamera.nearClipPlane = .05f;
            captureCamera.farClipPlane = 100;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(.035f, .045f, .05f);
            var cameraData = captureCamera.GetUniversalAdditionalCameraData();
            cameraData.requiresDepthTexture = true;
            cameraData.renderPostProcessing = false;
            captureTarget = new RenderTexture(1280, 720, 24);
            captureTarget.Create();
            captureCamera.targetTexture = captureTarget;
            Directory.CreateDirectory(Captures);
            foreach (string id in IDs)
                foreach (string phase in new[] { "Incoming", "IncomingLate", "Impact", "ImpactLate", "Firepit" })
                    File.Delete(Captures + "/" + id + "_" + phase + ".png");
            var player = new GameObject("CastVFX Player").AddComponent<PlayerStatus>();
            foreach (string id in IDs) yield return ItemAssets.LoadAsync(ItemCatalog.GetMeta(id).VfxPath, _ => { });
            yield return ItemAssets.LoadAsync(ItemAssets.AreaPath, _ => { });

            yield return CheckStrike(ItemCatalog.Create("2"), player, Vector3.zero, "catalog", 12, 1, 0, 1, 1, true);
            yield return CheckStrike(ItemCatalog.Create("11"), player, new Vector3(20, 0, 0), "catalog", 8, 1, .85f, 1, 1, true);
            yield return CheckStrike(Custom("2", new FireballEffect(1, 200, .65f, .3f, 2, .6f, .5f, 6, .1f), 4),
                player, new Vector3(40, 0, 0), "custom speed, scale and delay", 4, .65f, .1f, 2, .6f, false);
            yield return CheckStrike(Custom("11", new LightningEffect(1.5f, 200, .65f, .2f, 1.7f, 1.4f, .5f, 8, .4f), 6),
                player, new Vector3(60, 0, 0), "custom speed, scale and delay", 6, .65f, .4f, 1.7f, 1.4f, false);
            yield return CheckDecoy(player);
            foreach (string id in IDs)
                foreach (string phase in new[] { "Incoming", "Impact" })
                    Require(File.Exists(Captures + "/" + id + "_" + phase + ".png"), id + " " + phase + " capture is missing.");
            foreach (string id in new[] { "2", "11" })
                Require(File.Exists(Captures + "/" + id + "_ImpactLate.png"), id + " late impact capture is missing.");
            Require(File.Exists(Captures + "/2_IncomingLate.png") && File.Exists(Captures + "/2_Firepit.png"),
                "Fireball camera approach or persistent firepit capture is missing.");
        }

        private static IEnumerator CheckStrike(Item item, PlayerStatus player, Vector3 point, string scenario,
            float diameter, float delay, float incomingDelay, float incomingSpeed, float impactSpeed, bool capture)
        {
            var entry = new Observation { item = item.ID, scenario = scenario, incomingAt = -1, nearestArrival = -1 };
            report.cases.Add(entry);
            MeleeEnemy inside = Enemy(point), outside = Enemy(point + Vector3.right * (diameter / 2 + 2));
            float hp = inside.Status.HP, outsideHp = outside.Status.HP;
            Equip(item, player);
            PositionCamera(point);
            Cast(item, player, point);
            float limit = Time.time + 4;
            ItemCastVfx visual;
            while ((visual = Visual(item)) == null && Time.time < limit) yield return null;
            Require(visual != null, item.ID + " did not create its top-level VFX.");
            float start = Time.time, previousY = float.NaN;
            bool incomingCaptured = false, incomingLateCaptured = false, firstOnScreen = false;
            while (visual.ImpactInstance == null && Time.time < limit)
            {
                Require(Near(hp, inside.Status.HP) && Near(outsideHp, outside.Status.HP), item.ID + " dealt damage before impact.");
                Require(!ElementManager.Instance.TryGet(inside, out _), item.ID + " applied an element before impact.");
                if (visual.Incoming != null)
                {
                    if (entry.incomingAt < 0)
                    {
                        entry.incomingAt = Time.time - start;
                        if (item.ID == "2")
                        {
                            Vector3 cameraSpace = captureCamera.transform.InverseTransformPoint(visual.Incoming.transform.position);
                            entry.startBehindUpperLeft = cameraSpace.x < 0 && cameraSpace.y > 0 && cameraSpace.z < 0;
                            Require(entry.startBehindUpperLeft, "Fireball did not start behind the camera's upper-left side.");
                        }
                    }
                    Vector3 incomingPoint = visual.Incoming.transform.position;
                    if (item.ID == "11")
                    {
                        LineRenderer outer = visual.Incoming.GetComponentsInChildren<LineRenderer>(true).Single(line => line.name == "LightningOuter");
                        Require(outer.positionCount >= 8, "Lightning has no jagged sky column.");
                        Vector3 top = LinePoint(outer, 0);
                        incomingPoint = LinePoint(outer, outer.positionCount - 1);
                        Vector3 topScreen = captureCamera.WorldToViewportPoint(top);
                        entry.lightningAboveScreen |= topScreen.z > 0 && topScreen.y > 1;
                        entry.lightningOuterWidth = Mathf.Max(entry.lightningOuterWidth, outer.widthMultiplier * outer.widthCurve.Evaluate(.5f));
                    }
                    float y = incomingPoint.y;
                    entry.movedDown |= float.IsFinite(previousY) && y < previousY - .001f;
                    previousY = y;
                    float distance = Vector3.Distance(incomingPoint, visual.transform.position);
                    entry.nearestArrival = entry.nearestArrival < 0 ? distance : Mathf.Min(entry.nearestArrival, distance);
                    if (item.ID == "2")
                    {
                        Vector3 screen = captureCamera.WorldToViewportPoint(incomingPoint);
                        if (!firstOnScreen && screen.z > captureCamera.nearClipPlane && screen.x >= 0 && screen.x <= 1 && screen.y >= 0 && screen.y <= 1)
                        {
                            firstOnScreen = true;
                            entry.enteredUpperLeft = screen.x < .5f && screen.y > .6f;
                        }
                    }
                    CheckSpeed(item, "incomingPrefab", visual.Incoming, incomingSpeed);
                    float elapsed = Time.time - start - entry.incomingAt;
                    if (capture && !incomingCaptured && elapsed >= (item.ID == "11" ? .07f : .70f))
                    {
                        Capture(item.ID + "_Incoming");
                        incomingCaptured = true;
                    }
                    if (capture && item.ID == "2" && !incomingLateCaptured && elapsed >= .82f)
                    {
                        Capture(item.ID + "_IncomingLate");
                        incomingLateCaptured = true;
                    }
                }
                yield return null;
            }
            Require(visual != null && visual.ImpactInstance != null, item.ID + " did not create its impact VFX.");
            entry.impactAt = Time.time - start;
            Require(Mathf.Abs(entry.impactAt - delay) < .2f && Mathf.Abs(entry.incomingAt - incomingDelay) < .2f,
                item.ID + " staged timing differs from its Effect settings.");
            Require(entry.movedDown, item.ID + " incoming VFX did not descend.");
            if (item.ID == "2")
                Require(entry.enteredUpperLeft && entry.nearestArrival < 2,
                    "Fireball did not enter from the upper-left and approach its impact point.");
            else
                Require(entry.lightningAboveScreen && entry.lightningOuterWidth >= .7f,
                    "Lightning did not form a thick column from beyond the top of the screen.");
            entry.observedDiameter = visual.ImpactInstance.transform.lossyScale.x * visual.ImpactReferenceDiameter;
            Require(Near(entry.observedDiameter, diameter), item.ID + " impact diameter does not follow Effect radius.");
            CheckSpeed(item, "impactPrefab", visual.ImpactInstance, impactSpeed);
            entry.incomingSpeed = incomingSpeed;
            entry.impactSpeed = impactSpeed;
            yield return null;
            if (item.ID == "11")
            {
                Require(visual.Incoming != null, "Lightning column vanished before its ground connection was visible.");
                LineRenderer[] lines = visual.Incoming.GetComponentsInChildren<LineRenderer>(true);
                entry.lightningConnected = lines.Length == 2 && lines.All(line =>
                    Vector3.Distance(LinePoint(line, line.positionCount - 1), visual.transform.position) < .02f);
                Require(entry.lightningConnected, "Lightning did not connect both blue/core columns to the impact point.");
            }
            else entry.firepitDiameter = CheckFirepit(visual, diameter);
            Require(Near(hp - inside.Status.HP, player.AttackDamage * 2), item.ID + " impact damage is incorrect.");
            Require(Near(outsideHp, outside.Status.HP) && !ElementManager.Instance.TryGet(outside, out _), item.ID + " hit outside its range.");
            Require(ElementManager.Instance.TryGet(inside, out ElementState element) &&
                    element.Type == (item.ID == "2" ? ElementType.Burn : ElementType.Shock), item.ID + " impact element is incorrect.");
            entry.effectApplied = entry.outsideUnaffected = true;
            PositionCamera(point);
            yield return new WaitForSeconds(capture ? (item.ID == "11" ? .06f : .2f) : .08f);
            if (capture)
            {
                Capture(item.ID + "_Impact");
                yield return new WaitForSeconds(item.ID == "11" ? .19f : .2f);
                Capture(item.ID + "_ImpactLate");
                if (item.ID == "2")
                {
                    yield return new WaitForSeconds(.5f);
                    Require(visual != null && visual.ImpactInstance != null, "Firepit expired before its lingering ground effect.");
                    Require(visual.ImpactInstance.GetComponentsInChildren<ParticleSystem>(true)
                        .Any(particle => particle.name == "Firepit_MoltenBed" && particle.particleCount > 0),
                        "Firepit ground bed emitted no lingering particles.");
                    Capture("2_Firepit");
                }
            }
            if (!capture)
            {
                float impactAt = start + entry.impactAt;
                while (visual != null && Time.time - impactAt < 1.5f) yield return null;
                entry.impactLifetime = Time.time - impactAt;
                Require(visual == null && Mathf.Abs(entry.impactLifetime - .5f) < .2f,
                    item.ID + " custom impact lifetime differs from Effect settings.");
            }
            item.Unequip();
            yield return null;
            Require(visual == null && Visual(item) == null && Objects(item).All(go => go == null), item.ID + " impact survived Unequip.");
            entry.cleanedOnUnequip = true;
            Object.Destroy(inside.gameObject);
            Object.Destroy(outside.gameObject);
            WriteReport();
            Debug.Log("[ItemCastVfxChecks] " + item.ID + " " + scenario + " passed: impact at " + entry.impactAt.ToString("F2") + "s, diameter " + diameter);
        }

        private static IEnumerator CheckDecoy(PlayerStatus player)
        {
            Vector3 point = new Vector3(80, 0, 0);
            var entry = new Observation { item = "6", scenario = "catalog model, taunt, damage, lifetime and cancellation" };
            report.cases.Add(entry);
            Item item = ItemCatalog.Create("6");
            Equip(item, player);
            MeleeEnemy inside = Enemy(point), outside = Enemy(point + Vector3.right * 8);
            PositionCamera(point);
            Cast(item, player, point);
            float limit = Time.time + 4;
            ItemCastVfx visual;
            while ((visual = Visual(item)) == null && Time.time < limit) yield return null;
            Require(visual != null, "Decoy top-level VFX did not spawn.");
            float start = Time.time, previousY = float.NaN;
            bool incomingCaptured = false;
            while (Decoy(item) == null && Time.time < limit)
            {
                Require(inside.FSM.Targets.Count == 0, "Decoy taunted before landing.");
                if (visual.Incoming != null)
                {
                    float y = visual.Incoming.transform.position.y;
                    entry.movedDown |= float.IsFinite(previousY) && y < previousY - .001f;
                    previousY = y;
                    if (!incomingCaptured && Time.time - start >= .35f)
                    {
                        Capture("6_Incoming");
                        incomingCaptured = true;
                    }
                }
                yield return null;
            }
            DecoyController decoy = Decoy(item);
            Require(decoy != null && entry.movedDown, "Decoy did not fall and create its gameplay object.");
            entry.impactAt = Time.time - start;
            Require(Mathf.Abs(entry.impactAt - 1) < .2f, "Decoy landing time differs from Effect delay.");
            Require(visual.ImpactInstance != null && visual.SettledInstance != null, "Decoy landing lacks dust or its model.");
            GameObject model = visual.SettledInstance;
            Require(model.transform.IsChildOf(decoy.transform) && Near(model.transform.lossyScale.x, 1), "Decoy model is detached or inherits its taunt radius scale.");
            Require(inside.FSM.Targets.Contains(decoy.gameObject) && !outside.FSM.Targets.Contains(decoy.gameObject), "Decoy taunt range is incorrect.");
            entry.observedDiameter = decoy.GetComponent<SphereCollider>().radius * decoy.transform.lossyScale.x * 2;
            Require(Near(entry.observedDiameter, 12), "Decoy taunt diameter changed.");
            entry.effectApplied = entry.outsideUnaffected = true;
            PositionCamera(point);
            yield return new WaitForSeconds(.08f);
            Capture("6_Impact");
            decoy.TakeDamage(new DamageInfo(1000));
            yield return null;
            Require(decoy == null && model == null && inside.FSM.Targets.Count == 0, "Destroyed decoy left its model or taunt target.");
            entry.tauntReleased = true;

            Cast(item, player, point);
            limit = Time.time + 4;
            while ((decoy = Decoy(item)) == null && Time.time < limit) yield return null;
            Require(decoy != null, "Decoy did not respawn.");
            model = decoy.GetComponentsInChildren<Renderer>(true).Select(r => r.gameObject).FirstOrDefault();
            Require(model != null, "Respawned Decoy has no visible model.");
            float landed = Time.time;
            while (decoy != null && Time.time - landed < 21) yield return null;
            entry.settledLifetime = Time.time - landed;
            Require(decoy == null && model == null && Mathf.Abs(entry.settledLifetime - 20) < .3f && inside.FSM.Targets.Count == 0,
                "Decoy natural lifetime did not remove its model and taunt.");

            Cast(item, player, point);
            limit = Time.time + 4;
            while ((visual = Visual(item)) == null && Time.time < limit) yield return null;
            Require(visual != null && Decoy(item) == null, "Decoy cancellation check did not reach its incoming phase.");
            item.Unequip();
            yield return new WaitForSeconds(1.1f);
            Require(visual == null && Visual(item) == null && Decoy(item) == null && Objects(item).All(go => go == null), "Decoy landed after Unequip.");
            entry.cleanedOnUnequip = true;
            Object.Destroy(inside.gameObject);
            Object.Destroy(outside.gameObject);
            WriteReport();
            Debug.Log("[ItemCastVfxChecks] 6 passed: fall, dust, textured model, taunt, damage, 20-second lifetime and incoming cancellation.");
        }

        private static Item Custom(string id, ItemEffect effect, float diameter) => new Item(id,
            new ItemMeta("CastVFX Check", "", 0, 0, 1, ItemKind.Active, ItemTarget.Enemies,
                preview: new ItemPreview(ItemPreviewType.Circle, diameter), vfxPath: ItemCatalog.GetMeta(id).VfxPath),
            new ItemRuntime(ItemTrigger.SkillUse, effect));

        private static void Equip(Item item, PlayerStatus player)
        {
            equippedItems.Add(item);
            item.Equip(player, 1 << LayerMask.NameToLayer("Enemy"));
        }

        private static MeleeEnemy Enemy(Vector3 position)
        {
            var host = new GameObject("CastVFX Target");
            host.SetActive(false);
            host.layer = LayerMask.NameToLayer("Enemy");
            host.transform.position = position;
            host.AddComponent<EnemyStatus>().ConfigureSpawnStats(1000, 0, 0);
            host.AddComponent<SphereCollider>().radius = .25f;
            var enemy = host.AddComponent<MeleeEnemy>();
            host.SetActive(true);
            enemy.PrepareForTesting(null);
            BuffManager.Instance.SetCondition(enemy.Status, frozenTargetSource, BuffStat.Stun, 100);
            return enemy;
        }

        private static void Cast(Item item, PlayerStatus player, Vector3 point) =>
            item.Runtime.Execute(new ItemEvent(ItemTrigger.SkillUse, player, point, skillItem: item, cast: new SkillCast()));

        private static List<GameObject> Objects(Item item) =>
            (List<GameObject>)typeof(ItemRuntime).GetField("objects", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(item.Runtime);
        private static ItemCastVfx Visual(Item item) => Objects(item).Where(go => go != null)
            .Select(go => go.GetComponent<ItemCastVfx>()).FirstOrDefault(component => component != null);
        private static DecoyController Decoy(Item item) => Objects(item).Where(go => go != null)
            .Select(go => go.GetComponent<DecoyController>()).FirstOrDefault(component => component != null);

        private static void PositionCamera(Vector3 point)
        {
            // PJS/Review의 시점과 같은 60도 하향 perspective로 카메라 상대 연출을 검사한다.
            captureCamera.transform.position = point + new Vector3(0, 30, 17.320508f);
            captureCamera.transform.LookAt(point);
        }

        private static Vector3 LinePoint(LineRenderer line, int index) => line.useWorldSpace
            ? line.GetPosition(index) : line.transform.TransformPoint(line.GetPosition(index));

        private static float CheckFirepit(ItemCastVfx visual, float diameter)
        {
            float actual = 0;
            foreach (string name in new[] { "Firepit_MoltenBed", "Firepit_ScorchedRim" })
            {
                ParticleSystem bed = visual.ImpactInstance.GetComponentsInChildren<ParticleSystem>(true).Single(particle => particle.name == name);
                var renderer = bed.GetComponent<ParticleSystemRenderer>();
                Require(renderer.renderMode == ParticleSystemRenderMode.Mesh && renderer.mesh != null && !bed.main.startSize3D,
                    name + " is not a calibrated ground mesh particle.");
                actual = renderer.mesh.bounds.size.x * bed.main.startSize.constant * bed.transform.lossyScale.x;
                float z = renderer.mesh.bounds.size.z * bed.main.startSize.constant * bed.transform.lossyScale.z;
                Require(Near(actual, diameter) && Near(z, diameter), name + " footprint does not match the actual effect diameter.");
            }
            return actual;
        }

        private static void Capture(string name)
        {
            RenderTexture previous = RenderTexture.active;
            var frame = new Texture2D(captureTarget.width, captureTarget.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = captureTarget;
                frame.ReadPixels(new Rect(0, 0, frame.width, frame.height), 0, 0);
                frame.Apply();
                File.WriteAllBytes(Captures + "/" + name + ".png", frame.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.Destroy(frame); }
        }

        private static void CheckSpeed(Item item, string field, GameObject instance, float multiplier)
        {
            var root = Resources.Load<GameObject>(item.Meta.VfxPath).GetComponent<ItemCastVfx>();
            var template = new SerializedObject(root).FindProperty(field).objectReferenceValue as GameObject;
            ParticleSystem[] expected = template.GetComponentsInChildren<ParticleSystem>(true);
            ParticleSystem[] actual = instance.GetComponentsInChildren<ParticleSystem>(true);
            Require(actual.Length == expected.Length, item.ID + " changed its stage particle hierarchy.");
            for (int i = 0; i < expected.Length; i++)
                Require(Near(actual[i].main.simulationSpeed, expected[i].main.simulationSpeed * multiplier),
                    item.ID + "/" + actual[i].name + " playback speed does not follow Effect settings.");
        }

        private static void Finish(Exception error)
        {
            SessionState.SetBool(Running, false);
            deadline = 0;
            foreach (Item item in equippedItems) item.Unequip();
            equippedItems.Clear();
            if (report == null) report = new Report();
            report.result = error == null ? "passed" : "failed";
            report.error = error?.ToString() ?? "";
            WriteReport();
            if (EditorApplication.isPlaying)
            {
                Application.runInBackground = previousRunInBackground;
                if (captureCamera != null) captureCamera.targetTexture = null;
                if (captureTarget != null) { captureTarget.Release(); Object.Destroy(captureTarget); }
            }
            if (error == null) Debug.Log("[ItemCastVfxChecks] PASS: staged timing, descent, scales, playback speeds, hit ranges, elements, Decoy model/taunt/lifetime and cleanup.");
            else Debug.LogException(error);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        private static void WriteReport() => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        private static bool Near(float a, float b) => Mathf.Abs(a - b) < .02f;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
