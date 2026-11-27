using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UJam.EditorTools
{
    /// <summary>
    /// 개발 보조용 Editor 브리지입니다. Temp/ClaudeBridge/cmd.txt에 적힌 명령을 한 줄씩 실행하고
    /// 결과를 result.txt, 콘솔 로그를 console.txt에 기록합니다. 게임 빌드에는 포함되지 않습니다.
    /// 작업이 끝나면 이 폴더(Assets/_Project/Editor/ClaudeBridge)를 삭제해도 게임에는 영향이 없습니다.
    /// </summary>
    [InitializeOnLoad]
    internal static class ClaudeEditorBridge
    {
        private const string Dir = "Temp/ClaudeBridge";
        private static double _nextPoll;
        private static string _pendingShot;
        private static int _pendingShotFrames;

        static ClaudeEditorBridge()
        {
            // 에셋 임포트 워커 프로세스에서도 InitializeOnLoad가 실행되므로 메인 에디터에서만 동작합니다.
            if (AssetDatabase.IsAssetImportWorkerProcess() || Application.isBatchMode) return;
            Directory.CreateDirectory(Dir);
            EditorApplication.update += Poll;
            Application.logMessageReceivedThreaded += OnLog;
            CompilationPipeline.assemblyCompilationFinished += OnCompiled;
            Result($"READY playing={EditorApplication.isPlaying} t={DateTime.Now:HH:mm:ss}");
        }

        private static void OnCompiled(string assembly, CompilerMessage[] messages)
        {
            foreach (CompilerMessage m in messages)
            {
                if (m.type != CompilerMessageType.Error) continue;
                File.AppendAllText(Path.Combine(Dir, "compile.txt"), $"{assembly}: {m.file}({m.line}): {m.message}\n");
            }
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss}] {type}: {condition}\n";
                if (type == LogType.Exception || type == LogType.Error)
                {
                    string[] st = (stackTrace ?? string.Empty).Split('\n');
                    line += string.Join("\n", st.Take(4).Select(s => "    " + s)) + "\n";
                }
                File.AppendAllText(Path.Combine(Dir, "console.txt"), line);
            }
            catch
            {
                // 로그 기록 실패는 무시한다.
            }
        }

        private static void Result(string line)
        {
            File.AppendAllText(Path.Combine(Dir, "result.txt"), line + "\n");
        }

        private static void Poll()
        {
            if (_pendingShot != null)
            {
                if (--_pendingShotFrames > 0) return;
                ScreenCapture.CaptureScreenshot(_pendingShot);
                Result($"OK shot {_pendingShot}");
                _pendingShot = null;
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 0.4;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string cmdPath = Path.Combine(Dir, "cmd.txt");
            if (!File.Exists(cmdPath)) return;

            List<string> lines = File.ReadAllLines(cmdPath).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count == 0)
            {
                File.Delete(cmdPath);
                return;
            }

            string command = lines[0].Trim();
            lines.RemoveAt(0);
            if (lines.Count == 0) File.Delete(cmdPath);
            else File.WriteAllLines(cmdPath, lines);

            try
            {
                Run(command);
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : e;
                Result($"ERR {command} :: {inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}");
            }
        }

        private static void Run(string command)
        {
            string[] a = Tokenize(command);
            switch (a[0])
            {
                case "ping":
                    Result($"OK ping playing={EditorApplication.isPlaying} scene={SceneManager.GetActiveScene().path}");
                    break;
                case "refresh":
                    Result("OK refresh (requested)");
                    AssetDatabase.Refresh();
                    break;
                case "menu":
                    Result($"{(EditorApplication.ExecuteMenuItem(a[1]) ? "OK" : "ERR")} menu {a[1]}");
                    break;
                case "call":
                    Result($"OK call {a[1]} -> {Call(a[1], a.Skip(2).ToArray())}");
                    break;
                case "open":
                    if (EditorSceneManager.GetActiveScene().isDirty)
                    {
                        Result($"ERR open {a[1]} :: 현재 씬에 저장되지 않은 변경이 있어 열지 않았습니다.");
                        break;
                    }
                    EditorSceneManager.OpenScene(a[1], OpenSceneMode.Single);
                    Result($"OK open {a[1]}");
                    break;
                case "play":
                    EditorApplication.isPlaying = true;
                    Result("OK play (requested)");
                    break;
                case "stop":
                    EditorApplication.isPlaying = false;
                    Result("OK stop (requested)");
                    break;
                case "shot":
                    _pendingShot = Path.GetFullPath(a[1]);
                    _pendingShotFrames = a.Length > 2 ? int.Parse(a[2]) : 3;
                    break;
                case "renderprefab":
                    RenderPrefab(a[1], a[2], int.Parse(a[3]), int.Parse(a[4]));
                    Result($"OK renderprefab {a[2]}");
                    break;
                case "renderscene":
                    RenderScene(a[1], a[2], int.Parse(a[3]), int.Parse(a[4]), a.Skip(5).ToArray());
                    Result($"OK renderscene {a[2]}");
                    break;
                case "dump":
                    File.WriteAllText(a[2], Dump(a[1]));
                    Result($"OK dump {a[2]}");
                    break;
                case "invoke":
                    Result($"OK invoke {a[1]}.{a[2]} -> {InvokeInstance(a[1], a[2], a.Skip(3).ToArray())}");
                    break;
                case "click":
                {
                    GameObject go = FindLoaded(a[1]);
                    Button button = go != null ? go.GetComponent<Button>() : null;
                    if (button == null) { Result($"ERR click {a[1]} :: Button not found"); break; }
                    button.onClick.Invoke();
                    Result($"OK click {a[1]} interactable={button.interactable}");
                    break;
                }
                case "state":
                {
                    GameObject go = FindLoaded(a[1]);
                    string extra = string.Empty;
                    if (go != null && go.transform is RectTransform rt) extra = $" pos={rt.anchoredPosition} scale={rt.localScale}";
                    TMP_Text text = go != null ? go.GetComponentInChildren<TMP_Text>(true) : null;
                    if (text != null) extra += $" text=\"{text.text}\"";
                    Graphic graphic = go != null ? go.GetComponent<Graphic>() : null;
                    if (graphic != null) extra += $" color={graphic.color} enabled={graphic.enabled} rect={graphic.rectTransform.rect.size}";
                    Selectable sel = go != null ? go.GetComponent<Selectable>() : null;
                    if (sel != null) extra += $" interactable={sel.interactable} effective={sel.IsInteractable()}";
                    CanvasGroup group = go != null ? go.GetComponentInParent<CanvasGroup>() : null;
                    if (group != null) extra += $" group={group.name}(a={group.alpha:0.00},i={group.interactable})";
                    Result($"OK state {a[1]} found={go != null} active={(go != null && go.activeInHierarchy)}{extra}");
                    break;
                }
                case "rendergame":
                    RenderGame(a[1], a.Length > 2 ? int.Parse(a[2]) : 1920, a.Length > 3 ? int.Parse(a[3]) : 1080);
                    Result($"OK rendergame {a[1]}");
                    break;
                case "timescale":
                    Time.timeScale = float.Parse(a[1]);
                    Result($"OK timescale {Time.timeScale}");
                    break;
                case "toggle":
                {
                    GameObject go = FindLoaded(a[1]);
                    if (go == null) { Result($"ERR toggle {a[1]} :: not found"); break; }
                    go.SetActive(false);
                    go.SetActive(true);
                    Result($"OK toggle {a[1]}");
                    break;
                }
                case "bg":
                    // 플레이 중에만 적용되는 런타임 값(프로젝트 설정은 바꾸지 않음): 에디터가 백그라운드여도 게임 루프 유지
                    Application.runInBackground = a[1] == "1";
                    Result($"OK bg {Application.runInBackground}");
                    break;
                default:
                    Result($"ERR unknown {command}");
                    break;
            }
        }

        private static string[] Tokenize(string command)
        {
            List<string> tokens = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool quoted = false;
            foreach (char c in command)
            {
                if (c == '"') { quoted = !quoted; continue; }
                if (c == ' ' && !quoted)
                {
                    if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                    continue;
                }
                sb.Append(c);
            }
            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens.ToArray();
        }

        private static string Call(string typeAndMethod, string[] args)
        {
            int dot = typeAndMethod.LastIndexOf('.');
            string typeName = typeAndMethod.Substring(0, dot);
            string methodName = typeAndMethod.Substring(dot + 1);
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType(typeName)).FirstOrDefault(t => t != null);
            if (type == null) throw new Exception($"Type not found: {typeName}");
            MethodInfo method = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == args.Length);
            if (method == null) throw new Exception($"Method not found: {methodName}/{args.Length}");
            object[] converted = method.GetParameters().Select((p, i) => Convert.ChangeType(args[i], p.ParameterType)).ToArray();
            object ret = method.Invoke(null, converted);
            return ret?.ToString() ?? "null";
        }

        private static GameObject FindLoaded(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == name) return t.gameObject;
                    }
                }
            }
            return null;
        }

        private static string InvokeInstance(string typeName, string methodName, string[] args)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType(typeName)).FirstOrDefault(t => t != null);
            if (type == null) throw new Exception($"Type not found: {typeName}");
            UnityEngine.Object target = UnityEngine.Object.FindAnyObjectByType(type, FindObjectsInactive.Include);
            if (target == null) throw new Exception($"Instance not found: {typeName}");
            MethodInfo method = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == args.Length);
            if (method != null)
            {
                object[] converted = method.GetParameters().Select((p, i) => Convert.ChangeType(args[i], p.ParameterType)).ToArray();
                return method.Invoke(target, converted)?.ToString() ?? "null";
            }
            PropertyInfo prop = type.GetProperty(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null) return prop.GetValue(target)?.ToString() ?? "null";
            throw new Exception($"Member not found: {methodName}/{args.Length}");
        }

        /// <summary>
        /// 플레이 중 Main Camera 화면 + Overlay Canvas를 함께 PNG로 저장합니다.
        /// Canvas를 잠시 Screen Space - Camera로 바꿨다가 즉시 되돌립니다(플레이 모드 값이라 저장되지 않음).
        /// </summary>
        private static void RenderGame(string outPng, int w, int h)
        {
            Camera cam = Camera.main;
            if (cam == null) throw new Exception("Main Camera not found");
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.isActiveAndEnabled).ToArray();
            RenderTexture previousTarget = cam.targetTexture;
            float[] previousDistance = canvases.Select(c => c.planeDistance).ToArray();
            try
            {
                cam.targetTexture = rt;
                foreach (Canvas canvas in canvases)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = cam.nearClipPlane + 0.2f;
                }
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPng)));
                File.WriteAllBytes(outPng, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                    canvases[i].planeDistance = previousDistance[i];
                }
                cam.targetTexture = previousTarget;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        // ---------- Rendering ----------

        private static void RenderPrefab(string prefabPath, string outPng, int w, int h)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new Exception($"Prefab not found: {prefabPath}");
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                RenderRoots(scene, new[] { instance }, outPng, w, h);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>
        /// 씬의 Canvas 루트들을 Preview Scene에 복제해 렌더링합니다. 원본 씬은 수정하지 않습니다.
        /// toggles: "+이름"은 강제로 활성화, "-이름"은 비활성화합니다.
        /// </summary>
        private static void RenderScene(string scenePath, string outPng, int w, int h, string[] toggles)
        {
            Scene source = SceneManager.GetSceneByPath(scenePath);
            bool openedHere = false;
            if (!source.IsValid() || !source.isLoaded)
            {
                source = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedHere = true;
            }

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                List<GameObject> copies = new List<GameObject>();
                foreach (GameObject root in source.GetRootGameObjects())
                {
                    if (root.GetComponentInChildren<Canvas>(true) == null) continue;
                    GameObject copy = UnityEngine.Object.Instantiate(root);
                    copy.name = root.name;
                    SceneManager.MoveGameObjectToScene(copy, preview);
                    copies.Add(copy);
                }

                foreach (string toggle in toggles)
                {
                    bool on = toggle[0] == '+';
                    string name = toggle.Substring(1);
                    foreach (GameObject copy in copies)
                    {
                        foreach (Transform t in copy.GetComponentsInChildren<Transform>(true))
                        {
                            if (t.name == name) t.gameObject.SetActive(on);
                        }
                    }
                }

                RenderRoots(preview, copies.ToArray(), outPng, w, h);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                if (openedHere) EditorSceneManager.CloseScene(source, true);
            }
        }

        private static void RenderRoots(Scene scene, GameObject[] roots, string outPng, int w, int h)
        {
            GameObject camGo = new GameObject("BridgeCamera");
            SceneManager.MoveGameObjectToScene(camGo, scene);
            Camera cam = camGo.AddComponent<Camera>();
            cam.scene = scene;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.55f, 0.32f, 1f);
            cam.orthographic = true;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            camGo.transform.position = new Vector3(0, 0, -50);

            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;

            List<Canvas> rootCanvases = new List<Canvas>();
            foreach (GameObject root in roots)
            {
                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true).Where(c => c.isRootCanvas || c.transform.parent == null).ToArray();
                if (canvases.Length == 0)
                {
                    GameObject canvasGo = new GameObject("BridgeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                    SceneManager.MoveGameObjectToScene(canvasGo, scene);
                    CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                    root.transform.SetParent(canvasGo.transform, false);
                    canvases = new[] { canvasGo.GetComponent<Canvas>() };
                }
                rootCanvases.AddRange(canvases);
            }

            int order = 0;
            foreach (Canvas canvas in rootCanvases.OrderBy(c => c.sortingOrder))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 40f - (order++ * 0.5f);
            }

            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in roots.SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true))) text.ForceMeshUpdate(true, true);
            foreach (LayoutGroup lg in roots.SelectMany(r => r.GetComponentsInChildren<LayoutGroup>(true)))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)lg.transform);
            }
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPng)));
            File.WriteAllBytes(outPng, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }

        // ---------- Dump ----------

        private static string Dump(string assetPath)
        {
            StringBuilder sb = new StringBuilder();
            if (assetPath.EndsWith(".prefab"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                DumpGo(prefab.transform, 0, sb);
                return sb.ToString();
            }

            Scene scene = SceneManager.GetSceneByPath(assetPath);
            bool openedHere = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
                openedHere = true;
            }
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects()) DumpGo(root.transform, 0, sb);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
            return sb.ToString();
        }

        private static void DumpGo(Transform t, int depth, StringBuilder sb)
        {
            string pad = new string(' ', depth * 2);
            sb.Append(pad).Append(t.gameObject.activeSelf ? "" : "(off) ").Append(t.name);
            if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
            {
                string src = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject));
                sb.Append(" [prefab ").Append(Path.GetFileName(src)).Append(']');
            }
            if (t is RectTransform rt)
            {
                sb.Append($" rect(aMin={Fmt(rt.anchorMin)} aMax={Fmt(rt.anchorMax)} piv={Fmt(rt.pivot)} pos={Fmt(rt.anchoredPosition)} size={Fmt(rt.sizeDelta)})");
            }
            else
            {
                sb.Append($" pos={t.localPosition}");
            }
            List<string> comps = new List<string>();
            foreach (Component c in t.GetComponents<Component>())
            {
                if (c == null) { comps.Add("MISSING"); continue; }
                switch (c)
                {
                    case Transform _: break;
                    case CanvasRenderer _: break;
                    case Image img:
                        comps.Add($"Image(sprite={(img.sprite ? img.sprite.name : "-")} col=#{ColorUtility.ToHtmlStringRGBA(img.color)} type={img.type}{(img.raycastTarget ? "" : " noRaycast")})");
                        break;
                    case TMP_Text txt:
                        comps.Add($"TMP(\"{txt.text.Replace("\n", "\\n")}\" size={txt.fontSize} col=#{ColorUtility.ToHtmlStringRGBA(txt.color)} font={(txt.font ? txt.font.name : "-")} align={txt.alignment})");
                        break;
                    case Canvas cv:
                        comps.Add($"Canvas({cv.renderMode} order={cv.sortingOrder})");
                        break;
                    case CanvasScaler cs:
                        comps.Add($"Scaler({cs.uiScaleMode} ref={cs.referenceResolution} match={cs.matchWidthOrHeight})");
                        break;
                    case GridLayoutGroup g:
                        comps.Add($"Grid(cell={g.cellSize} spacing={g.spacing} pad={g.padding} constraint={g.constraint}:{g.constraintCount})");
                        break;
                    case HorizontalOrVerticalLayoutGroup hv:
                        comps.Add($"{c.GetType().Name}(spacing={hv.spacing} pad={hv.padding})");
                        break;
                    case Button b:
                        comps.Add($"Button(onClick={b.onClick.GetPersistentEventCount()}:{string.Join(",", Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Select(i => (b.onClick.GetPersistentTarget(i) ? b.onClick.GetPersistentTarget(i).name : "null") + "." + b.onClick.GetPersistentMethodName(i)))})");
                        break;
                    default:
                        comps.Add(c.GetType().Name);
                        break;
                }
            }
            if (comps.Count > 0) sb.Append("  {").Append(string.Join(", ", comps)).Append('}');
            sb.Append('\n');
            foreach (Transform child in t) DumpGo(child, depth + 1, sb);
        }

        private static string Fmt(Vector2 v) => $"{v.x:0.##},{v.y:0.##}";
    }
}
