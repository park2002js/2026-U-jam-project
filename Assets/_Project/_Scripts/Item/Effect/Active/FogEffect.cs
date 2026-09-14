using System;
using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    // GUID 33, 장판 B: 첫 Overlap + 이후 Collider Enter/Exit. 적마다 모든 피해가 증가한다.
    public sealed class FogEffect : ItemEffect
    {
        public float Radius { get; }
        public float Percent { get; }
        public float Duration { get; }
        private readonly string prefabPath;
        private GameObject prefab;
        private readonly HashSet<GameObject> areas = new();

        public FogEffect(float radius, float percent, float duration, string prefabPath = ItemPrefabs.AreaPath)
        {
            if (!float.IsFinite(radius) || radius <= 0 || !float.IsFinite(percent) || percent < 0 ||
                !float.IsFinite(duration) || duration <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
            Radius = radius; Percent = percent; Duration = duration; this.prefabPath = prefabPath;
        }
        public override void OnEquip(ItemRuntime runtime)
        {
            runtime.Run(ItemPrefabs.LoadAsync(prefabPath, loaded =>
            {
                if (!runtime.IsEquipped) return;
                if (loaded != null && loaded.GetComponent<AreaTrigger>() != null) prefab = loaded;
                else Debug.LogError("[FogEffect] 장판 Prefab 루트에 AreaTrigger가 필요합니다.");
            }));
        }
        public override bool CanExecute(ItemUseContext context) => prefab != null;
        public override void Execute(ItemUseContext context)
        {
            var area = UnityEngine.Object.Instantiate(prefab, context.Position, Quaternion.identity);
            area.transform.localScale = Vector3.one * (Radius * 2f); // Prefab SphereCollider.radius = 0.5
            areas.Add(area);
            float until = Time.time + Duration;
            var source = new object(); // 겹치는 장판이 서로의 디버프를 해제하지 않는다.
            area.GetComponent<AreaTrigger>().Initialize(context.Runtime.EnemyMask,
                enemy =>
                {
                    float remaining = until - Time.time;
                    if (remaining > 0) TemporaryBuffs.Instance.Apply(enemy.Status, source, BuffStat.DamageTaken, Percent, remaining);
                },
                enemy =>
                {
                    if (enemy != null) TemporaryBuffs.Instance.Remove(enemy.Status, source, BuffStat.DamageTaken);
                });
            context.Runtime.Run(Expire(area, source));
        }
        private IEnumerator Expire(GameObject area, object source)
        {
            yield return new WaitForSeconds(Duration);
            TemporaryBuffs.Instance.RemoveSource(source);
            if (area != null) { area.SetActive(false); UnityEngine.Object.Destroy(area); }
            areas.Remove(area);
        }
        public override void OnUnequip(ItemRuntime runtime)
        {
            foreach (var area in areas)
                if (area != null) { area.SetActive(false); UnityEngine.Object.Destroy(area); }
            areas.Clear();
        }
    }
}
