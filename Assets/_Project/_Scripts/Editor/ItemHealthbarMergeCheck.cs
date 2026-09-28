using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Ujam.Runtime.Item;
using UJam.Runtime.Enemy;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using UJam.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ujam.Editor.Integration
{
    /// <summary>1차 브랜치 통합 검사. 메뉴 또는 Unity -executeMethod로 카탈로그, 피해 통지, 착탄 통지, 씬 연결을 확인한다.</summary>
    public static class ItemHealthbarMergeCheck
    {
        public const string ScenePath = "Assets/_Project/Scenes/_MainScene 1.unity";
        private static int checks;

        [MenuItem("Tools/Ujam/Integration/Check item and healthbar merge")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode를 종료한 뒤 검사하세요.");
            checks = 0;
            Require(ItemCatalog.IDs.Count() == 30, "30개 아이템");
            foreach (string id in ItemCatalog.IDs)
            {
                Item first = ItemCatalog.Create(id), second = ItemCatalog.Create(id);
                Require(first != null && !ReferenceEquals(first.Runtime, second.Runtime), "Runtime 개체 분리: " + id);
            }

            CheckDamageNotifications();
            CheckLandingNotifications();
            CheckSceneAndPrefabs();
            Debug.Log("[ItemHealthbarMergeCheck] PASS: " + checks + " checks; item triggers, damage/landing notifications and review scene references.");
        }

        private static void CheckDamageNotifications()
        {
            var host = new GameObject("MergeCheck.EnemyStatus");
            var status = host.AddComponent<EnemyStatus>();
            int uiCalls = 0, healthCalls = 0, deaths = 0;
            float reportedDamage = 0, reportedHealth = 0;
            Action<EnemyStatus, float, float, float> onDamage = (target, hp, max, damage) =>
            {
                if (target != status) return;
                uiCalls++; reportedDamage = damage; reportedHealth = hp;
            };
            EnemyStatus.DamageReported += onDamage;
            status.OnEnemyHpChanged += (_, __) => healthCalls++;
            status.Died += () => deaths++;
            try
            {
                status.SetDefaultHealth(100);
                status.init();
                Require(status.ApplyDamage(float.NaN) == 0 && uiCalls == 0, "무효 피해는 UI에 통지하지 않음");
                Require(status.ApplyDamage(25) == 25 && status.HP == 75, "기본 피해");
                Require(uiCalls == 1 && healthCalls == 1 && reportedDamage == 25 && reportedHealth == 75, "체력바/피격 플래시 통지");
                Require(status.ApplyDamage(200) == 75 && reportedDamage == 75, "초과 피해도 실제 감소량만 반환/통지");
                Require(status.ApplyDamage(1) == 0 && deaths == 1 && uiCalls == 2, "사망/피해 통지 중복 방지");
            }
            finally { EnemyStatus.DamageReported -= onDamage; UnityEngine.Object.DestroyImmediate(host); }
        }

        private static void CheckLandingNotifications()
        {
            var host = new GameObject("MergeCheck.PlayerShooter");
            var shooter = host.AddComponent<PlayerShooter>();
            int landed = 0, enemyArrivals = 0, itemTriggers = 0;
            shooter.OnShotLanded += (_, __) => landed++;
            shooter.OnShootingHit += _ => enemyArrivals++;
            Action<ItemEvent> onShot = _ => itemTriggers++;
            EventManager.Instance.Subscribe(ItemTrigger.Shooting, onShot);
            try
            {
                var move = typeof(PlayerShooter).GetMethod("MoveBulletVisual", BindingFlags.NonPublic | BindingFlags.Instance);
                Require(move != null, "착탄 코루틴 존재");
                foreach (ShotResult result in new[] { ShotResult.Miss, ShotResult.World, ShotResult.Enemy })
                {
                    var signal = result == ShotResult.Enemy ? new ItemEvent(ItemTrigger.Shooting, null) : null;
                    var routine = (IEnumerator)move.Invoke(shooter, new object[] {
                        null, Vector3.zero, Vector3.zero, 0f, signal, result, default(RaycastHit) });
                    Require(!routine.MoveNext(), "이동 완료 시 통지: " + result);
                }
                Require(landed == 2 && enemyArrivals == 1, "벽/적 착탄 통지와 적 도착 통지 보존");
                Require(itemTriggers == 0, "시각 효과 완료가 아이템 트리거를 중복 발행하지 않음");
            }
            finally { EventManager.Instance.Unsubscribe(ItemTrigger.Shooting, onShot); UnityEngine.Object.DestroyImmediate(host); }
        }

        private static void CheckSceneAndPrefabs()
        {
            foreach (string name in new[] { "Kog", "Tee" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemy/" + name + ".prefab");
                Require(prefab != null && prefab.GetComponentInChildren<EnemyHitFlash>(true) != null, name + " 피격 플래시");
            }
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                Require(!components.Any(c => c == null), "검토 씬의 스크립트 참조");
                var tester = components.OfType<ItemPipelineTester>().Single();
                var healthbar = components.OfType<EnemyHealthBarManager>().Single();
                var impact = components.OfType<ShootImpactFx>().Single();
                Require(components.OfType<PlayerInventory>().Count() == 1, "인벤토리 연결 대상");
                RequireReference(tester, "inventory");
                RequireReference(healthbar, "_barParent");
                RequireReference(healthbar, "_barPrefab");
                RequireReference(impact, "_shooter");
                RequireReference(impact, "_enemyImpactFx");
                RequireReference(impact, "_worldImpactFx");
                var combat = components.OfType<PlayerCombatManager>().Single();
                RequireReference(combat, "_aimCamera");
                RequireReference(combat, "_playerStatus");
                RequireReference(combat, "_playerSkillManager");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void RequireReference(UnityEngine.Object owner, string property)
        {
            var serialized = new SerializedObject(owner);
            Require(serialized.FindProperty(property)?.objectReferenceValue != null, owner.GetType().Name + "." + property);
        }
        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("[ItemHealthbarMergeCheck] FAIL: " + label);
            checks++;
        }
    }
}
