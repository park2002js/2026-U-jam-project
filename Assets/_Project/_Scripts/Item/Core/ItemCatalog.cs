using System;
using System.Collections.Generic;

namespace Ujam.Runtime.Item
{
    /// <summary>
    /// 아이템 제작/수치 조정의 첫 진입점. 아래 new Item의 Meta/Effect 생성자 인자만 수정한다.
    /// 새 아이템은 전용 Effect 클래스와 여기의 new Item 한 개를 추가한다.
    /// CSV에 없는 가격은 등급×100, 은화살 추가 피해는 공격력 100%, 미끼 도발은 3칸을 기본값으로 정했다.
    /// 파도는 비고의 판정 폭 5×1칸, 전체 이동 거리 10칸을 따른다. 21번의 전류 표기는 출혈로 해석했다.
    /// </summary>
    public static class ItemCatalog
    {
        private static readonly Item[] definitions =
        {
            // 1. 집중 사격
            new Item("1", new ItemMeta("집중 사격", "스킬, 원형", price: 100, cooldown: 10f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Instant, target: ItemTarget.Enemies,
                description: "특정 영역을 향해 집중적으로 사격을 가합니다. 해당 위치에 있는 적들은 Shooting 공격을 받은 것과 같은 피해를 입습니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new FocusedFireEffect(radius: 0.5f, shots: 5, damagePercent: 100f))),

            // 2. 화염구
            new Item("2", new ItemMeta("화염구", "스킬, 원형, 속성", price: 100, cooldown: 10f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Normal, target: ItemTarget.Enemies,
                description: "전장에 화염구가 떨어져서 적들에게 피해와 동시에 화상을 부여합니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new FireballEffect(radius: 3f, damagePercent: 200f, delay: 1f))),

            // 3. 약화
            new Item("3", new ItemMeta("약화", "스킬, 원형, 디버프, 장판B", price: 100, cooldown: 10f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Normal, target: ItemTarget.Enemies,
                description: "적들을 약화시킵니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new WeakenEffect(radius: 2f, percent: 10f, duration: 5f, prefabPath: ItemAssets.AreaPath))),

            // 4. 안개
            new Item("4", new ItemMeta("안개", "스킬, 원형, 디버프, 장판B", price: 100, cooldown: 10f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Normal, target: ItemTarget.Enemies,
                description: ""),
                new ItemRuntime(ItemTrigger.SkillUse, new FogEffect(radius: 2f, percent: 20f, duration: 5f, prefabPath: ItemAssets.AreaPath))),

            // 5. 중력장
            new Item("5", new ItemMeta("중력장", "스킬, 원형, 디버프, 장판B", price: 100, cooldown: 10f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Normal, target: ItemTarget.Enemies,
                description: ""),
                new ItemRuntime(ItemTrigger.SkillUse, new GravityFieldEffect(radius: 2f, percent: 20f, duration: 5f, prefabPath: ItemAssets.AreaPath))),

            // 6. 미끼덫
            new Item("6", new ItemMeta("미끼덫", "스킬", price: 100, cooldown: 10f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Instant, target: ItemTarget.Enemies,
                description: "적들의 어그로를 끌어주는 미끼입니다. 설치된 순간부터 천천히 체력이 감소합니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new DecoyTrapEffect(health: 100f, duration: 20f, radius: 3f, prefabPath: ItemAssets.AreaPath))),

            // 7. 냉각
            new Item("7", new ItemMeta("냉각", "스킬, 원형, 디버프, 속성", price: 200, cooldown: 10f,
                grade: 2, kind: ItemKind.Active, castType: ItemCastType.Normal, target: ItemTarget.Enemies,
                description: "전장에 빙결마법을 걸어서 적들의 행동을 제한한다."),
                new ItemRuntime(ItemTrigger.SkillUse, new CoolingEffect(radius: 1f, delay: 1f, stunDuration: 1f))),

            // 8. 가로 폭격
            new Item("8", new ItemMeta("가로 폭격", "스킬, 가로선", price: 200, cooldown: 10f,
                grade: 2, kind: ItemKind.Active, castType: ItemCastType.Instant, target: ItemTarget.Enemies,
                description: "전장의 가로줄에다가 폭격을 가해 적들을 쓸어버립니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new HorizontalBombardmentEffect(thickness: 1f, delay: 2f, damagePercent: 150f))),

            // 9. 십자 폭격
            new Item("9", new ItemMeta("십자 폭격", "스킬, 가로선, 세로선", price: 200, cooldown: 10f,
                grade: 2, kind: ItemKind.Active, castType: ItemCastType.Instant, target: ItemTarget.Enemies,
                description: "2번의 폭격을 가해 적들에게 피해를 입힙니다. 십자가 겹치는 부분의 적들은 추가 피해를 입습니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new CrossBombardmentEffect(thickness: 1f, delay: 2f, damagePercent: 120f))),

            // 10. 파도
            new Item("10", new ItemMeta("파도", "스킬, 직사각형, 디버프, 속성", price: 200, cooldown: 10f,
                grade: 2, kind: ItemKind.Active, castType: ItemCastType.Normal, target: ItemTarget.Enemies,
                description: "파도를 생성합니다. 파도는 이동하며 적들을 밀어내고 빙결을 부여합니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new WaveEffect(width: 5f, depth: 1f, distance: 10f, duration: 10f, interval: 0.2f, push: 0.5f))),

            // 11. 낙뢰
            new Item("11", new ItemMeta("낙뢰", "스킬, 원형, 속성", price: 100, cooldown: 10f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Normal, target: ItemTarget.Enemies,
                description: "전장에 낙뢰가 떨어져서 적들에게 피해와 동시에 감전을 부여합니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new LightningEffect(radius: 2f, damagePercent: 200f, delay: 1f,
                    boltPath: ItemAssets.BoltPath, groundPath: ItemAssets.GroundPath,
                    strikeHeight: 10f, boltWidth: 0.1f, boltLifetime: 0.15f, jitter: 0.3f,
                    groundScale: 0.3f, groundLifetime: 1.5f, segments: 8))),

            // 12. 돌풍
            new Item("12", new ItemMeta("돌풍", "스킬, 원형, 속성", price: 100, cooldown: 10f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Normal, target: ItemTarget.Enemies,
                description: "전장에 돌풍을 생성합니다. 돌풍은 해당 위치의 적들에게 바람 속성을 부여합니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new GustEffect(radius: 2f))),

            // 13. 아드레날린
            new Item("13", new ItemMeta("아드레날린", "스킬, 버프", price: 100, cooldown: 30f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "아드레날린 주사를 투여하여 일시적으로 공격력과 공격 속도를 상승시킵니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new AdrenalineEffect(attack: 10f, speed: 10f, duration: 5f))),

            // 14. 긴급 수리
            new Item("14", new ItemMeta("긴급 수리", "스킬, 회복, 스택", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Active, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "사용 즉시 거점을 보수하여 체력을 회복시킵니다. 3번만 사용가능하며, 3번째로 사용할 경우 해당 아이템은 삭제됩니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new EmergencyRepairEffect(healPercent: 10f, uses: 3))),

            // 15. 생명력 전환
            new Item("15", new ItemMeta("생명력 전환", "스킬, 버프", price: 200, cooldown: 30f,
                grade: 2, kind: ItemKind.Active, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "플레이어의 현재 체력을 대가로, 일시적으로 강력한 공격을 가할 수 있게 됩니다. 체력이 부족한 상황에서 해당 스킬 사용시 사망할 수 있습니다."),
                new ItemRuntime(ItemTrigger.SkillUse, new LifeConversionEffect(healthCost: 5f, damagePercent: 10f, duration: 20f))),

            // 16. 은화살
            new Item("16", new ItemMeta("은화살", "슈팅추가효과, 스택", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "계속해서 적을 맞추면 적에게 추가 피해를 입힙니다."),
                new ItemRuntime(ItemTrigger.Shooting, new SilverArrowEffect(hits: 5, damagePercent: 100f))),

            // 17. 집념
            new Item("17", new ItemMeta("집념", "슈팅추가효과, 스택, 버프", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "계속해서 적을 맞추면 플레이어의 공격 속도가 일시적으로 빨라집니다."),
                new ItemRuntime(ItemTrigger.Shooting, new TenacityEffect(hits: 5, percent: 10f, duration: 2f))),

            // 18. 화염 탄환
            new Item("18", new ItemMeta("화염 탄환", "슈팅추가효과, 속성", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "플레이어의 슈팅 공격에 랜덤한 확률로 화상 속성을 부여합니다."),
                new ItemRuntime(ItemTrigger.Shooting, new FireBulletEffect(chance: 10f))),

            // 19. 얼음 탄환
            new Item("19", new ItemMeta("얼음 탄환", "슈팅추가효과, 속성", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "플레이어의 슈팅 공격에 랜덤한 확률로 빙결 속성을 부여합니다."),
                new ItemRuntime(ItemTrigger.Shooting, new IceBulletEffect(chance: 10f))),

            // 20. 전류 탄환
            new Item("20", new ItemMeta("전류 탄환", "슈팅추가효과, 속성", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "플레이어의 슈팅 공격에 랜덤한 확률로 감전 속성을 부여합니다."),
                new ItemRuntime(ItemTrigger.Shooting, new ElectricBulletEffect(chance: 10f))),

            // 21. 가시 탄환
            new Item("21", new ItemMeta("가시 탄환", "슈팅추가효과, 속성", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "플레이어의 슈팅 공격에 랜덤한 확률로 출혈 속성을 부여합니다."),
                new ItemRuntime(ItemTrigger.Shooting, new ThornBulletEffect(chance: 10f))),

            // 22. 과부화
            new Item("22", new ItemMeta("과부화", "스킬, 속성, 스택", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "플레이어의 무기가 일정 횟수 사용되면 과부화를 일으켜 추가피해와 함께 감전 속성을 부여합니다."),
                new ItemRuntime(ItemTrigger.SkillHit, new OverloadEffect(uses: 5, damagePercent: 100f))),

            // 23. 염화
            new Item("23", new ItemMeta("염화", "버프, 속성", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "더 높은 고온의 불을 사용해 화상 속성 데미지를 상승시킵니다."),
                new ItemRuntime(ItemTrigger.Equipped, new FlameEnhancementEffect(percent: 20f))),

            // 24. 시체 폭발
            new Item("24", new ItemMeta("시체 폭발", "버프, 원형", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "특정 번째로 죽은 적에게 폭발을 일으켜 그 주변에 피해를 입힙니다."),
                new ItemRuntime(ItemTrigger.EnemyKilled, new CorpseExplosionEffect(kills: 4, radius: 1f, damagePercent: 100f))),

            // 25. 리저렉션
            new Item("25", new ItemMeta("리저렉션", "버프", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "플레이어의 죽음을 1회 막고, 체력을 회복합니다. 자동 사용되며 한번 사용되면 사라집니다."),
                new ItemRuntime(ItemTrigger.BeforeDeath, new ResurrectionEffect(healPercent: 50f))),

            // 26. 생명력 흡수
            new Item("26", new ItemMeta("생명력 흡수", "버프", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "적에게 Shooting으로 입힌 피해의 일부만큼 체력을 회복합니다."),
                new ItemRuntime(ItemTrigger.ShootingResolved, new LifeStealEffect(percent: 2f))),

            // 27. 광전사의 분노
            new Item("27", new ItemMeta("광전사의 분노", "버프", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "체력이 낮아질 수록, 공격력과 공격속도가 상승합니다."),
                new ItemRuntime(ItemTrigger.HealthChanged, new BerserkerRageEffect(stepPercent: 4f))),

            // 28. 벽돌
            new Item("28", new ItemMeta("벽돌", "버프", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "최대 체력을 늘려줍니다."),
                new ItemRuntime(ItemTrigger.Equipped, new BrickEffect(percent: 10f))),

            // 29. 마공학 장치
            new Item("29", new ItemMeta("마공학 장치", "버프", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "스킬 쿨타임이 감소합니다."),
                new ItemRuntime(ItemTrigger.Equipped, new MagitechDeviceEffect(percent: 10f))),

            // 30. 수전노
            new Item("30", new ItemMeta("수전노", "버프", price: 100, cooldown: 0f,
                grade: 1, kind: ItemKind.Passive, castType: ItemCastType.Instant, target: ItemTarget.Player,
                description: "적을 쓰러뜨려서 얻는 획득 재화량이 증가합니다. 소수점 이하는 반올림합니다."),
                new ItemRuntime(ItemTrigger.Equipped, new MiserEffect(percent: 10f)))
        };
        private static readonly Dictionary<string, Item> byId = BuildIndex();
        private static Dictionary<string, Item> BuildIndex()
        {
            var result = new Dictionary<string, Item>(StringComparer.Ordinal);
            foreach (var item in definitions) result.Add(item.ID, item);
            return result;
        }

        /// <summary>상점이 현재 등록된 상품 ID 목록을 얻을 때 사용한다.</summary>
        public static IEnumerable<string> IDs => byId.Keys;
        /// <summary>Shop/Inventory UI는 실행 객체를 만들지 않고 이 API로 메타만 조회한다.</summary>
        public static ItemMeta GetMeta(string id) => byId.TryGetValue(Normalize(id), out var item) ? item.Meta : null;
        /// <summary>외부 도구가 Trigger/Effect 설정을 조회할 때 사용한다. 보유 아이템의 상태는 Inventory의 Item.Runtime에서 읽는다.</summary>
        public static ItemRuntime GetRuntime(string id) => Create(id)?.Runtime;
        /// <summary>구매/테스트 시 새 보유 개체를 만든다. 같은 ID도 Runtime과 카운터는 공유하지 않는다.</summary>
        public static Item Create(string id) => byId.TryGetValue(Normalize(id), out var item) ? item.Copy() : null;
        /// <summary>ID 조회와 함께 보유 가능한 새 Item을 얻는 API. Create와 같은 동작이다.</summary>
        public static Item Get(string id) => Create(id);
        /// <summary>테스트/추가 콘텐츠에서 정의를 등록한다. ID는 Normalize한 표기를 사용하고 기존 ID를 덮어쓰지 않는다.</summary>
        public static void Register(Item item)
        {
            if (item == null || item.IsEquipped) throw new ArgumentException("장착되지 않은 정의가 필요합니다.");
            if (item.ID != Normalize(item.ID)) throw new ArgumentException("Item.ID에는 정규화된 ID를 사용하세요.");
            byId.Add(item.ID, item);
        }
        /// <summary>기존 Item001/Item_001 표기를 CSV의 숫자 문자열 ID로 통일한다.</summary>
        public static string Normalize(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "";
            string value = id.Trim();
            if (value.StartsWith("Item", StringComparison.OrdinalIgnoreCase)) value = value.Substring(4).TrimStart('_');
            return int.TryParse(value, out int number) ? number.ToString() : id.Trim();
        }
    }
}
