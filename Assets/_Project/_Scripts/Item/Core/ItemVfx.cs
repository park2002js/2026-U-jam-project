using System;
using System.Collections;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>교체 가능한 VFX 프리팹의 기준 지름을 보정하고 아이템이 정한 수명 동안 재생한다.</summary>
    public sealed class ItemVfx : MonoBehaviour
    {
        [SerializeField, Min(0.0001f), Tooltip("루트 스케일 1일 때 주 지면 링의 지름. 장식 glow/연기는 경계 밖으로 퍼질 수 있다.")]
        private float referenceDiameter = 1f;

        public float ReferenceDiameter => referenceDiameter;

        public void Play(float diameter, float duration)
        {
            Validate(diameter, duration);
            if (!float.IsFinite(referenceDiameter) || referenceDiameter <= 0)
                throw new InvalidOperationException("VFX 기준 지름은 유한한 양수여야 합니다.");

            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particle in particles)
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 크기는 애니메이션되는 시각 자식 대신 바깥 루트에서 보정한다.
            transform.localScale = Vector3.one * (diameter / referenceDiameter);
            foreach (ParticleSystem particle in particles)
            {
                var main = particle.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            foreach (ParticleSystem particle in particles)
            {
                ParticleSystem ancestor = particle.transform.parent != null
                    ? particle.transform.parent.GetComponentInParent<ParticleSystem>() : null;
                if (ancestor == null || !ancestor.transform.IsChildOf(transform)) particle.Play(true);
            }
            Destroy(gameObject, duration);
        }

        public static IEnumerator Spawn(ItemUseContext context, float diameter, float duration, Transform parent = null)
        {
            Validate(diameter, duration);
            if (context == null) throw new ArgumentNullException(nameof(context));
            string path = context.Item.Meta.VfxPath;
            if (string.IsNullOrWhiteSpace(path) || !context.Item.IsEquipped) yield break;

            bool attached = parent != null;
            GameObject prefab = null;
            yield return ItemAssets.LoadAsync(path, loaded => prefab = loaded);
            if (prefab == null || !context.Item.IsEquipped || attached && parent == null) yield break;

            GameObject instance = Instantiate(prefab, context.Position + Vector3.up * .03f,
                Quaternion.LookRotation(context.Item.Meta.Preview.Direction));
            context.Item.Runtime.Track(instance);
            var vfx = instance.GetComponent<ItemVfx>();
            if (vfx == null)
            {
                Debug.LogError($"[ItemVfx] {path} 프리팹 루트에 ItemVfx가 필요합니다.");
                Destroy(instance);
                yield break;
            }
            vfx.Play(diameter, duration);
            // 장판 부모의 판정 스케일이 VFX 크기에 두 번 적용되지 않도록 월드 크기를 유지한다.
            if (parent != null) instance.transform.SetParent(parent, true);
        }

        private static void Validate(float diameter, float duration)
        {
            if (!float.IsFinite(diameter) || diameter <= 0)
                throw new ArgumentOutOfRangeException(nameof(diameter), "VFX 지름은 유한한 양수여야 합니다.");
            if (!float.IsFinite(duration) || duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration), "VFX 수명은 유한한 양수여야 합니다.");
        }
    }
}
