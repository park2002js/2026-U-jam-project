using System;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Combat
{
    // 상태이상 종류
    public enum StatusType
    {
        Shock,    // 감전
        Freeze,   // 빙결
        Burn,     // 화상
        Bleed     // 출혈
    }

    /// <summary>
    /// 대상에게 상태이상 지속 이펙트를 붙이고 떼는 컴포넌트. 적 프리팹에 부착한다.
    /// 프리팹 생성과 함께, 필요하면 대상 모델의 색을 덮어쓴다.
    /// </summary>
    public class StatusVfxController : MonoBehaviour
    {
        [Serializable]
        private class StatusVfxEntry
        {
            public StatusType type;
            public GameObject prefab;                            // 지속되는 동안 반복 재생될 이펙트
            public Vector3 offset = new Vector3(0f, 1f, 0f);     // 부착 위치 보정
            public Vector3 rotation;                             // 대상 기준 회전 보정 (오일러 각)
            public bool fitToTargetSize = true;                  // 대상 콜라이더 크기에 맞춰 스케일
            public float sizeMultiplier = 1f;                    // 크기 미세 조정 배율

            [Header("모델 색 변경")]
            public bool tintModel;                               // 켜면 대상 모델의 색을 덮어쓴다
            public Color tintColor = Color.white;                // 덮어쓸 색
            [Range(0f, 1f)] public float tintStrength = 0.7f;    // 원래 색과 섞는 비율
        }

        [Header("상태별 이펙트 프리팹")]
        [SerializeField] private StatusVfxEntry[] _entries;

        [Header("부착 기준")]
        [SerializeField] private Transform _attachPoint;   // 비우면 자기 자신

        // 현재 켜져 있는 이펙트 인스턴스
        private readonly Dictionary<StatusType, GameObject> _active = new();
        // 색을 덮어쓰고 있는 상태들 (여러 상태가 겹칠 수 있으므로 기록해 둔다)
        private readonly List<StatusType> _tinting = new();

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private int _colorId = -1;

        private void Awake() => CacheRenderers();

        // 대상의 Renderer와 색 프로퍼티 이름을 한 번만 찾아둔다
        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();

            // URP는 _BaseColor, 빌트인은 _Color를 사용한다
            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null || renderer.sharedMaterial == null) continue;

                if (renderer.sharedMaterial.HasProperty("_BaseColor")) { _colorId = Shader.PropertyToID("_BaseColor"); break; }
                if (renderer.sharedMaterial.HasProperty("_Color")) { _colorId = Shader.PropertyToID("_Color"); break; }
            }
        }

        /// <summary>상태이상 시작 — 이펙트를 생성해 붙이고, 설정돼 있으면 모델 색도 바꾼다.</summary>
        public void StartStatus(StatusType type)
        {
            // 이미 켜져 있으면 중복 생성하지 않음
            if (_active.ContainsKey(type) || _tinting.Contains(type)) return;

            StatusVfxEntry entry = FindEntry(type);
            if (entry == null)
            {
                Debug.Log($"[StatusVfx] {type} 설정이 없음", this);
                return;
            }

            // 1. 프리팹 생성 (지정돼 있을 때만)
            if (entry.prefab != null)
            {
                Transform parent = _attachPoint != null ? _attachPoint : transform;

                GameObject fx = Instantiate(entry.prefab, parent.position + entry.offset, Quaternion.identity);
                fx.transform.SetParent(parent);
                fx.transform.localRotation = Quaternion.Euler(entry.rotation);
                ApplyScale(fx, entry);

                _active.Add(type, fx);
            }

            // 2. 모델 색 변경 (켜져 있을 때만)
            if (entry.tintModel)
            {
                _tinting.Add(type);
                RefreshTint();
            }

            Debug.Log($"[StatusVfx] {type} 이펙트 시작 → {name}", this);
        }

        /// <summary>상태이상 종료 — 이펙트를 제거하고 색을 되돌린다.</summary>
        public void StopStatus(StatusType type)
        {
            bool changed = false;

            if (_active.TryGetValue(type, out GameObject fx))
            {
                if (fx != null) Destroy(fx);
                _active.Remove(type);
                changed = true;
            }

            if (_tinting.Remove(type))
            {
                RefreshTint();
                changed = true;
            }

            if (changed) Debug.Log($"[StatusVfx] {type} 이펙트 종료 → {name}", this);
        }

        /// <summary>지금 해당 상태가 켜져 있는지 확인한다.</summary>
        public bool IsActive(StatusType type) => _active.ContainsKey(type) || _tinting.Contains(type);

        /// <summary>모든 상태 이펙트를 제거하고 색을 원래대로 돌린다. (사망 등)</summary>
        public void StopAll()
        {
            foreach (KeyValuePair<StatusType, GameObject> pair in _active)
            {
                if (pair.Value != null) Destroy(pair.Value);
            }
            _active.Clear();

            _tinting.Clear();
            RefreshTint();
        }

        private void OnDestroy() => StopAll();

        /// <summary>
        /// 현재 켜져 있는 상태들 중 마지막 것의 색을 적용한다.
        /// 남은 색 상태가 없으면 PropertyBlock을 비워 원래 머티리얼 색으로 되돌린다.
        /// </summary>
        private void RefreshTint()
        {
            if (_renderers == null) CacheRenderers();
            if (_colorId < 0) return;   // 색 프로퍼티를 찾지 못한 경우

            // 되돌리기: 색을 요구하는 상태가 하나도 없음
            if (_tinting.Count == 0)
            {
                foreach (Renderer renderer in _renderers)
                {
                    if (renderer != null) renderer.SetPropertyBlock(null);
                }
                return;
            }

            // 여러 상태가 겹치면 가장 최근에 걸린 것을 우선한다
            StatusVfxEntry entry = FindEntry(_tinting[_tinting.Count - 1]);
            if (entry == null) return;

            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null || renderer.sharedMaterial == null) continue;

                // 원래 색과 섞어서 과하게 덮이지 않게 한다
                Color original = renderer.sharedMaterial.HasProperty(_colorId)
                    ? renderer.sharedMaterial.GetColor(_colorId)
                    : Color.white;
                Color result = Color.Lerp(original, entry.tintColor, entry.tintStrength);

                renderer.GetPropertyBlock(_block);
                _block.SetColor(_colorId, result);
                renderer.SetPropertyBlock(_block);
            }
        }

        private StatusVfxEntry FindEntry(StatusType type)
        {
            if (_entries == null) return null;
            foreach (StatusVfxEntry entry in _entries)
            {
                if (entry != null && entry.type == type) return entry;
            }
            return null;
        }

        /// <summary>
        /// 이펙트 크기를 정한다.
        /// 대상이 자식으로 붙으면 부모 스케일의 영향을 받으므로 이를 상쇄해
        /// 월드 기준 크기가 의도한 값이 되게 한다.
        /// </summary>
        private void ApplyScale(GameObject fx, StatusVfxEntry entry)
        {
            Vector3 parentScale = fx.transform.parent != null ? fx.transform.parent.lossyScale : Vector3.one;

            float baseSize = 1f;
            if (entry.fitToTargetSize)
            {
                Collider col = GetComponentInChildren<Collider>();
                if (col != null)
                {
                    float size = Mathf.Max(col.bounds.size.x, col.bounds.size.y, col.bounds.size.z);
                    if (size > 0f) baseSize = size;
                }
            }

            float target = baseSize * Mathf.Max(0.0001f, entry.sizeMultiplier);

            fx.transform.localScale = new Vector3(
                parentScale.x != 0f ? target / parentScale.x : target,
                parentScale.y != 0f ? target / parentScale.y : target,
                parentScale.z != 0f ? target / parentScale.z : target);
        }
    }
}