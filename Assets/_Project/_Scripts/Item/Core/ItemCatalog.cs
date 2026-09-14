using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    // 새 아이템은 Meta와 new Runtime/new Effect를 반환하는 제작 함수 한 개를 등록한다.
    public static class ItemCatalog
    {
        private static readonly Dictionary<string, (ItemMeta meta, Func<ItemRuntime> create)> entries = new();
        private static bool initialized;
        public static string Normalize(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid)) return "";
            string value = guid.Trim();
            if (value.StartsWith("Item", StringComparison.OrdinalIgnoreCase)) value = value.Substring(4).TrimStart('_');
            return int.TryParse(value, out int number) ? number.ToString() : guid.Trim();
        }
        public static void Register(ItemMeta meta, Func<ItemRuntime> createRuntime)
        {
            EnsureDefaults();
            if (meta == null || createRuntime == null) throw new ArgumentNullException(nameof(meta));
            if (Normalize(meta.GUID) != meta.GUID) throw new ArgumentException("숫자 ID는 31처럼 정규화하여 등록하세요.");
            entries.Add(meta.GUID, (meta, createRuntime));
        }
        public static ItemMeta GetMeta(string guid)
        {
            EnsureDefaults();
            return entries.TryGetValue(Normalize(guid), out var entry) ? entry.meta : null;
        }
        public static ItemData Create(string guid)
        {
            EnsureDefaults();
            return entries.TryGetValue(Normalize(guid), out var entry) ? new ItemData(entry.meta, entry.create()) : null;
        }
        public static IEnumerable<ItemMeta> AllMeta
        {
            get { EnsureDefaults(); foreach (var entry in entries.Values) yield return entry.meta; }
        }
        private static void EnsureDefaults()
        {
            if (initialized) return;
            initialized = true;
            // 표에 미정인 가격/반경/시간/쿨타임은 이곳의 테스트 기본값으로 조정한다.
            Register(new ItemMeta("31", "가속 장치", "5초 동안 액티브 쿨타임 감소 속도가 30% 증가합니다.",
                100, ItemKind.Active, DemoIcon(new Color(1f, 0.75f, 0.15f))),
                () => new ItemRuntime(ItemTrigger.SkillUse, new AcceleratorEffect(30f, 5f), cooldown: 12f));
            Register(new ItemMeta("33", "안개", "5초 동안 반경 3 안의 적이 받는 피해량이 30% 증가합니다.",
                200, ItemKind.Active, DemoIcon(new Color(0.45f, 0.8f, 1f))),
                () => new ItemRuntime(ItemTrigger.SkillUse, new FogEffect(3f, 30f, 5f), cooldown: 10f,
                    previewMode: ItemPreviewMode.Ground,
                    preview: new ItemPreviewDefinition(ItemPrefabs.PreviewPath, new Vector3(6f, 1f, 6f))));
        }
        // 테스트 아이콘. 실제 아트 작업 시 Meta의 ItemSprite/ActiveSkillIcon만 교체한다.
        private static Sprite DemoIcon(Color color)
        {
            var texture = new Texture2D(16, 16) { name = "Item test icon", filterMode = FilterMode.Point };
            var pixels = new Color[256];
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                pixels[y * 16 + x] = x < 2 || y < 2 || x > 13 || y > 13 ? Color.white : color;
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            entries.Clear(); initialized = false;
        }
    }
}
