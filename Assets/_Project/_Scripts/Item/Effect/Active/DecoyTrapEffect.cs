using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>미끼덫: 단위 Collider Prefab을 설치하고 미끼 컴포넌트에 체력 감소/도발/수명을 위임한다.</summary>
    public sealed class DecoyTrapEffect : ItemEffect
    {
        private readonly float health, duration, radius;
        private readonly string prefabPath;
        /// <summary>Catalog에서 체력/수명/도발 반경/Prefab 경로를 정한다.</summary>
        public DecoyTrapEffect(float health, float duration, float radius, string prefabPath)
        { this.health = health; this.duration = duration; this.radius = radius; this.prefabPath = prefabPath; }
        /// <summary>중앙 SkillUse가 호출한다. 지정 위치에 미끼를 설치한다.</summary>
        public override void Execute(ItemUseContext c) => c.Run(Spawn(c));
        private IEnumerator Spawn(ItemUseContext c)
        {
            GameObject prefab = null;
            yield return ItemAssets.LoadAsync(prefabPath, loaded => prefab = loaded);
            if (prefab == null || !c.Item.IsEquipped) yield break;
            var instance = Object.Instantiate(prefab, c.Position, Quaternion.identity);
            instance.SetActive(false);
            instance.name = "미끼덫";
            instance.transform.localScale = Vector3.one * (radius * ItemWorld.CellWidth * 2);
            var decoy = instance.AddComponent<DecoyController>();
            decoy.Initialize(health, duration, c.EnemyMask, enemy => c.ReportHits(new[] { enemy }));
            c.Item.Runtime.Track(instance);
            /* VFX: instance 아래에 미끼 모델/수명 표시를 추가한다. 도발 Collider 크기와 모델 크기는 분리한다. */
            instance.SetActive(true);
        }
    }
}
