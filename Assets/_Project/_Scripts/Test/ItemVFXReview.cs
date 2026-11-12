using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ujam.Runtime.Item;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UJam.Runtime.Test
{
    /// <summary>리뷰 전용 입력·GUI·배치. 판정과 VFX는 기존 Item/Skill/Shooter 코드를 그대로 호출한다.</summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class ItemVFXReview : MonoBehaviour
    {
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private GameObject playerTemplate;
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private EnemyBase[] dummies;
        [SerializeField] private PlayerSkillPreview circlePrefab, rectanglePrefab;
        [SerializeField] private ItemScreenVfxCanvas screenVfx;
        [SerializeField] private Vector3 rowCenter, rowRight = Vector3.right;
        [SerializeField] private float groundY;
        private readonly Dictionary<string, Item> owned = new();
        private PlayerStatus player;
        private PlayerSkillManager skills;
        private PlayerShooter shooter;
        private GameObject playerRig;
        private PlayerSkillPreview circle, rectangle, preview;
        private Item selected;
        private bool armed, previousBackground, initialized;
        private int armedFrame;
        private Vector2 scroll;
        private string message = "아이템 ID 선택 → 지면 좌클릭으로 사용";
        private GUIStyle heading, small, label;
        private float UiScale => Mathf.Clamp(Screen.width / 1600f, .75f, 1.25f);
        private Rect Panel => new Rect(12 * UiScale, 12 * UiScale, 300 * UiScale, Screen.height - 24 * UiScale);
        public Camera ReviewCamera => reviewCamera;
        public IReadOnlyList<EnemyBase> Dummies => dummies;
        public PlayerSkillPreview CurrentPreview => preview;

        private void Awake()
        {
            initialized = true;
            previousBackground = Application.runInBackground;
            Application.runInBackground = true;
            Time.timeScale = 1;
        }

        private void Start()
        {
            CreatePlayer();
            circle = Instantiate(circlePrefab);
            rectangle = Instantiate(rectanglePrefab);
            circle.Hide(); rectangle.Hide();
            foreach (var enemy in dummies) if (enemy != null) enemy.PrepareForTesting(null);
        }

        private void CreatePlayer()
        {
            playerRig = Instantiate(playerTemplate, playerTemplate.transform.position, playerTemplate.transform.rotation);
            playerRig.name = "Review Player";
            playerRig.SetActive(true);
            player = playerRig.GetComponent<PlayerStatus>();
            skills = playerRig.GetComponent<PlayerSkillManager>();
            shooter = playerRig.GetComponent<PlayerShooter>();
            if (screenVfx != null) screenVfx.Bind(player);
            else Debug.LogError("[ItemVFXReview] Canvas에 ItemScreenVFX 프리팹을 배치하고 연결하세요.", this);
        }

        public void SelectItem(string id)
        {
            ItemMeta meta = ItemCatalog.GetMeta(id);
            if (meta == null || player == null) return;
            CancelAim();
            if (meta.Kind == ItemKind.Passive)
            {
                if (owned.TryGetValue(id, out var passive)) Remove(passive);
                else Own(id);
                string hint = id == "16" || id == "22" || id == "24"
                    ? " 장착됨 · 아래 발동 조건으로 확인" : " 장착됨 · 우클릭 사격/체력 버튼으로 확인";
                message = id + " " + meta.Name + (owned.ContainsKey(id) ? hint : " 해제됨");
                return;
            }
            selected = Own(id);
            skills.UnEquip(0);
            if (!skills.Equip(0, selected)) { message = "스킬 장착 실패"; return; }
            if (meta.Target == ItemTarget.Player)
            {
                ConfirmTarget(player.transform.position);
                return;
            }
            armed = true;
            armedFrame = Time.frameCount;
            preview = meta.Preview.Type == ItemPreviewType.Circle ? circle :
                meta.Preview.Type == ItemPreviewType.Rectangle ? rectangle : null;
            if (preview != null) preview.Show(meta.Preview.Scale, meta.Preview.Direction);
            message = id + " " + meta.Name + " · 지면 좌클릭으로 확정 / 우클릭·Esc 취소";
        }

        private Item Own(string id)
        {
            if (owned.TryGetValue(id, out Item existing) && existing.IsEquipped) return existing;
            Item item = ItemCatalog.Create(id);
            owned[id] = item;
            item.Equip(player, 1 << LayerMask.NameToLayer("Enemy"), () => Remove(item));
            return item;
        }

        private void Remove(Item item)
        {
            if (item == null) return;
            skills?.Remove(item);
            item.Unequip();
            if (player != null) player.ForgetItem(item);
            owned.Remove(item.ID);
            if (selected == item) { CancelAim(); selected = null; }
        }

        public void AimAt(Vector3 point, Vector3 normal)
        {
            if (armed && preview != null) preview.SetPosition(point, normal);
        }

        public bool ConfirmTarget(Vector3 point)
        {
            if (selected == null || !selected.IsEquipped) return false;
            Item item = selected;
            bool used = skills.ConfirmUse(0, item.Meta.Target == ItemTarget.Player ? player.transform.position : point);
            if (used) { CancelAim(); message = item.ID + " " + item.Meta.Name + " 사용 · ID 재선택으로 다시 조준"; }
            else message = player.CurrentHealth <= 0 ? "플레이어 사망 · 전체 초기화를 눌러주세요" : "시전 대기 " + skills.GetRemainingCooldown(0).ToString("F1") + "초";
            return used;
        }

        private void CancelAim()
        {
            armed = false;
            if (preview != null) preview.Hide();
            preview = null;
        }

        private void Update()
        {
            if (player == null || Mouse.current == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CancelAim();
            Vector2 mouse = Mouse.current.position.ReadValue();
            if (Panel.Contains(new Vector2(mouse.x, Screen.height - mouse.y)))
            {
                if (preview != null) preview.Hide();
                return;
            }
            bool ground = Physics.Raycast(reviewCamera.ScreenPointToRay(mouse), out RaycastHit hit, 1000,
                1 << LayerMask.NameToLayer("Ground"), QueryTriggerInteraction.Ignore);
            if (armed)
            {
                if (preview != null)
                {
                    if (ground) { preview.Show(selected.Meta.Preview.Scale, selected.Meta.Preview.Direction); AimAt(hit.point, hit.normal); }
                    else preview.Hide();
                }
                if (Mouse.current.rightButton.wasPressedThisFrame) CancelAim();
                else if (ground && Time.frameCount > armedFrame && Mouse.current.leftButton.wasPressedThisFrame) ConfirmTarget(hit.point);
            }
            else if (Mouse.current.rightButton.isPressed || Mouse.current.rightButton.wasPressedThisFrame) shooter.TryShoot();
        }

        public void RespawnDummies()
        {
            foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                if (enemy.gameObject.scene == gameObject.scene) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            dummies = new EnemyBase[enemyPrefabs.Length];
            for (int i = 0; i < enemyPrefabs.Length; i++)
                dummies[i] = Instantiate(enemyPrefabs[i], rowCenter, Quaternion.LookRotation(-Vector3.ProjectOnPlane(reviewCamera.transform.forward, Vector3.up)), transform).GetComponent<EnemyBase>();
            Arrange(dummies, rowCenter, rowRight, groundY);
            foreach (var enemy in dummies) enemy.PrepareForTesting(null);
            message = "적 더미 8종 재배치 · 장착 아이템은 유지됩니다";
        }

        public void ResetReview()
        {
            CancelAim();
            if (screenVfx != null) screenVfx.Clear();
            foreach (Item item in owned.Values.ToArray()) Remove(item);
            selected = null;
            if (playerRig != null) { playerRig.SetActive(false); Destroy(playerRig); }
            CreatePlayer();
            RespawnDummies();
            message = "전체 초기화 완료 · 아이템 ID를 선택하세요";
        }

        /// <summary>화면 연출 대신 실제 시전·적중·피해 조건을 재현한다. 소모된 아이템도 검증할 수 있도록 개체를 반환한다.</summary>
        public Item ReviewScreenEffect(string id, int rageHealthPercent = 40)
        {
            if (id != "13" && id != "14" && id != "15" && id != "17" && id != "25" && id != "27") return null;
            if (player == null || player.CurrentHealth <= 0)
            {
                message = "플레이어 사망 · 전체 초기화 후 화면 VFX를 확인하세요";
                return null;
            }
            CancelAim();
            Item item = Own(id);
            if (id == "13" || id == "14" || id == "15")
            {
                if (id == "14") SetReviewHealth(70);
                SelectItem(id);
            }
            else if (id == "17")
            {
                var target = dummies.FirstOrDefault(enemy => enemy != null && enemy.Status.HP > 0);
                if (target == null) { message = "살아 있는 표적이 없습니다 · 적을 재배치하세요"; return item; }
                for (int hit = 0; hit < 5; hit++)
                    EventManager.Instance.Publish(new ItemEvent(ItemTrigger.Shooting, player, target.transform.position, new[] { target }));
                message = "17 집념 · 같은 더미 5회 적중 조건으로 발동 · 다시 눌러 갱신";
            }
            else if (id == "25")
            {
                player.TakeDamage(new DamageInfo(player.MaxHealth * 2, "Review", ignoreDamageTaken: true));
                message = "25 리저렉션 · 실제 치명 피해를 막고 부활 · 아이템 소모 후에도 연출 유지";
            }
            else
            {
                int percent = Mathf.Clamp(rageHealthPercent, 1, 100);
                SetReviewHealth(percent);
                message = "27 광전사의 분노 · HP " + percent + "% · 아래 단계와 회복 버튼으로 농도 비교";
            }
            return item;
        }

        private void SetReviewHealth(float percent)
        {
            float difference = player.MaxHealth * percent / 100f - player.CurrentHealth;
            if (difference > 0) player.Heal(difference);
            else if (difference < 0) player.TakeDamage(new DamageInfo(-difference, "Review", ignoreDamageTaken: true));
        }

        /// <summary>조건을 채우기 전에 적이 죽지 않도록 리뷰 인스턴스의 체력만 충분히 준비한다.</summary>
        public void PreparePassiveTargets()
        {
            if (player == null) return;
            RespawnDummies();
            float health = Mathf.Max(1000, player.AttackDamage * player.DamageMultiplier * 100);
            foreach (var enemy in dummies) enemy.Status.ConfigureSpawnStats(health, 0, 0);
            message = "패시브 표적 준비 · 같은 적에게 우클릭 5회 / 스킬 5회 / 처치 4회로 확인";
        }

        /// <summary>실제 슬롯 시전으로 과부화의 사용 횟수와 적중 조건을 확인한다.</summary>
        public bool CastPassiveReviewSkill()
        {
            if (player == null || player.CurrentHealth <= 0 || !owned.ContainsKey("22")) return false;
            var target = dummies.Where(enemy => enemy != null && enemy.Status.HP > 0)
                .OrderBy(enemy => (enemy.transform.position - rowCenter).sqrMagnitude).FirstOrDefault();
            if (target == null) { message = "살아 있는 표적이 없습니다 · 표적을 재배치하세요"; return false; }
            SelectItem("1");
            bool used = ConfirmTarget(target.transform.position);
            if (used) message = "집중 사격 실제 시전 · 과부화는 5번째 스킬의 적중 시 발동합니다";
            return used;
        }

        /// <summary>실제 플레이어 처치 이벤트를 쌓고 살아 있는 이웃에게 시체 폭발 피해를 확인한다.</summary>
        public bool KillPassiveReviewTarget()
        {
            if (player == null || player.CurrentHealth <= 0 || !owned.ContainsKey("24")) return false;
            var living = dummies.Where(enemy => enemy != null && enemy.Status.HP > 0).ToArray();
            if (living.Length == 0) { message = "살아 있는 표적이 없습니다 · 표적을 재배치하세요"; return false; }
            var target = living[0];
            if (living.Length > 1)
            {
                Vector3 position = living[1].transform.position + rowRight * (ItemWorld.CellWidth * .5f);
                position.y = target.transform.position.y;
                target.transform.position = position;
            }
            float percent = target.Status.HP * 200 / Mathf.Max(.01f, player.AttackDamage * player.DamageMultiplier);
            bool killed = player.DealDamage(target, percent, "Review") > 0 && target.Status.HP <= 0;
            if (killed) message = "실제 플레이어 처치 · 시체 폭발은 4번째 처치 위치에서 주변 적에게 피해를 줍니다";
            return killed;
        }

        private static Bounds BodyBounds(EnemyBase enemy)
        {
            Renderer[] body = enemy.GetComponentsInChildren<Renderer>(true)
                .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
            Bounds bounds = body.Length > 0 ? body[0].bounds : new Bounds(enemy.transform.position + Vector3.up, Vector3.one);
            foreach (Renderer renderer in body.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void Arrange(EnemyBase[] enemies, Vector3 center, Vector3 right, float floor)
        {
            float[] widths = enemies.Select(enemy => Mathf.Max(1, BodyBounds(enemy).size.x)).ToArray();
            float cursor = -(widths.Sum() + .7f * (enemies.Length - 1)) / 2;
            for (int i = 0; i < enemies.Length; i++)
            {
                Transform target = enemies[i].transform;
                target.position = center + right * (cursor + widths[i] / 2);
                target.position += Vector3.up * (floor - BodyBounds(enemies[i]).min.y);
                cursor += widths[i] + .7f;
            }
        }

        private void OnGUI()
        {
            if (player == null) return;
            heading ??= new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = true };
            small ??= new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            label ??= new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * UiScale);
            GUILayout.BeginArea(new Rect(12, 12, 300, Screen.height / UiScale - 24), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("ITEM VFX REVIEW", heading);
            GUILayout.Label("ID 선택 → 지면 좌클릭\n우클릭: 조준 취소 / 사격\nID 재선택: 쿨타임 초기화", small);
            DrawScreenReview();
            DrawIDs(1, 15, "액티브");
            DrawIDs(16, 30, "패시브 · 클릭하여 장착/해제");
            if (owned.ContainsKey("16") || owned.ContainsKey("22") || owned.ContainsKey("24"))
            {
                GUILayout.Label("패시브 발동 확인", small);
                if (GUILayout.Button("튼튼한 표적 재배치", GUILayout.Height(30))) PreparePassiveTargets();
                if (owned.ContainsKey("16")) GUILayout.Label("16 은화살: 같은 적에게 우클릭 사격 5회. 빗나가거나 표적이 바뀌면 연속 적중이 초기화됩니다.", small);
                if (owned.ContainsKey("22"))
                {
                    GUILayout.Label("22 과부화: 스킬 5회 사용 후 해당 시전의 적중 대상에 발동합니다.", small);
                    if (GUILayout.Button("22 확인: 집중 사격 1회 시전", GUILayout.Height(30))) CastPassiveReviewSkill();
                }
                if (owned.TryGetValue("24", out var corpse))
                {
                    GUILayout.Label("24 시체 폭발: 실제 처치 누적 " + player.GetCount(corpse) + "/4 · 발동 시 초기화", small);
                    if (GUILayout.Button("24 확인: 이웃 옆에서 적 1체 처치", GUILayout.Height(30))) KillPassiveReviewTarget();
                }
            }
            if (selected != null)
            {
                ItemMeta meta = selected.Meta;
                GUILayout.Label(selected.ID + " " + meta.Name, heading);
                GUILayout.Label(meta.Preview.Type + " / XZ " + meta.Preview.Scale + "\n" +
                    (screenVfx != null && screenVfx.GetView(selected.ID) != null ? "Canvas 화면 연출 · 미리 배치된 레이어 사용" :
                    string.IsNullOrEmpty(meta.VfxPath) ? "전용 VFX 미연결 · 실제 효과 확인" : meta.VfxPath), small);
            }
            GUILayout.Space(6);
            GUILayout.Label(message, small);
            GUILayout.Label("PLAYER HP " + player.CurrentHealth.ToString("F0") + "/" + player.MaxHealth +
                "  ATK " + player.AttackDamage + "\n" + (armed ? "지면 클릭 대기" : "선택된 시전 없음") + " / 남은 쿨타임 " + skills.GetRemainingCooldown(0).ToString("F1"), small);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("피해 20%", GUILayout.Height(30))) player.TakeDamage(new DamageInfo(player.MaxHealth * .2f, "Review"));
            if (GUILayout.Button("회복", GUILayout.Height(30))) player.Heal(player.MaxHealth);
            if (GUILayout.Button("치명 피해", GUILayout.Height(30))) player.TakeDamage(new DamageInfo(player.MaxHealth * 2, "Review"));
            GUILayout.EndHorizontal();
            if (GUILayout.Button("적만 재배치", GUILayout.Height(32))) RespawnDummies();
            if (GUILayout.Button("전체 초기화", GUILayout.Height(32))) ResetReview();
            GUILayout.Label("더미는 피해·속성·밀치기·도발·사망을 그대로 받습니다. 미끼 도발 시 이동/공격할 수 있습니다.", small);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            foreach (var enemy in dummies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.Status.HP <= 0) continue;
                Vector3 point = reviewCamera.WorldToScreenPoint(BodyBounds(enemy).center + Vector3.up * (BodyBounds(enemy).extents.y + .4f));
                if (point.z < 0) continue;
                var states = new List<string>();
                foreach (ElementType type in Enum.GetValues(typeof(ElementType)))
                    if (ElementManager.Instance.TryGet(enemy, type, out var state)) states.Add(type + (state.Stacks > 0 ? " " + state.Stacks : ""));
                if (enemy.Status.IsStunned) states.Add("STUN");
                if (enemy.FSM.Targets.Any(target => target != null && target.GetComponent<DecoyController>() != null)) states.Add("TAUNT");
                string text = enemy.name.Replace("(Clone)", "") + "\nHP " + enemy.Status.HP.ToString("F0") + "/" + enemy.Status.MaxHealth +
                    (states.Count > 0 ? "\n" + string.Join(" / ", states) : "");
                GUI.Box(new Rect(point.x / UiScale - 65, (Screen.height - point.y) / UiScale - 44, 130, states.Count > 0 ? 62 : 44), text, label);
            }
            GUI.matrix = previous;
        }

        private void DrawIDs(int first, int last, string title)
        {
            GUILayout.Label(title, small);
            for (int id = first; id <= last; id++)
            {
                if ((id - first) % 5 == 0) GUILayout.BeginHorizontal();
                string key = id.ToString();
                Color previous = GUI.backgroundColor;
                if (id >= 16 && owned.ContainsKey(key)) GUI.backgroundColor = new Color(.35f, .95f, .65f);
                else if (selected != null && selected.ID == key) GUI.backgroundColor = new Color(1, .8f, .25f);
                if (GUILayout.Button(key, GUILayout.Height(32))) SelectItem(key);
                GUI.backgroundColor = previous;
                if ((id - first) % 5 == 4 || id == last) GUILayout.EndHorizontal();
            }
        }

        private void DrawScreenReview()
        {
            GUILayout.Label("화면 VFX 빠른 확인", heading);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("13 아드레날린", GUILayout.Height(30))) ReviewScreenEffect("13");
            if (GUILayout.Button("14 긴급 수리", GUILayout.Height(30))) ReviewScreenEffect("14");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("15 생명력 전환", GUILayout.Height(30))) ReviewScreenEffect("15");
            if (GUILayout.Button("17 집념 · 5적중", GUILayout.Height(30))) ReviewScreenEffect("17");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("25 리저렉션", GUILayout.Height(30))) ReviewScreenEffect("25");
            if (GUILayout.Button("27 광전사의 분노", GUILayout.Height(30))) ReviewScreenEffect("27");
            GUILayout.EndHorizontal();
            GUILayout.Label("14: HP 70% 준비 후 회복·소모 / 15: 체력 지불\n17: 같은 적 5적중 / 25: 치명 피해 후 부활\n27: HP 단계 선택 · 다른 장착 아이템은 유지", small);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("80%", GUILayout.Height(28))) ReviewScreenEffect("27", 80);
            if (GUILayout.Button("60%", GUILayout.Height(28))) ReviewScreenEffect("27", 60);
            if (GUILayout.Button("40%", GUILayout.Height(28))) ReviewScreenEffect("27", 40);
            if (GUILayout.Button("20%", GUILayout.Height(28))) ReviewScreenEffect("27", 20);
            if (GUILayout.Button("회복", GUILayout.Height(28))) ReviewScreenEffect("27", 100);
            GUILayout.EndHorizontal();
            GUILayout.Label("HP " + player.CurrentHealth.ToString("F0") + "/" + player.MaxHealth, small);
        }

        private void OnDestroy()
        {
            if (!initialized) return;
            if (screenVfx != null) screenVfx.Clear();
            foreach (Item item in owned.Values.ToArray()) Remove(item);
            if (playerRig != null) Destroy(playerRig);
            if (circle != null) Destroy(circle.gameObject);
            if (rectangle != null) Destroy(rectangle.gameObject);
            Application.runInBackground = previousBackground;
        }

#if UNITY_EDITOR
        // 제작과 검증도 이 테스트 파일 안에 두어 기존 게임 스크립트를 수정하지 않는다.
        [InitializeOnLoad]
        public static class EditorTools
        {
            private const string ScenePath = "Assets/_Project/Scenes/ItemVFXReview.unity";
            private const string SourcePath = "Assets/_Project/Scenes/PJS_Scene.unity";
            private const string Request = "Library/ItemVFXReview.request";
            private const string Report = "Library/ItemVFXReviewReport.txt";
            private const string ModeKey = "UJam.ItemVFXReview.Mode";
            private const string PreviousSceneKey = "UJam.ItemVFXReview.PreviousStartScene";
            private static double deadline;
            private static readonly string[] EnemyNames = { "Golem", "Eyebeast", "Big_Golem", "Sniper_Eyebeast", "Witch", "Skeleton", "Slime", "Rollbug" };

            static EditorTools()
            {
                EditorApplication.update += Poll;
                EditorApplication.playModeStateChanged += PlayChanged;
            }

            [MenuItem("Tools/Items/Create ItemVFXReview Scene")]
            public static void Build()
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play before creating the review scene.");
                Require(!File.Exists(ScenePath), "ItemVFXReview already exists; its authored scene is preserved.");
                Require(AssetDatabase.CopyAsset(SourcePath, ScenePath), "Could not copy PJS_Scene.");
                Scene previous = SceneManager.GetActiveScene();
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                bool saved = false;
                try
                {
                    SceneManager.SetActiveScene(scene);
                    foreach (GameObject root in scene.GetRootGameObjects())
                        if (PrefabUtility.IsAnyPrefabInstanceRoot(root))
                            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    var sourceCombat = All<PlayerCombatManager>(scene).First();
                    Camera camera = sourceCombat.AimCamera;
                    PlayerStatus originalStatus = sourceCombat.PlayerStatus;
                    Require(camera != null && originalStatus != null, "PJS combat camera/player references are missing.");
                    camera.transform.SetParent(null, true);
                    camera.name = "PJS Main Camera";
                    var review = new GameObject("Item VFX Review").AddComponent<ItemVFXReview>();
                    review.reviewCamera = camera;
                    const string previewFolder = "Assets/_Project/Prefabs/Player/Skill/Preview/";
                    review.circlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(previewFolder + "SkillPreviewCircle.prefab").GetComponent<PlayerSkillPreview>();
                    review.rectanglePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(previewFolder + "SkillPreviewRectangle.prefab").GetComponent<PlayerSkillPreview>();
                    var template = new GameObject("Review Player Template (inactive)");
                    template.SetActive(false);
                    var status = template.AddComponent<PlayerStatus>();
                    EditorUtility.CopySerialized(originalStatus, status);
                    var skills = template.AddComponent<PlayerSkillManager>();
                    var shooter = template.AddComponent<PlayerShooter>();
                    var combat = template.AddComponent<PlayerCombatManager>();
                    Bind(combat, "_aimCamera", camera);
                    Bind(combat, "_playerStatus", status);
                    Bind(combat, "_playerSkillManager", skills);
                    Bind(combat, "_playerShooter", shooter);
                    Layer(combat, "_groundMask", 1 << LayerMask.NameToLayer("Ground"));
                    Layer(combat, "_enemyMask", 1 << LayerMask.NameToLayer("Enemy"));
                    review.playerTemplate = template;
                    var grid = All<GridSystem>(scene).First();
                    var timer = All<RuntimeTimer>(scene).First();
                    GameObject[] maps = scene.GetRootGameObjects().Where(root => root.name == "Map" || root.name == "map_v1.00_prefab").ToArray();
                    Require(maps.Length == 2, "PJS Map roots could not be identified.");
                    var keep = new HashSet<GameObject>(maps)
                    {
                        camera.gameObject, review.gameObject, template, grid.transform.root.gameObject, timer.transform.root.gameObject
                    };
                    foreach (GameObject root in scene.GetRootGameObjects())
                        if (root.name == "Directional Light" || root.name == "Global Volume") keep.Add(root);
                    foreach (GameObject root in scene.GetRootGameObjects())
                        if (!keep.Contains(root)) UnityEngine.Object.DestroyImmediate(root);
                    foreach (MonoBehaviour component in timer.GetComponents<MonoBehaviour>())
                        if (component != timer) UnityEngine.Object.DestroyImmediate(component);
                    foreach (MonoBehaviour component in grid.GetComponents<MonoBehaviour>())
                        if (component != grid) UnityEngine.Object.DestroyImmediate(component);
                    foreach (GameObject map in maps)
                    {
                        // Map의 게임 진행/거점만 제거하고 렌더링·Collider 배치는 그대로 복사한다.
                        foreach (MonoBehaviour component in map.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
                            if (component != null) UnityEngine.Object.DestroyImmediate(component);
                        foreach (Transform child in map.GetComponentsInChildren<Transform>(true))
                            if (child.CompareTag("BaseCore")) child.tag = "Untagged";
                    }
                    var canvasRoot = new GameObject("Review Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                    var canvas = canvasRoot.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100;
                    var scaler = canvasRoot.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    const string screenPrefabPath = "Assets/_Project/Prefabs/VFX/ScreenEffect/ItemScreenVFX.prefab";
                    var screenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(screenPrefabPath);
                    Require(screenPrefab != null, "The common ItemScreenVFX prefab is missing.");
                    var screenInstance = (GameObject)PrefabUtility.InstantiatePrefab(screenPrefab, scene);
                    screenInstance.transform.SetParent(canvasRoot.transform, false);
                    review.screenVfx = screenInstance.GetComponent<ItemScreenVfxCanvas>();
                    Require(review.screenVfx != null, "The common screen prefab requires its Canvas controller.");
                    Transform ground = maps.SelectMany(map => map.GetComponentsInChildren<Transform>(true))
                        .First(child => child.gameObject.layer == LayerMask.NameToLayer("Ground") && child.GetComponent<Collider>() != null);
                    review.groundY = ground.position.y;
                    review.rowCenter = GroundPoint(camera, new Vector2(.5f, .52f), review.groundY);
                    review.rowRight = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
                    template.transform.position = GroundPoint(camera, new Vector2(.5f, .17f), review.groundY);
                    review.enemyPrefabs = EnemyNames.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/" + name + ".prefab")).ToArray();
                    Require(review.enemyPrefabs.All(prefab => prefab != null), "An Enemy prefab is missing.");
                    var group = new GameObject("Enemy Dummies · All 8 Types");
                    review.dummies = review.enemyPrefabs.Select(prefab =>
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                        instance.transform.SetParent(group.transform, true);
                        instance.transform.SetPositionAndRotation(review.rowCenter, Quaternion.LookRotation(-Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up)));
                        return instance.GetComponent<EnemyBase>();
                    }).ToArray();
                    Arrange(review.dummies, review.rowCenter, review.rowRight, review.groundY);
                    EditorUtility.SetDirty(review);
                    Require(EditorSceneManager.SaveScene(scene, ScenePath), "Could not save ItemVFXReview.");
                    saved = true;
                    File.WriteAllText(Report, "BUILD PASS: PJS camera/Map/environment/Grid/element VFX copied; 8 enemies in one row; authored Overlay Canvas with common ItemScreenVFX prefab; one new test script.");
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                    if (!saved) AssetDatabase.DeleteAsset(ScenePath);
                }
            }

            private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

            private static void Bind(UnityEngine.Object target, string name, UnityEngine.Object value)
            {
                var serialized = new SerializedObject(target);
                serialized.FindProperty(name).objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            private static void Layer(UnityEngine.Object target, string name, int value)
            {
                var serialized = new SerializedObject(target);
                serialized.FindProperty(name).intValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            private static Vector3 GroundPoint(Camera camera, Vector2 viewport, float y)
            {
                Ray ray = camera.ViewportPointToRay(viewport);
                Require(new Plane(Vector3.up, Vector3.up * y).Raycast(ray, out float distance), "Camera does not see the field.");
                return ray.GetPoint(distance);
            }

            private static void Poll()
            {
                if (SessionState.GetString(ModeKey, "") == "check" && deadline > 0 && EditorApplication.timeSinceStartup > deadline)
                { Finish(new TimeoutException("ItemVFXReview check exceeded 60 seconds.")); return; }
                if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
                string mode;
                try { mode = File.ReadAllText(Request).Trim(); File.Delete(Request); }
                catch (IOException) { return; }
                try
                {
                    if (mode == "build") { Build(); return; }
                    Require(mode == "check" || mode == "play", "Expected build/check/play request.");
                    CheckScene();
                    SessionState.SetString(PreviousSceneKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
                    SessionState.SetString(ModeKey, mode);
                    EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                    EditorApplication.isPlaying = true;
                }
                catch (Exception error) { File.WriteAllText(Report, error.ToString()); Debug.LogException(error); }
            }

            private static void CheckScene()
            {
                Scene source = EditorSceneManager.OpenPreviewScene(SourcePath);
                Scene target = EditorSceneManager.OpenPreviewScene(ScenePath);
                try
                {
                    var review = All<ItemVFXReview>(target).Single();
                    Require(review.screenVfx != null && review.screenVfx.GetComponentInParent<Canvas>() != null,
                        "Review requires its authored ItemScreenVFX under a Canvas.");
                    Camera original = All<PlayerCombatManager>(source).Single().AimCamera;
                    Require(Vector3.Distance(review.reviewCamera.transform.position, original.transform.position) < .001f &&
                        Quaternion.Angle(review.reviewCamera.transform.rotation, original.transform.rotation) < .001f &&
                        Mathf.Abs(review.reviewCamera.fieldOfView - original.fieldOfView) < .001f, "PJS camera changed.");
                    Require(review.dummies.Length == 8 && review.dummies.All(enemy => enemy != null), "Eight dummy types are required.");
                    Require(All<GameManager>(target).Length == 0, "Review scene must not run the game loop.");
                    Require(All<RuntimeTimer>(target).Length == 1 && All<GridSystem>(target).Length == 1, "Review needs one Timer and Grid.");
                    foreach (string name in new[] { "Map", "map_v1.00_prefab" })
                    {
                        Transform a = source.GetRootGameObjects().Single(root => root.name == name).transform;
                        Transform b = target.GetRootGameObjects().Single(root => root.name == name).transform;
                        Require(a.position == b.position && a.rotation == b.rotation && a.localScale == b.localScale &&
                            a.GetComponentsInChildren<Renderer>(true).Length == b.GetComponentsInChildren<Renderer>(true).Length,
                            "PJS " + name + " layout changed.");
                    }
                    foreach (var enemy in review.dummies)
                    {
                        Vector3 screen = review.reviewCamera.WorldToViewportPoint(BodyBounds(enemy).center);
                        Require(screen.z > 0 && screen.x > .2f && screen.x < .98f && screen.y > .05f && screen.y < .95f,
                            enemy.name + " is outside the visible review row.");
                        Require(Mathf.Abs(Vector3.Dot(enemy.transform.position - review.rowCenter, Vector3.Cross(review.rowRight, Vector3.up))) < .001f,
                            "Dummies must form one row.");
                    }
                }
                finally { EditorSceneManager.ClosePreviewScene(target); EditorSceneManager.ClosePreviewScene(source); }
            }

            private static void PlayChanged(PlayModeStateChange state)
            {
                string mode = SessionState.GetString(ModeKey, "");
                if (string.IsNullOrEmpty(mode)) return;
                if (state == PlayModeStateChange.EnteredPlayMode && mode == "check")
                {
                    deadline = EditorApplication.timeSinceStartup + 60;
                    var review = FindFirstObjectByType<ItemVFXReview>();
                    if (review == null) { Finish(new InvalidOperationException("Review scene did not load.")); return; }
                    review.StartCoroutine(Guard(Verify(review)));
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(PreviousSceneKey, ""));
                    SessionState.SetString(ModeKey, "");
                    deadline = 0;
                }
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

            private static IEnumerator Verify(ItemVFXReview review)
            {
                yield return null;
                Require(review.player != null && review.dummies.All(enemy => enemy.isActiveAndEnabled && enemy.Status.HP > 0), "Review initialization failed.");
                EnemyBase target = review.dummies[0];
                Vector3 point = target.transform.position;
                float hp = target.Status.HP;
                review.SelectItem("11");
                Require(review.preview == review.circle && review.preview.gameObject.activeSelf && review.preview.transform.localScale.x == 8,
                    "Circle Preview selection failed.");
                review.AimAt(point, Vector3.up);
                Require(review.ConfirmTarget(point), "Real ConfirmUse rejected the review cast.");
                Require(review.preview == null && target.Status.HP == hp, "Preview hiding or delayed application changed.");
                yield return new WaitForSeconds(1.2f);
                Require(target.Status.HP < hp && ElementManager.Instance.TryGet(target, ElementType.Shock, out _), "Lightning did not damage/shock a real dummy.");
                review.SelectItem("10");
                Require(review.preview == review.rectangle && review.preview.transform.localScale == new Vector3(10, 1, 2), "Rectangle metadata was not applied.");
                review.SelectItem("6");
                Require(review.armed && review.preview == null, "None must aim without a Preview in this placement test.");
                Require(review.ConfirmTarget(point), "Decoy cast rejected.");
                float decoyDeadline = Time.time + 4;
                while (FindObjectsByType<DecoyController>(FindObjectsSortMode.None).Length == 0 && Time.time < decoyDeadline) yield return null;
                Require(FindObjectsByType<DecoyController>(FindObjectsSortMode.None).Any(decoy => decoy.GetComponentsInChildren<Renderer>().Length > 0),
                    "Actual Decoy model did not appear.");
                review.SelectItem("20");
                Require(review.owned["20"].IsEquipped, "Passive ID did not equip.");
                review.SelectItem("20");
                Require(!review.owned.ContainsKey("20"), "Passive ID did not toggle off.");
                review.ResetReview();
                yield return null;
                Require(review.dummies.Length == 8 && review.dummies.All(enemy => enemy != null && enemy.Status.HP == enemy.Status.MaxHealth) &&
                    review.owned.Count == 0 && review.player.CurrentHealth == review.player.MaxHealth &&
                    FindObjectsByType<DecoyController>(FindObjectsSortMode.None).Length == 0, "Full reset left effects, equipment or damaged dummies.");

                int screenCanvasCount = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length;
                Item adrenaline = review.ReviewScreenEffect("13");
                var adrenalineFx = ItemScreenVfx.GetActive(adrenaline);
                Require(adrenalineFx != null && !review.armed && BuffManager.Instance.Percent(review.player, BuffStat.AttackSpeed) > 0,
                    "Adrenaline quick review did not cast immediately through the real skill slot.");
                Require(review.ReviewScreenEffect("13") == adrenaline && ItemScreenVfx.GetActive(adrenaline) == adrenalineFx,
                    "Quick review must refresh the equipped item without adding another Canvas.");

                Item repair = review.ReviewScreenEffect("14");
                Require(repair != null && !repair.IsEquipped && !review.owned.ContainsKey("14") && review.skills.GetSkill(0) == null &&
                    Mathf.Abs(review.player.CurrentHealth / review.player.MaxHealth - .8f) < .001f && ItemScreenVfx.GetActive(repair) != null,
                    "Repair review must prepare health, heal immediately, consume itself and keep its Canvas alive.");
                float beforeConversion = review.player.CurrentHealth;
                Item conversion = review.ReviewScreenEffect("15");
                Require(ItemScreenVfx.GetActive(conversion) != null &&
                    Mathf.Abs(beforeConversion - review.player.CurrentHealth - review.player.MaxHealth * .05f) < .01f &&
                    BuffManager.Instance.Percent(review.player, BuffStat.Damage) > 0, "Life Conversion review skipped its real health cost or buff.");

                Item tenacity = review.ReviewScreenEffect("17");
                var tenacityFx = ItemScreenVfx.GetActive(tenacity);
                Require(tenacityFx != null && review.ReviewScreenEffect("17") == tenacity && ItemScreenVfx.GetActive(tenacity) == tenacityFx &&
                    review.owned.ContainsKey("13") && review.owned.ContainsKey("15"),
                    "Tenacity quick review must trigger actual hit conditions repeatedly and preserve other equipment.");

                Item resurrection = review.ReviewScreenEffect("25");
                Require(resurrection != null && !resurrection.IsEquipped && !review.owned.ContainsKey("25") &&
                    Mathf.Abs(review.player.CurrentHealth / review.player.MaxHealth - .5f) < .001f && ItemScreenVfx.GetActive(resurrection) != null,
                    "Resurrection quick review must prevent actual lethal damage and consume its item while its Canvas remains visible.");
                Item secondResurrection = review.ReviewScreenEffect("25");
                Require(secondResurrection != resurrection && ItemScreenVfx.GetActive(secondResurrection) != null,
                    "A consumed Resurrection must be reacquired for repeat review.");

                Item rage = review.ReviewScreenEffect("27");
                Require(ItemScreenVfx.GetActive(rage)?.RageStep == 3, "Rage quick review must start at forty-percent health.");
                foreach (int percent in new[] { 80, 60, 40, 20, 100 })
                {
                    Require(review.ReviewScreenEffect("27", percent) == rage, "Rage stage selection replaced the equipped item.");
                    int step = percent == 100 ? 0 : (100 - percent) / 20;
                    Require(Mathf.Abs(review.player.CurrentHealth / review.player.MaxHealth - percent / 100f) < .001f &&
                        ItemScreenVfx.GetActive(rage)?.RageStep == step, "Rage review did not apply the requested exact health stage " + percent);
                }
                yield return new WaitForSeconds(.5f);
                Require(ItemScreenVfx.GetActive(rage) == null && ItemScreenVfx.GetActive(adrenaline) != null,
                    "Rage recovery must fade its own Canvas without clearing another item.");
                review.ResetReview();
                yield return null; yield return null;
                Require(review.screenVfx.GetComponentsInChildren<ItemScreenVfx>(true).Length == 6 &&
                    review.screenVfx.GetComponentsInChildren<ItemScreenVfx>(true).All(layer => !layer.IsPlaying) &&
                    new[] { adrenaline, repair, conversion, tenacity, resurrection, secondResurrection, rage }
                        .All(item => ItemScreenVfx.GetActive(item) == null), "Full reset must clear sources and retain the six reusable screen layers.");
                Require(FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length == screenCanvasCount,
                    "Using screen items must not create additional Canvases.");
            }

            private static void Finish(Exception error)
            {
                deadline = 0;
                File.WriteAllText(Report, error == null
                    ? "CHECK PASS: PJS camera/Map; 8 enabled enemy types in a visible row; Circle/Rectangle/None; real ConfirmUse damage+Shock; Decoy model; passive toggle; six screen VFX quick reviews through actual item conditions; Repair/Resurrection consumption and replay; exact Rage health stages/recovery; equipment preservation; full reset."
                    : error.ToString());
                if (error != null) Debug.LogException(error);
                else Debug.Log("[ItemVFXReview] Native scene and runtime checks passed.");
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            }

            private static void Require(bool condition, string message)
            { if (!condition) throw new InvalidOperationException(message); }
        }
#endif
    }
}
