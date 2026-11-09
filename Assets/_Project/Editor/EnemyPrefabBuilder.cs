using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UJam.Runtime.Enemy;
using UJam.Runtime.Enemy.Movement;
using UJam.Runtime.Enemy.Projectiles;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UJam.Editor
{
    // Rebuilds only the eight production enemies; legacy prefabs and FBX sources stay intact.
    [InitializeOnLoad]
    public static class EnemyPrefabBuilder
    {
        private const string Art = "Assets/_Project/Art/EnemyChar/";
        private const string Prefabs = "Assets/_Project/Prefabs/Enemy/";
        private const string Request = "Library/BuildEnemyPrefabs.request";
        private const string VfxRequest = "Library/BuildWitchDeathVfx.request";
        private const string TransitionRequest = "Library/ConfigureEnemyTransitions.request";
        private const string ReportPath = "Library/EnemyPrefabBuildReport.json";
        private static readonly string[] States = { "Idle", "Move", "Attack", "Death", "Spawn", "Summon" };
        private static readonly string[] WitchBody = { "Witch_Head", "Witch_Neck", "Witch_Hand_Right", "Witch_Hand_Left", "Witch_Eyes", "Witch_Robe", "Witch_Sleeve_Right", "Witch_Sleeve_Left", "Witch_Pendant" };
        private static readonly string[] WitchStaff = { "Witch_Staff_Wood", "Witch_Staff_Silver", "Witch_Staff_Crystal_20_Faces" };

        private sealed class Spec
        {
            public string name, folder;
            public Type behaviour;
            public float speed, range, attackSpeed, damage, hp, hitFrame;
            public int credits;
            public Vector3 expectedSize;

            public Spec(string name, string folder, Type behaviour, float speed, float range,
                float attackSpeed, float damage, float hp, int credits, float hitFrame, Vector3 expectedSize)
            {
                this.name = name; this.folder = folder; this.behaviour = behaviour;
                this.speed = speed; this.range = range; this.attackSpeed = attackSpeed;
                this.damage = damage; this.hp = hp; this.credits = credits;
                this.hitFrame = hitFrame; this.expectedSize = expectedSize;
            }
        }

        private static readonly Spec[] Specs =
        {
            new Spec("Golem", "Golem", typeof(MeleeEnemy), .5f, 1, 1, 20, 200, 100, 18, new Vector3(4, 3.608423f, 4)),
            new Spec("Big_Golem", "Golem", typeof(MeleeEnemy), .5f, 1, 1, 30, 500, 300, 18, new Vector3(5, 5.112474f, 5)),
            new Spec("Eyebeast", "Eyebeast", typeof(RangedEnemy), .5f, 3, 1, 80, 120, 100, 15, new Vector3(2.399289f, 2.754892f, 2.142080f)),
            new Spec("Sniper_Eyebeast", "Eyebeast", typeof(SniperEnemy), .5f, 5, .8f, 100, 100, 150, 30, new Vector3(2.399289f, 2.754892f, 2.142080f)),
            new Spec("Witch", "Witch", typeof(WitchEnemy), .5f, 2, 1, 50, 150, 200, 33, new Vector3(2.021932f, 4.119852f, 2.441023f)),
            new Spec("Skeleton", "Skeleton", typeof(SkeletonEnemy), 1, 1, 2, 80, 80, 20, 12, new Vector3(1.160144f, 2.747421f, 2.044519f)),
            new Spec("Slime", "Slime", typeof(SlimeEnemy), .4f, 1, 1, 80, 150, 200, 27, new Vector3(3, 2.775f, 3)),
            new Spec("Rollbug", "RollBug", typeof(RollbugEnemy), 2, .5f, 0, 200, 20, 500, -1, new Vector3(1.44f, 2, 1.977733f))
        };

        [Serializable]
        private sealed class BuildReport
        {
            public string result, error;
            public int validatedClipCount, validatedEventCount;
            public List<Entry> enemies = new List<Entry>();
        }

        [Serializable]
        private sealed class Entry
        {
            public string name, prefab, colliderPose;
            public Vector3 colliderCenter, colliderSize, expectedSize;
            public string[] clips;
            public int rendererCount, validatedClipCount, validatedEventCount;
            public bool validated;
        }

        [Serializable]
        private sealed class VfxReport
        {
            public string result, error, shader, graphicsDevice;
            public int bodyRenderers, staffRenderers, dustEmitters, materials;
            public bool shaderSupported, palettePreserved, modelTransformPreserved;
        }

        static EnemyPrefabBuilder()
        {
            EditorApplication.update += RunRequestedBuild;
        }

        private static void RunRequestedBuild()
        {
            if ((!File.Exists(Request) && !File.Exists(VfxRequest) && !File.Exists(TransitionRequest)) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool fullBuild = File.Exists(Request);
            bool transitions = !fullBuild && File.Exists(TransitionRequest);
            try { File.Delete(fullBuild ? Request : transitions ? TransitionRequest : VfxRequest); }
            catch (IOException) { return; }
            try { if (fullBuild) BuildAll(); else if (transitions) ConfigureAnimationTransitions(); else BuildWitchDeathVfx(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        [MenuItem("UJam/Enemies/Rebuild Production Prefabs")]
        public static void BuildAll()
        {
            var report = new BuildReport { result = "building" };
            try
            {
                Require(!AnimationMode.InAnimationMode(), "Exit animation preview before rebuilding enemies.");
                ValidateMovement();
                var linear = LoadOrCreate<LinearMovement>(Art + "Eyebeast/Enemy_LinearMovement.asset");
                var eyeProjectile = BuildProjectile("Eyebeast", "Eyebeast", new Color(.7f, .12f, 1), linear);
                var witchProjectile = BuildProjectile("Witch", "Witch", new Color(.65f, .8f, .92f), linear);
                foreach (Spec spec in Specs)
                    report.enemies.Add(BuildEnemy(spec, spec.name == "Witch" ? witchProjectile : eyeProjectile, linear));
                ConnectSummons();
                BuildWitchDeathVfx();
                ConfigureAnimationTransitions();
                EnemyVfxBuild.ConfigureAll();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                foreach (Spec spec in Specs)
                    ValidatePrefab(spec, report.enemies.Single(entry => entry.name == spec.name));
                report.validatedClipCount = report.enemies.Sum(entry => entry.validatedClipCount);
                report.validatedEventCount = report.enemies.Sum(entry => entry.validatedEventCount);
                Require(report.validatedClipCount == 32 && report.validatedEventCount >= 25,
                    "The production enemies must preserve 32 clips and all gameplay animation signals.");
                report.result = "passed";
                Debug.Log("[EnemyPrefabBuilder] Built and validated all eight production enemy prefabs. Big Golem damage is 30 (confirmed as 1.5 times Golem).");
            }
            catch (Exception exception)
            {
                report.result = "failed";
                report.error = exception.ToString();
                throw;
            }
            finally
            {
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            }
        }

        private static Entry BuildEnemy(Spec spec, Projectile projectile, LinearMovement linear)
        {
            string modelPath = Art + spec.folder + "/" + spec.name + ".fbx";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Require(source != null, "Missing model: " + modelPath);
            Dictionary<string, AnimationClip> clips = BuildClips(spec, modelPath);
            Require(clips.ContainsKey("Idle") && clips.ContainsKey("Move"), spec.name + " requires Idle and Move clips.");
            AnimatorController controller = BuildController(spec, clips);
            var root = new GameObject(spec.name);
            try
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                Require(Approximately(model.transform.localScale, Vector3.one), spec.name + " imported model root scale must remain one.");
                model.name = "Model";
                Animator animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                model.AddComponent<EnemyAnimation>();

                string pose = spec.name == "Rollbug" ? "Move" : "Idle";
                Bounds bounds = MeasureBounds(root.transform, model, clips[pose]);
                Require(Approximately(bounds.size, spec.expectedSize, .08f),
                    spec.name + " imported geometry differs from the documented actual size: " + bounds.size.ToString("F6"));
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = bounds.center;
                collider.size = bounds.size;
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezeRotation;
                int layer = LayerMask.NameToLayer("Enemy");
                Require(layer >= 0, "The Enemy layer is missing.");
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;

                EnemyStatus status = root.AddComponent<EnemyStatus>();
                EnemyMovement movement = spec.name == "Rollbug"
                    ? (EnemyMovement)root.AddComponent<RollbugMovement>() : root.AddComponent<GridColMovement>();
                EnemyBase enemy = (EnemyBase)root.AddComponent(spec.behaviour);
                SetObject(enemy, "_status", status);
                SetString(status, "_enemyName", spec.name.Replace('_', ' '));
                SetFloat(status, "_speed", spec.speed);
                SetFloat(status, "_attackRange", spec.range);
                SetFloat(status, "_attackSpeed", spec.attackSpeed);
                SetFloat(status, "_attackDamage", spec.damage);
                SetFloat(status, "_maxHealth", spec.hp);
                SetInt(status, "_credits", spec.credits);
                SetObject(status, "_movement", movement);
                if (enemy is RangedEnemy)
                {
                    string anchorName = spec.name == "Witch" ? "Staff" : "Body";
                    Transform anchor = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .SelectMany(renderer => renderer.bones).FirstOrDefault(t => t != null && t.name.Equals(anchorName, StringComparison.OrdinalIgnoreCase))
                        ?? model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals(anchorName, StringComparison.OrdinalIgnoreCase))
                        ?? model.transform;
                    var point = new GameObject("FirePoint").transform;
                    point.SetParent(anchor, false);
                    point.position = root.transform.TransformPoint(new Vector3(bounds.center.x,
                        bounds.center.y + bounds.extents.y * .15f, bounds.max.z));
                    point.gameObject.layer = layer;
                    SetObject(enemy, "_projectilePrefab", projectile);
                    SetObject(enemy, "_projectileMovement", linear);
                    SetObject(enemy, "_firePoint", point);
                    SetFloat(enemy, "_projectileSpeed", 5);
                }
                string prefabPath = Prefabs + spec.name + ".prefab";
                Require(PrefabUtility.SaveAsPrefabAsset(root, prefabPath) != null, "Could not save " + prefabPath);
                return new Entry
                {
                    name = spec.name, prefab = prefabPath, colliderPose = pose + " local frame 0",
                    colliderCenter = bounds.center, colliderSize = bounds.size, expectedSize = spec.expectedSize,
                    clips = clips.Keys.ToArray(), rendererCount = model.GetComponentsInChildren<Renderer>(true).Length
                };
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Dictionary<string, AnimationClip> BuildClips(Spec spec, string modelPath)
        {
            AnimationClip[] imported = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            var result = new Dictionary<string, AnimationClip>();
            foreach (string state in States)
            {
                AnimationClip original = imported.FirstOrDefault(clip => clip.name == state)
                    ?? imported.FirstOrDefault(clip => clip.name.EndsWith("|" + state, StringComparison.Ordinal));
                if (original == null) continue;
                Require(Mathf.Abs(original.length - ExpectedDuration(spec.name, state)) < .02f,
                    spec.name + " " + state + " imported duration is " + original.length + ", expected " + ExpectedDuration(spec.name, state));
                var copy = Object.Instantiate(original);
                copy.name = state;
                copy.frameRate = 60;
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
                settings.loopTime = state == "Idle" || state == "Move";
                AnimationUtility.SetAnimationClipSettings(copy, settings);
                var events = EnemyVfxBuild.Events(spec.name, state, copy.length, spec.hitFrame);
                AnimationUtility.SetAnimationEvents(copy, events.ToArray());
                string path = Art + spec.folder + "/" + spec.name + "_" + state + ".anim";
                AnimationClip saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (saved == null) { AssetDatabase.CreateAsset(copy, path); saved = copy; }
                else { EditorUtility.CopySerialized(copy, saved); Object.DestroyImmediate(copy); EditorUtility.SetDirty(saved); }
                result.Add(state, saved);
            }
            int expected = spec.name == "Rollbug" ? 2 : spec.name == "Witch" || spec.name == "Skeleton" ? 5 : 4;
            Require(result.Count == expected, spec.name + " imported clip count mismatch. Found: " + string.Join(", ", imported.Select(c => c.name)));
            return result;
        }

        private static void AddEvent(List<AnimationEvent> events, string signal, float time)
        {
            events.Add(new AnimationEvent { functionName = "OnAnimationSignal", stringParameter = signal, time = time });
        }

        private static float ExpectedDuration(string enemy, string state)
        {
            switch (state)
            {
                case "Idle": return enemy == "Golem" || enemy == "Big_Golem" ? 4 : enemy == "Slime" || enemy == "Rollbug" ? 1 : 2;
                case "Move":
                    switch (enemy)
                    {
                        case "Golem": return 1.2f;
                        case "Witch": return .8f;
                        case "Skeleton": return .6f;
                        case "Slime": return 1;
                        case "Rollbug": return 32 / 60f;
                        default: return 1.5f;
                    }
                case "Attack": return enemy == "Sniper_Eyebeast" ? 1.25f : enemy == "Skeleton" ? .5f : 1;
                case "Death":
                    switch (enemy)
                    {
                        case "Golem": case "Big_Golem": return 5;
                        case "Witch": return 3;
                        case "Skeleton": return .6f;
                        case "Slime": return .9f;
                        default: return 2;
                    }
                default: return 1.5f;
            }
        }

        private static void ValidateMovement()
        {
            var position = new Vector3(15, 0, 3);
            var direction = new Vector3(.6f, 0, .8f);
            RollbugMovement.ReflectPosition(ref position, ref direction, -15, 15);
            Require(direction.x < 0 && Mathf.Approximately(direction.z, .8f) && position.z == 3, "Wall reflection changed forward movement.");
            position.x = 50; direction.x = .6f;
            RollbugMovement.ReflectPosition(ref position, ref direction, -15, 15);
            Require(Mathf.Approximately(position.x, -10) && direction.x > 0, "Two-wall overshoot reflection failed.");
            position.x = 80; direction.x = .6f;
            RollbugMovement.ReflectPosition(ref position, ref direction, -15, 15);
            Require(Mathf.Approximately(position.x, 10) && direction.x < 0, "Three-wall overshoot reflection failed.");
            Require(RollbugMovement.TryRangeEntry(new Vector2(-2, 0), new Vector2(2, 0), .5f, out float fraction)
                && Mathf.Abs(fraction - .375f) < .0001f, "Range sweep missed an attack between frames.");
            Require(!RollbugMovement.TryRangeEntry(new Vector2(-2, 1), new Vector2(2, 1), .5f, out _), "Range sweep accepted an outside segment.");
            Require(RollbugMovement.TryRangeEntry(Vector2.zero, Vector2.right, .5f, out fraction) && fraction == 0,
                "Range sweep did not immediately accept an inside start.");
        }

        private static AnimatorController BuildController(Spec spec, Dictionary<string, AnimationClip> clips)
        {
            string path = Art + spec.folder + "/" + spec.name + ".controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path)
                ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState state in machine.states) machine.RemoveState(state.state);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (var pair in clips)
            {
                AnimatorState state = machine.AddState(pair.Key);
                state.motion = pair.Value;
                if (pair.Key == "Idle") machine.defaultState = state;
            }
            ConfigureStateSpeeds(controller);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureStateSpeeds(AnimatorController controller)
        {
            controller.parameters = controller.parameters.Where(parameter => parameter.name != "PlaybackSpeed").ToArray();
            foreach (ChildAnimatorState entry in controller.layers[0].stateMachine.states)
            {
                string parameter = entry.state.name + "Speed";
                if (!controller.parameters.Any(value => value.name == parameter))
                    controller.AddParameter(new AnimatorControllerParameter { name = parameter,
                        type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
                entry.state.speed = 1f;
                entry.state.speedParameter = parameter;
                entry.state.speedParameterActive = true;
            }
            EditorUtility.SetDirty(controller);
        }

        [MenuItem("UJam/Enemies/Configure Animation Transitions")]
        public static void ConfigureAnimationTransitions()
        {
            foreach (Spec spec in Specs)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Art + spec.folder + "/" + spec.name + ".controller");
                Require(controller != null, spec.name + " controller is missing.");
                ConfigureStateSpeeds(controller);
            }
            ConfigureRollbugEffects();
            AssetDatabase.SaveAssets();
            File.WriteAllText("Library/EnemyTransitionConfiguration.json", "{\"result\":\"passed\",\"controllers\":8,\"stateSpeedParameters\":true,\"rollbugEffects\":true}");
        }

        private static void ConfigureRollbugEffects()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(Prefabs + "Rollbug.prefab");
            try
            {
                RollbugVfxSetup.Configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "Rollbug.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static GameObject BuildRollbugBurst(string name, Material material, bool detonation)
        {
            var root = new GameObject(name);
            try
            {
                root.layer = LayerMask.NameToLayer("Ignore Raycast");
                var system = root.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = system.main;
                main.loop = false;
                main.duration = .3f;
                main.playOnAwake = true;
                main.stopAction = ParticleSystemStopAction.Destroy;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = new ParticleSystem.MinMaxCurve(.25f, detonation ? .65f : .4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(detonation ? 1.5f : .5f, detonation ? 4f : 1.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(.1f, detonation ? .45f : .2f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .22f, .03f, 1f), new Color(1f, .8f, .15f, 1f));
                var emission = system.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)(detonation ? 70 : 25)) });
                var shape = system.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = .3f;
                shape.position = Vector3.up;
                var color = system.colorOverLifetime;
                color.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.25f, .12f, .1f), 1) },
                    new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
                color.color = gradient;
                var size = system.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                return PrefabUtility.SaveAsPrefabAsset(root, Art + "RollBug/" + name + ".prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Bounds MeasureBounds(Transform root, GameObject model, AnimationClip pose)
        {
            bool hasVertex = false;
            var bounds = new Bounds();
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(model, pose, 0);
                AnimationMode.EndSampling();
                foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var baked = new Mesh();
                    try { renderer.BakeMesh(baked); AddVertices(baked, root.worldToLocalMatrix * renderer.transform.localToWorldMatrix, ref bounds, ref hasVertex); }
                    finally { Object.DestroyImmediate(baked); }
                }
                foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null) AddVertices(filter.sharedMesh,
                        root.worldToLocalMatrix * filter.transform.localToWorldMatrix, ref bounds, ref hasVertex);
            }
            finally { AnimationMode.StopAnimationMode(); }
            Require(hasVertex && bounds.size.x > 0 && bounds.size.y > 0 && bounds.size.z > 0, model.name + " has no measurable geometry.");
            return bounds;
        }

        private static void AddVertices(Mesh mesh, Matrix4x4 matrix, ref Bounds bounds, ref bool hasVertex)
        {
            using (Mesh.MeshDataArray data = MeshUtility.AcquireReadOnlyMeshData(mesh))
            using (var vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Temp))
            {
                data[0].GetVertices(vertices);
                foreach (Vector3 vertex in vertices)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(vertex);
                    if (!hasVertex) { bounds = new Bounds(point, Vector3.zero); hasVertex = true; }
                    else bounds.Encapsulate(point);
                }
            }
        }

        private static Projectile BuildProjectile(string name, string folder, Color color, LinearMovement movement)
        {
            string directory = Art + folder + "/";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(directory + name + "_Projectile.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, directory + name + "_Projectile.mat");
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            var root = new GameObject(name + "_Projectile");
            try
            {
                root.layer = LayerMask.NameToLayer("Projectile");
                Projectile projectile = root.AddComponent<Projectile>();
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "Orb";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = Vector3.one * .25f;
                visual.layer = root.layer;
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.GetComponent<Renderer>().sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(root, directory + name + "_Projectile.prefab").GetComponent<Projectile>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void ConnectSummons()
        {
            GameObject witch = PrefabUtility.LoadPrefabContents(Prefabs + "Witch.prefab");
            try
            {
                SetObject(witch.GetComponent<WitchEnemy>(), "_skeletonPrefab",
                    AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Skeleton.prefab").GetComponent<SkeletonEnemy>());
                PrefabUtility.SaveAsPrefabAsset(witch, Prefabs + "Witch.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(witch); }
            GameObject slime = PrefabUtility.LoadPrefabContents(Prefabs + "Slime.prefab");
            try
            {
                SetObject(slime.GetComponent<SlimeEnemy>(), "_splitPrefab",
                    AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Slime.prefab").GetComponent<SlimeEnemy>());
                PrefabUtility.SaveAsPrefabAsset(slime, Prefabs + "Slime.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(slime); }
        }

        private static void ValidatePrefab(Spec spec, Entry entry)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefab);
            Require(prefab != null && prefab.GetComponent(spec.behaviour) != null, spec.name + " missing behaviour.");
            Require(prefab.transform.localPosition == Vector3.zero && prefab.transform.localScale == Vector3.one, spec.name + " root transform changed.");
            Transform model = prefab.transform.Find("Model");
            Require(model != null && model.localPosition == Vector3.zero && Approximately(model.localScale, Vector3.one), spec.name + " model transform changed.");
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, spec.name + " contains a missing script.");
            BoxCollider collider = prefab.GetComponent<BoxCollider>();
            Require(collider != null && Approximately(collider.size, entry.colliderSize, .001f), spec.name + " collider mismatch.");
            EnemyStatus status = prefab.GetComponent<EnemyStatus>();
            var serialized = new SerializedObject(status);
            Require(serialized.FindProperty("_movement").objectReferenceValue != null, spec.name + " movement missing.");
            Require(Mathf.Approximately(serialized.FindProperty("_maxHealth").floatValue, spec.hp)
                && Mathf.Approximately(serialized.FindProperty("_attackDamage").floatValue, spec.damage)
                && Mathf.Approximately(serialized.FindProperty("_speed").floatValue, spec.speed)
                && Mathf.Approximately(serialized.FindProperty("_attackRange").floatValue, spec.range)
                && Mathf.Approximately(serialized.FindProperty("_attackSpeed").floatValue, spec.attackSpeed)
                && serialized.FindProperty("_credits").intValue == spec.credits, spec.name + " status mismatch.");
            Animator animator = model.GetComponent<Animator>();
            Require(animator != null && animator.runtimeAnimatorController != null && !animator.applyRootMotion, spec.name + " animator missing.");
            Require(model.GetComponent<EnemyAnimation>() != null, spec.name + " animation event receiver missing.");
            var controller = animator.runtimeAnimatorController as AnimatorController;
            Require(controller != null, spec.name + " controller has an unexpected type.");
            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            Require(states.Length == entry.clips.Length, spec.name + " controller state count mismatch.");
            foreach (string name in entry.clips)
            {
                AnimatorState state = states.SingleOrDefault(candidate => candidate.state.name == name).state;
                Require(state != null && state.motion is AnimationClip, spec.name + " missing clip for state " + name);
                var clip = (AnimationClip)state.motion;
                Require(state.speedParameterActive && state.speedParameter == name + "Speed", spec.name + " " + name + " speed control missing.");
                Require(Mathf.Abs(clip.length - ExpectedDuration(spec.name, name)) < .02f, spec.name + " " + name + " duration mismatch.");
                Require(AnimationUtility.GetAnimationClipSettings(clip).loopTime == (name == "Idle" || name == "Move"), spec.name + " " + name + " loop flag mismatch.");
                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
                var expectedEvents = EnemyVfxBuild.Events(spec.name, name, clip.length, spec.hitFrame);
                Require(events.Length == expectedEvents.Count, spec.name + " " + name + " event count mismatch.");
                foreach (AnimationEvent expected in expectedEvents)
                    Require(events.Count(e => e.functionName == expected.functionName && e.stringParameter == expected.stringParameter
                        && Mathf.Abs(e.time - expected.time) < .0001f) == 1, spec.name + " " + name + " missing or duplicated event " + expected.stringParameter);
                entry.validatedClipCount++;
                entry.validatedEventCount += events.Length;
            }
            if (prefab.GetComponent<RangedEnemy>() != null)
            {
                var ranged = new SerializedObject(prefab.GetComponent<RangedEnemy>());
                foreach (string field in new[] { "_projectilePrefab", "_projectileMovement", "_firePoint" })
                    Require(ranged.FindProperty(field).objectReferenceValue != null, spec.name + " missing " + field);
            }
            if (spec.name == "Witch") Require(new SerializedObject(prefab.GetComponent<WitchEnemy>()).FindProperty("_skeletonPrefab").objectReferenceValue != null, "Witch skeleton prefab missing.");
            if (spec.name == "Slime") Require(new SerializedObject(prefab.GetComponent<SlimeEnemy>()).FindProperty("_splitPrefab").objectReferenceValue != null, "Slime split prefab missing.");
            entry.validated = true;
        }

        [MenuItem("UJam/Enemies/Apply Witch Death VFX")]
        public static void BuildWitchDeathVfx()
        {
            var report = new VfxReport { result = "building" };
            GameObject root = null;
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Build Witch VFX outside Play mode.");
                Shader shader = Shader.Find("UJam/Enemy/Witch Dissolve");
                Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Witch dissolve shader is missing or contains compiler errors.");
                Require(shader.isSupported || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null, "Witch dissolve shader is unsupported.");
                report.shader = shader.name;
                report.shaderSupported = shader.isSupported;
                report.graphicsDevice = SystemInfo.graphicsDeviceType.ToString();
                root = PrefabUtility.LoadPrefabContents(Prefabs + "Witch.prefab");
                Transform model = root.transform.Find("Model");
                Require(model != null, "Witch model root is missing.");
                Vector3 position = model.localPosition, scale = model.localScale;
                Quaternion rotation = model.localRotation;
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                Renderer[] body = WitchBody.Select(name => renderers.SingleOrDefault(renderer => renderer.name == name)).ToArray();
                Renderer[] staff = WitchStaff.Select(name => renderers.SingleOrDefault(renderer => renderer.name == name)).ToArray();
                Require(body.All(renderer => renderer is SkinnedMeshRenderer) && staff.All(renderer => renderer is SkinnedMeshRenderer)
                    && renderers.Length == 12, "Witch must have the documented nine body and three staff skinned renderers.");

                var converted = new Dictionary<Material, Material>();
                foreach (Renderer renderer in body.Concat(staff))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int index = 0; index < materials.Length; index++)
                    {
                        Material source = materials[index];
                        Require(source != null, renderer.name + " has a missing source material.");
                        if (!converted.TryGetValue(source, out Material material))
                        {
                            Texture texture = TextureOf(source);
                            Require(texture != null, renderer.name + " has no palette texture to preserve.");
                            Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor")
                                : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                            string textureProperty = source.HasProperty("_BaseMap") && source.GetTexture("_BaseMap") != null ? "_BaseMap" : "_MainTex";
                            Vector2 uvScale = source.HasProperty(textureProperty) ? source.GetTextureScale(textureProperty) : Vector2.one;
                            Vector2 uvOffset = source.HasProperty(textureProperty) ? source.GetTextureOffset(textureProperty) : Vector2.zero;
                            bool alreadyConverted = source.shader == shader && AssetDatabase.GetAssetPath(source).StartsWith(Art + "Witch/Witch_Dissolve_", StringComparison.Ordinal);
                            string path = Art + "Witch/Witch_Dissolve_" + converted.Count + ".mat";
                            material = alreadyConverted ? source : AssetDatabase.LoadAssetAtPath<Material>(path);
                            if (material == null)
                            {
                                material = new Material(source);
                                AssetDatabase.CreateAsset(material, path);
                            }
                            material.shader = shader;
                            material.SetTexture("_BaseMap", texture);
                            material.SetTextureScale("_BaseMap", uvScale);
                            material.SetTextureOffset("_BaseMap", uvOffset);
                            material.SetColor("_BaseColor", color);
                            material.SetFloat("_Dissolve", 0);
                            material.SetFloat("_NoiseScale", 8);
                            material.SetFloat("_EdgeWidth", .045f);
                            material.SetColor("_EdgeColor", new Color(.55f, .2f, .7f));
                            material.renderQueue = -1;
                            EditorUtility.SetDirty(material);
                            Require(TextureOf(material) == texture && material.GetColor("_BaseColor") == color,
                                renderer.name + " palette or color changed while assigning dissolve.");
                            converted.Add(source, material);
                        }
                        materials[index] = material;
                    }
                    renderer.sharedMaterials = materials;
                    renderer.enabled = true;
                }

                Transform effects = root.transform.Find("DeathVFX");
                if (effects == null)
                {
                    effects = new GameObject("DeathVFX").transform;
                    effects.SetParent(root.transform, false);
                }
                effects.gameObject.layer = root.layer;
                Material dustMaterial = BuildAshMaterial();
                ParticleSystem[] bodyDust = body.Select(renderer => BuildAshEmitter(effects, (SkinnedMeshRenderer)renderer, dustMaterial, false)).ToArray();
                ParticleSystem[] staffDust = staff.Select(renderer => BuildAshEmitter(effects, (SkinnedMeshRenderer)renderer, dustMaterial, true)).ToArray();
                WitchDeathVfx driver = root.GetComponent<WitchDeathVfx>();
                if (driver == null) driver = root.AddComponent<WitchDeathVfx>();
                SetObjects(driver, "_bodyRenderers", body);
                SetObjects(driver, "_staffRenderers", staff);
                SetObjects(driver, "_bodyDust", bodyDust);
                SetObjects(driver, "_staffDust", staffDust);
                driver.ResetEffect();
                Require(model.localPosition == position && model.localScale == scale && model.localRotation == rotation,
                    "Witch VFX changed the source model transform.");
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Witch VFX contains a missing script.");
                Require(body.All(renderer => renderer.sharedMaterials.All(material => material.shader == shader && TextureOf(material) != null))
                    && staff.All(renderer => renderer.sharedMaterials.All(material => material.shader == shader && TextureOf(material) != null)), "Witch material assignments are incomplete.");
                Require(PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "Witch.prefab") != null, "Could not save Witch VFX prefab.");
                AssetDatabase.SaveAssets();
                report.bodyRenderers = body.Length;
                report.staffRenderers = staff.Length;
                report.dustEmitters = bodyDust.Length + staffDust.Length;
                report.materials = converted.Values.Distinct().Count();
                report.palettePreserved = report.modelTransformPreserved = true;
                report.result = "passed";
                Debug.Log("[EnemyPrefabBuilder] Applied Witch dissolve to nine body and three staff renderers with twelve skinned-surface ash emitters.");
            }
            catch (Exception exception)
            {
                report.result = "failed";
                report.error = exception.ToString();
                throw;
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
                File.WriteAllText("Library/WitchDeathVfxBuildReport.json", JsonUtility.ToJson(report, true));
            }
        }

        private static Texture TextureOf(Material material)
        {
            Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
            if (texture == null && material.HasProperty("_MainTex")) texture = material.GetTexture("_MainTex");
            return texture;
        }

        private static Material BuildAshMaterial()
        {
            const string texturePath = Art + "Witch/Witch_Ash.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                var image = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                try
                {
                    for (int y = 0; y < 32; y++)
                        for (int x = 0; x < 32; x++)
                        {
                            Vector2 point = new Vector2((x + .5f) / 16 - 1, (y + .5f) / 16 - 1);
                            float edge = .67f + Mathf.PerlinNoise(x * .19f, y * .19f) * .28f;
                            float alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((edge - point.magnitude) * 5));
                            image.SetPixel(x, y, new Color(1, 1, 1, alpha));
                        }
                    image.Apply();
                    File.WriteAllBytes(texturePath, image.EncodeToPNG());
                }
                finally { Object.DestroyImmediate(image); }
                AssetDatabase.ImportAsset(texturePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }
            const string path = Art + "Witch/Witch_Ash.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Require(shader != null && !ShaderUtil.ShaderHasError(shader), "URP particle shader is unavailable.");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static ParticleSystem BuildAshEmitter(Transform effects, SkinnedMeshRenderer surface, Material material, bool staff)
        {
            string name = (staff ? "Staff_" : "Body_") + surface.name;
            Transform child = effects.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(effects, false);
            }
            child.gameObject.layer = effects.gameObject.layer;
            ParticleSystem system = child.GetComponent<ParticleSystem>();
            if (system == null) system = child.gameObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = staff ? .5f : .7f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(staff ? .18f : .22f, staff ? .26f : .35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .18f);
            main.startSize = new ParticleSystem.MinMaxCurve(.025f, .065f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.4f, .32f, .48f, .9f), new Color(.72f, .68f, .75f, .85f));
            main.gravityModifier = 0;
            main.maxParticles = 128;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            Vector3 size = surface.bounds.size;
            emission.rateOverTime = Mathf.Clamp((size.x * size.y + size.y * size.z + size.x * size.z) * 24, 20, 160);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
            shape.skinnedMeshRenderer = surface;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.useMeshColors = false;
            shape.normalOffset = .015f;
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-.14f, .14f);
            velocity.y = new ParticleSystem.MinMaxCurve(.2f, .5f);
            velocity.z = new ParticleSystem.MinMaxCurve(-.14f, .14f);
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = .06f;
            noise.frequency = 1.3f;
            noise.scrollSpeed = .25f;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.8f, .75f, .86f), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(.6f, .55f), new GradientAlphaKey(0, 1) });
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = gradient;
            ParticleSystem.SizeOverLifetimeModule scale = system.sizeOverLifetime;
            scale.enabled = true;
            scale.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .7f), new Keyframe(.12f, 1), new Keyframe(1, 0)));
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Require(!main.playOnAwake && !emission.enabled && shape.skinnedMeshRenderer == surface, "Ash emitter configuration failed.");
            return system;
        }

        private static void SetObjects(Object target, string field, IEnumerable<Object> values)
        {
            Object[] array = values.ToArray();
            SerializedProperty property = Property(target, field, out SerializedObject serialized);
            property.arraySize = array.Length;
            for (int index = 0; index < array.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = array[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool Approximately(Vector3 a, Vector3 b, float tolerance = .00001f)
            => Mathf.Abs(a.x - b.x) <= tolerance && Mathf.Abs(a.y - b.y) <= tolerance && Mathf.Abs(a.z - b.z) <= tolerance;
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
        private static SerializedProperty Property(Object target, string field, out SerializedObject serialized)
        {
            serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            Require(property != null, target.GetType().Name + " has no serialized field " + field);
            return property;
        }
        private static void SetObject(Object target, string field, Object value)
        { Property(target, field, out SerializedObject serialized).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetFloat(Object target, string field, float value)
        { Property(target, field, out SerializedObject serialized).floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetInt(Object target, string field, int value)
        { Property(target, field, out SerializedObject serialized).intValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetString(Object target, string field, string value)
        { Property(target, field, out SerializedObject serialized).stringValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
