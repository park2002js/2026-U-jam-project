using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UJam.Runtime.Systems;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>미끼덫: 착지 시 Dummy Prefab을 복제하고 체력/크기/수명/도발은 Prefab에 맡긴다.</summary>
    public class DecoyTrapEffect : ItemEffect
    {
        private readonly float delay, fallDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight, incomingDelay;
        private readonly string prefabPath;
        /// <summary>Catalog에서는 Prefab 경로와 낙하/착탄 연출 시점만 정한다.</summary>
        public DecoyTrapEffect(string prefabPath, float delay = 1f,
            float fallDuration = 1f, float incomingSpeed = 1f, float impactSpeed = 1f, float impactLifetime = 1.5f,
            float spawnHeight = 10f, float incomingDelay = 0f) : base("6")
        {
            this.prefabPath = prefabPath;
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
            if (visual != null)
                visual.BeginIncoming(spawnHeight, fallDuration, incomingSpeed, incomingDelay,
                    () => SFXManager.Instance?.PlayAt(GetClip("DecoyDrop"), c.Position));
            yield return new WaitForSeconds(delay);
            if (!c.Item.IsEquipped) yield break;

            var instance = Object.Instantiate(prefab, c.Position, Quaternion.identity);
            instance.name = "미끼덫";

            c.Item.Runtime.Track(instance);
            var decoy = instance.GetComponent<DecoyController>();
            if (decoy == null)
            {
                Object.Destroy(instance);
                throw new System.InvalidOperationException("미끼 Prefab에 DecoyController가 필요합니다.");
            }
            decoy.Taunted += enemy => c.ReportHits(new[] { enemy });
            SFXManager.Instance?.PlayAt(GetClip("DecoyLand"), c.Position);
            // 활성 Prefab은 복제 즉시 Overlap을 끝내므로 이미 도발한 적도 스킬 적중으로 전달한다.
            foreach (var enemy in new List<EnemyBase>(instance.GetComponent<AreaTrigger>().Enemies))
                c.ReportHits(new[] { enemy });

            if (visual != null)
            {
                var sphere = instance.GetComponent<SphereCollider>();
                Vector3 scale = instance.transform.lossyScale;
                float diameter = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)) * 2;
                visual.Impact(diameter, impactSpeed, impactLifetime);
            }
        }
    }
}
