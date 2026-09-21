using System.Collections;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>낙뢰: 기존 LightningSkill의 LineRenderer 번개와 지면 ParticleSystem 표현을 옮겼다. CSV 규칙대로 지연 후 피해→감전을 처리한다.</summary>
    public sealed class LightningEffect : ItemEffect
    {
        private readonly float radius, damagePercent, delay, strikeHeight, boltWidth, boltLifetime, jitter, groundScale, groundLifetime;
        private readonly int segments;
        private readonly string boltPath, groundPath;

        /// <summary>Catalog에서 게임 수치와 기존 낙뢰 VFX의 모든 조정값/Resources 경로를 지정한다.</summary>
        public LightningEffect(float radius, float damagePercent, float delay, string boltPath, string groundPath,
            float strikeHeight, float boltWidth, float boltLifetime, float jitter, float groundScale, float groundLifetime, int segments)
        {
            this.radius = radius; this.damagePercent = damagePercent; this.delay = delay;
            this.boltPath = boltPath; this.groundPath = groundPath; this.strikeHeight = strikeHeight;
            this.boltWidth = boltWidth; this.boltLifetime = boltLifetime; this.jitter = jitter;
            this.groundScale = groundScale; this.groundLifetime = groundLifetime; this.segments = Mathf.Max(1, segments);
        }
        /// <summary>장착 시 VFX를 미리 비동기 로드한다. Effect에 소유자 상태를 저장하지 않는다.</summary>
        public override void OnEquip(ItemUseContext c) => c.Run(Preload());
        private IEnumerator Preload()
        {
            yield return ItemAssets.LoadAsync(boltPath, _ => { });
            yield return ItemAssets.LoadAsync(groundPath, _ => { });
        }
        /// <summary>중앙 SkillUse가 호출한다. 프리뷰 없이 현재 전달된 마우스 위치를 즉시 확정한다.</summary>
        public override void Execute(ItemUseContext c) => c.Run(Strike(c));
        private IEnumerator Strike(ItemUseContext c)
        {
            /* VFX: 시전 예고 표현이 필요하면 이곳에 추가한다. */
            yield return new WaitForSeconds(delay);
            c.Run(Visuals(c));
            var enemies = ItemWorld.Circle(c.Position, radius, c.EnemyMask);
            foreach (var enemy in enemies) { c.Damage(enemy, damagePercent); c.Element(enemy, ItemElement.Shock); }
            c.ReportHits(enemies);
        }
        private IEnumerator Visuals(ItemUseContext c)
        {
            GameObject boltPrefab = null, groundPrefab = null;
            yield return ItemAssets.LoadAsync(boltPath, loaded => boltPrefab = loaded);
            if (boltPrefab != null)
            {
                var bolt = Object.Instantiate(boltPrefab, c.Position, Quaternion.identity);
                c.Item.Runtime.Track(bolt);
                var line = bolt.GetComponent<LineRenderer>();
                if (line != null)
                {
                    line.useWorldSpace = true;
                    line.textureMode = LineTextureMode.Tile;
                    line.positionCount = segments + 1;
                    Vector3 top = c.Position + Vector3.up * strikeHeight;
                    for (int i = 0; i <= segments; i++)
                    {
                        Vector3 point = Vector3.Lerp(c.Position, top, (float)i / segments);
                        if (i != 0 && i != segments) { point.x += Random.Range(-jitter, jitter); point.z += Random.Range(-jitter, jitter); }
                        line.SetPosition(i, point);
                    }
                    line.startWidth = boltWidth; line.endWidth = boltWidth; line.numCapVertices = 2;
                }
                Object.Destroy(bolt, boltLifetime);
            }
            yield return ItemAssets.LoadAsync(groundPath, loaded => groundPrefab = loaded);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, c.Position, Quaternion.identity);
                c.Item.Runtime.Track(ground);
                foreach (var particle in ground.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = particle.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
                ground.transform.localScale = Vector3.one * (radius * ItemWorld.CellWidth * groundScale);
                Object.Destroy(ground, groundLifetime);
            }
            /* VFX: 추가 사운드/카메라 효과는 이곳에 붙인다. 기존 번개 줄기와 지면 표현은 위에서 수행한다. */
        }
    }
}
