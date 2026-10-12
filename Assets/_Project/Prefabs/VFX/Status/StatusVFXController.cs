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
    /// 메인 로직과 무관하게 StartStatus / StopStatus 호출만으로 동작한다.
    /// </summary>
    public class StatusVfxController : MonoBehaviour
    {
        [Serializable]
        private class StatusVfxEntry
        {
            public StatusType type;
            public GameObject prefab;                            // 지속되는 동안 반복 재생될 이펙트
            public Vector3 offset = new Vector3(0f, 1f, 0f);     // 부착 위치 보정
            public bool fitToTargetSize = true;                  // 대상 콜라이더 크기에 맞춰 스케일
            public float sizeMultiplier = 1f;                    // 크기 미세 조정 배율
        }

        [Header("상태별 이펙트 프리팹")]
        [SerializeField] private StatusVfxEntry[] _entries;

        [Header("부착 기준")]
        [SerializeField] private Transform _attachPoint;   // 비우면 자기 자신

        // 현재 켜져 있는 이펙트 인스턴스
        private readonly Dictionary<StatusType, GameObject> _active = new();

        /// <summary>상태이상 시작 — 이펙트를 생성해 대상에 붙인다.</summary>
        public void StartStatus(StatusType type)
        {
            // 이미 켜져 있으면 중복 생성하지 않음
            if (_active.ContainsKey(type)) return;

            StatusVfxEntry entry = FindEntry(type);
            if (entry == null || entry.prefab == null)
            {
                Debug.Log($"[StatusVfx] {type} 프리팹이 지정되지 않음", this);
                return;
            }

            Transform parent = _attachPoint != null ? _attachPoint : transform;
            GameObject fx = Instantiate(entry.prefab, parent.position + entry.offset, Quaternion.identity);
            fx.transform.SetParent(parent);   // 대상을 따라다니게 함

            ApplyScale(fx, entry);

            _active.Add(type, fx);
            Debug.Log($"[StatusVfx] {type} 이펙트 시작 → {name}", this);
        }

        /// <summary>상태이상 종료 — 이펙트를 제거한다.</summary>
        public void StopStatus(StatusType type)
        {
            if (!_active.TryGetValue(type, out GameObject fx)) return;

            if (fx != null) Destroy(fx);
            _active.Remove(type);
            Debug.Log($"[StatusVfx] {type} 이펙트 종료 → {name}", this);
        }

        /// <summary>지금 해당 상태가 켜져 있는지 확인한다.</summary>
        public bool IsActive(StatusType type) => _active.ContainsKey(type);

        /// <summary>모든 상태 이펙트를 제거한다. (사망 등)</summary>
        public void StopAll()
        {
            foreach (KeyValuePair<StatusType, GameObject> pair in _active)
            {
                if (pair.Value != null) Destroy(pair.Value);
            }
            _active.Clear();
        }

        private void OnDestroy() => StopAll();

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
            // 부모의 월드 스케일 (적 모델이 0.04처럼 작게 임포트된 경우를 보정)
            Vector3 parentScale = fx.transform.parent != null ? fx.transform.parent.lossyScale : Vector3.one;

            // 기준 크기: 콜라이더에 맞출지, 원본 크기를 유지할지
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