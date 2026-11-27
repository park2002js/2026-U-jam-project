using System.Text;
using UJam.Runtime.Enemy;
using UJam.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace UJam.EditorTools
{
    /// <summary>
    /// 개발용 밸런스 검증(플레이 모드 전용): 적 프리팹을 멀리 생성해 실제 피해 계산(PlayerStatus.DealDamage)으로
    /// 평타·시전 스킬 몇 번에 쓰러지는지 셉니다. 화상은 틱 피해(평타×계수×5틱)를, 감전은 실제 받는 피해 증가 버프를 적용합니다.
    /// </summary>
    internal static class BalanceTest
    {
        private static readonly string[] Enemies = { "Sniper_Eyebeast", "Rollbug", "Big_Golem", "Golem", "Eyebeast", "Witch", "Skeleton", "Slime" };

        public static string Run()
        {
            if (!Application.isPlaying) return "play mode only";
            PlayerStatus player = PlayerStatus.Instance != null ? PlayerStatus.Instance : Object.FindAnyObjectByType<PlayerStatus>();
            if (player == null) return "no PlayerStatus";

            float burnPercent = UJam.Runtime.Systems.ElementManager.Instance.BurnAttackPercent;
            StringBuilder sb = new StringBuilder($"attack={player.AttackDamage} dmgMul={player.DamageMultiplier} burn%={burnPercent}\n");
            sb.Append("enemy | HP | 평타 | 화염구(360%+화상) | 낙뢰(550%+감전) | 가로폭격(550%) | 십자 교차(300%x2) | 집중사격(110%x5)\n");
            foreach (string name in Enemies)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Enemy/{name}.prefab");
                int basic = Count(prefab, player, e => player.DealDamage(e, 100f, "test"), out float hp);
                int fire = Count(prefab, player, e =>
                {
                    player.DealDamage(e, 360f, "test");
                    for (int t = 0; t < 5 && e.Status.HP > 0; t++)
                        e.TakeDamage(new UJam.Runtime.Combat.DamageInfo(player.AttackDamage * burnPercent / 100f, "burn", UJam.Runtime.Combat.DamageSourceKind.Player, player));
                }, out _);
                int lightning = Count(prefab, player, e =>
                {
                    player.DealDamage(e, 550f, "test");
                    if (e.Status.HP > 0) UJam.Runtime.Systems.ElementManager.Instance.Apply(e, UJam.Runtime.Systems.ElementType.Shock, player);
                }, out _);
                int horizontal = Count(prefab, player, e => player.DealDamage(e, 550f, "test"), out _);
                int cross = Count(prefab, player, e => { player.DealDamage(e, 300f, "test"); player.DealDamage(e, 300f, "test"); }, out _);
                int focused = Count(prefab, player, e => { for (int i = 0; i < 5; i++) player.DealDamage(e, 110f, "test"); }, out _);
                sb.Append($"{name} | {hp} | {basic} | {fire} | {lightning} | {horizontal} | {cross} | {focused}\n");
            }
            return sb.ToString();
        }

        /// <summary>은화살(실제 아이템 효과)을 장착하고 평타 + 사격 이벤트로 처치 타수를 셉니다. 나머지 패시브는 1회 발동 피해를 보고합니다.</summary>
        public static string Passives()
        {
            if (!Application.isPlaying) return "play mode only";
            PlayerStatus player = PlayerStatus.Instance != null ? PlayerStatus.Instance : Object.FindAnyObjectByType<PlayerStatus>();
            var em = UJam.Runtime.Systems.ElementManager.Instance;
            Ujam.Runtime.Item.Item arrow = Ujam.Runtime.Item.ItemCatalog.Create("16");
            arrow.Equip(player, 1 << LayerMask.NameToLayer("Enemy"));
            StringBuilder sb = new StringBuilder("enemy | HP | 평타 | 평타+은화살 | 시체폭발(300%) | 출혈 1회(15%) | 과부화(200%)\n");
            try
            {
                foreach (string name in Enemies)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Enemy/{name}.prefab");
                    int basic = Count(prefab, player, e => player.DealDamage(e, 100f, "test"), out float hp);
                    int withArrow = Count(prefab, player, e =>
                    {
                        player.DealDamage(e, 100f, "test");
                        UJam.Runtime.Systems.EventManager.Instance.Publish(new Ujam.Runtime.Item.ItemEvent(
                            Ujam.Runtime.Item.ItemTrigger.Shooting, player, e.transform.position, new[] { e }));
                    }, out _);
                    float corpse = player.AttackDamage * 3f, bleed = hp * em.BleedMaxHealthPercent / 100f, overload = player.AttackDamage * 2f;
                    sb.Append($"{name} | {hp} | {basic} | {withArrow} | {corpse} ({corpse / hp:P0}) | {bleed:0.#} | {overload} (+스킬55={55 + overload})\n");
                }
                sb.Append($"화상 틱={player.AttackDamage * em.BurnAttackPercent / 100f} (염화 +10 → {player.AttackDamage * (em.BurnAttackPercent + 10f) / 100f}), 출혈 누적={em.BleedThreshold}회\n");
            }
            finally
            {
                arrow.Unequip();
            }
            return sb.ToString();
        }

        /// <summary>그리드 행마다 화면상의 세로 위치(1080 기준, 위가 0)를 보고합니다. 웨이브 스폰 행이 안개 띠에 가리는지 확인용.</summary>
        public static string GridRows()
        {
            Camera cam = Camera.main;
            var grid = Object.FindAnyObjectByType<UJam.Runtime.Grid.GridSystem>();
            if (cam == null || grid == null) return "no camera/grid";
            StringBuilder sb = new StringBuilder($"cam {cam.pixelWidth}x{cam.pixelHeight}\n");
            for (int row = 0; row < grid.RowCount; row += 1)
            {
                Vector3 world = new Vector3(grid.Origin.x + 7 * grid.CellWidth, 0.1f, grid.Origin.z + row * grid.CellHeight);
                Vector3 sp = cam.WorldToScreenPoint(world);
                sb.Append($"row {row}: y={(1f - sp.y / cam.pixelHeight) * 1080f:0}\n");
            }
            return sb.ToString();
        }

        private static int Count(GameObject prefab, PlayerStatus player, System.Action<EnemyBase> hit, out float maxHp)
        {
            GameObject go = Object.Instantiate(prefab, new Vector3(0, -500, 0), Quaternion.identity);
            EnemyBase enemy = go.GetComponent<EnemyBase>();
            enemy.Status.init();
            maxHp = enemy.Status.MaxHealth;
            int count = 0;
            while (enemy.Status.HP > 0 && count < 100)
            {
                hit(enemy);
                count++;
            }
            Object.Destroy(go);
            return count;
        }
    }
}
