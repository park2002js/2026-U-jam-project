using System;
using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>공유 공간 조회만 제공한다. 모든 반경/폭 인자는 칸 단위이며 원형 반경은 CellWidth를 기준으로 환산한다.</summary>
    public static class ItemWorld
    {
        public static float CellWidth => GridSystem.Instance != null && GridSystem.Instance.IsInitialized ? GridSystem.Instance.CellWidth : 1f;
        public static float CellHeight => GridSystem.Instance != null && GridSystem.Instance.IsInitialized ? GridSystem.Instance.CellHeight : 1f;

        /// <summary>원형 단발 효과와 A형 틱에서 사용한다. Collider가 여러 개인 적을 한 번만 반환한다.</summary>
        public static List<EnemyBase> Circle(Vector3 center, float radiusCells, LayerMask mask)
        {
            Physics.SyncTransforms();
            return Unique(Physics.OverlapSphere(center, radiusCells * CellWidth, mask, QueryTriggerInteraction.Collide));
        }
        /// <summary>직사각형/가로선/세로선 효과에서 사용한다. width/depth는 반폭이 아닌 전체 칸 수다.</summary>
        public static List<EnemyBase> Box(Vector3 center, float width, float depth, LayerMask mask)
        {
            Physics.SyncTransforms();
            return Unique(Physics.OverlapBox(center, new Vector3(width * CellWidth / 2f, 100f, depth * CellHeight / 2f),
                Quaternion.identity, mask, QueryTriggerInteraction.Collide));
        }
        /// <summary>가로/세로 필드 전체를 조회한다. 중심 고정축과 전체 길이는 GridSystem에서 가져온다.</summary>
        public static List<EnemyBase> Line(Vector3 cursor, float thickness, bool horizontal, LayerMask mask)
        {
            var grid = GridSystem.Instance;
            float columns = grid != null && grid.IsInitialized ? grid.ColumnCount : 32;
            float rows = grid != null && grid.IsInitialized ? grid.RowCount : 32;
            Vector3 origin = grid != null && grid.IsInitialized ? grid.Origin : new Vector3(-15.5f, 0, -15.5f);
            if (horizontal) cursor.x = origin.x + (columns - 1) * CellWidth / 2f;
            else cursor.z = origin.z + (rows - 1) * CellHeight / 2f;
            return Box(cursor, horizontal ? columns : thickness, horizontal ? thickness : rows, mask);
        }
        private static List<EnemyBase> Unique(Collider[] colliders)
        {
            var enemies = new HashSet<EnemyBase>();
            foreach (var collider in colliders)
            {
                var enemy = collider.GetComponentInParent<EnemyBase>();
                if (enemy != null && enemy.Status != null && enemy.Status.HP > 0) enemies.Add(enemy);
            }
            return new List<EnemyBase>(enemies);
        }

        /// <summary>B형 장판 공통 설치. 단위 구체 Prefab을 복제/스케일하고 초기 Overlap 및 Enter/Exit로 조건형 디버프를 관리한다.</summary>
        public static IEnumerator ConditionArea(ItemUseContext context, string prefabPath, float radius, float seconds,
            BuffStat stat, float percent)
        {
            GameObject prefab = null;
            yield return ItemAssets.LoadAsync(prefabPath, loaded => prefab = loaded);
            if (prefab == null || !context.Item.IsEquipped) yield break;
            var area = UnityEngine.Object.Instantiate(prefab, context.Position, Quaternion.identity);
            context.Item.Runtime.Track(area);
            area.transform.localScale = Vector3.one * (radius * CellWidth * 2);
            var relay = area.GetComponent<AreaTrigger>();
            if (relay == null) { UnityEngine.Object.Destroy(area); throw new InvalidOperationException("장판 Prefab에 AreaTrigger가 필요합니다."); }
            relay.Initialize(context.EnemyMask,
                enemy =>
                {
                    BuffManager.Instance.SetCondition(enemy.Status, area, stat, percent);
                    context.ReportHits(new[] { enemy });
                },
                enemy => { if (enemy != null) BuffManager.Instance.Remove(enemy.Status, area, stat); });
            /* VFX: area의 자식으로 장판 표현을 붙인다. 판정 Collider와 시각 크기를 분리한다. */
            try { yield return new WaitForSeconds(seconds); }
            finally
            {
                BuffManager.Instance.RemoveSource(area);
                if (area != null) { area.SetActive(false); UnityEngine.Object.Destroy(area); }
            }
        }
    }

    /// <summary>프리팹을 Resources.LoadAsync로 불러오는 공통 진입점. 경로는 Resources 이후 부분이며 확장자는 생략한다.</summary>
    public static class ItemAssets
    {
        private static readonly Dictionary<string, GameObject> cache = new();
        public const string AreaPath = "ItemPipeline/TriggerSphere";
        public const string BoltPath = "ItemPipeline/LightningBolt";
        public const string GroundPath = "ItemPipeline/LightningGround";

        /// <summary>Effect의 코루틴에서 yield return으로 호출한다. 실패하면 null을 돌려주고 오류 경로를 남긴다.</summary>
        public static IEnumerator LoadAsync(string path, Action<GameObject> loaded)
        {
            if (cache.TryGetValue(path, out var existing) && existing != null) { loaded(existing); yield break; }
            var request = Resources.LoadAsync<GameObject>(path);
            yield return request;
            var prefab = request.asset as GameObject;
            if (prefab != null) cache[path] = prefab;
            if (prefab == null) Debug.LogError($"[ItemAssets] Resources/{path}.prefab 로드 실패");
            loaded(prefab);
        }
    }
}
