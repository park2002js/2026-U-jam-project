using System.Collections;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>미끼덫: 단위 Collider Prefab을 설치하고 미끼 컴포넌트에 체력 감소/도발/수명을 위임한다.</summary>
    public class DecoyTrapEffect : ItemEffect
    {
        private readonly float health, duration, radius, delay, fallDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight, incomingDelay;
        private readonly string prefabPath;
        /// <summary>Catalog에서 체력/수명/도발 반경/Prefab 경로를 정한다.</summary>
        public DecoyTrapEffect(float health, float duration, float radius, string prefabPath, float delay = 1f,
            float fallDuration = 1f, float incomingSpeed = 1f, float impactSpeed = 1f, float impactLifetime = 1.5f,
            float spawnHeight = 10f, float incomingDelay = 0f)
        {
            this.health = health; this.duration = duration; this.radius = radius; this.prefabPath = prefabPath;
            this.delay = delay; this.fallDuration = fallDuration; this.incomingSpeed = incomingSpeed;
            this.impactSpeed = impactSpeed; this.impactLifetime = impactLifetime; this.spawnHeight = spawnHeight; this.incomingDelay = incomingDelay;
        }
        /// <summary>중앙 SkillUse가 호출한다. 지정 위치에 미끼를 설치한다.</summary>
        public override void Execute(ItemUseContext c) => c.Run(Spawn(c));
        private IEnumerator Spawn(ItemUseContext c)
        {
            GameObject prefab = null;
            yield return ItemAssets.LoadAsync(prefabPath, loaded => prefab = loaded);
            if (prefab == null || !c.Item.IsEquipped) yield break;

            ItemCastVfx visual = null;
            yield return ItemCastVfx.Create(c, value => visual = value);
            if (!c.Item.IsEquipped) yield break;
            if (visual != null) visual.BeginIncoming(spawnHeight, fallDuration, incomingSpeed, incomingDelay);
            yield return new WaitForSeconds(delay);
            if (!c.Item.IsEquipped) yield break;

            var instance = Object.Instantiate(prefab, c.Position, Quaternion.identity);
            instance.SetActive(false);
            instance.name = "미끼덫";
            instance.transform.localScale = Vector3.one * (radius * ItemWorld.CellWidth * 2);

            var decoy = instance.AddComponent<DecoyController>();
            decoy.Initialize(health, duration, c.EnemyMask, enemy => c.ReportHits(new[] { enemy }));
            c.Item.Runtime.Track(instance);

            if (visual != null)
            {
                visual.CreateSettled(instance.transform);
                visual.Impact(radius * ItemWorld.CellWidth * 2, impactSpeed, impactLifetime);
            }
            instance.SetActive(true);
        }
    }
}
