using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Combat
{
    /// <summary>
    /// 상태이상 이펙트 확인용 임시 코드.
    /// 씬에 있는 모든 StatusVfxController를 찾아 동시에 적용한다.
    /// 스포너가 런타임에 생성한 적도 함께 잡힌다.
    /// </summary>
    public class StatusVFXTester : MonoBehaviour
    {
        [Header("대상 범위")]
        [SerializeField]
        [Tooltip("비워두면 씬 전체에서 찾는다. 특정 그룹만 테스트하려면 그 부모를 넣는다.")]
        private Transform[] _targetRoots;

        [SerializeField]
        [Tooltip("비활성 상태의 적도 대상에 포함한다.")]
        private bool _includeInactive = true;

        [Header("테스트 키")]
        [SerializeField] private KeyCode _shockKey = KeyCode.Alpha1;    // 감전
        [SerializeField] private KeyCode _freezeKey = KeyCode.Alpha2;   // 빙결
        [SerializeField] private KeyCode _burnKey = KeyCode.Alpha3;     // 화상
        [SerializeField] private KeyCode _bleedKey = KeyCode.Alpha4;    // 출혈
        [SerializeField] private KeyCode _clearKey = KeyCode.Alpha0;    // 전체 해제

        [Header("지속시간")]
        [SerializeField, Min(0f)]
        [Tooltip("0이면 다시 키를 누를 때까지 계속 유지한다.")]
        private float _duration = 0f;

        private readonly List<StatusVfxController> _targets = new();

        private void Update()
        {
            if (Input.GetKeyDown(_shockKey)) ToggleAll(StatusType.Shock);
            if (Input.GetKeyDown(_freezeKey)) ToggleAll(StatusType.Freeze);
            if (Input.GetKeyDown(_burnKey)) ToggleAll(StatusType.Burn);
            if (Input.GetKeyDown(_bleedKey)) ToggleAll(StatusType.Bleed);
            if (Input.GetKeyDown(_clearKey)) ClearAll();
        }

        /// <summary>
        /// 대상을 다시 모은다. 루트를 지정하지 않으면 씬 전체에서 찾으므로
        /// 런타임에 생성된 Clone도 포함된다.
        /// </summary>
        private void RefreshTargets()
        {
            _targets.Clear();

            if (_targetRoots == null || _targetRoots.Length == 0)
            {
                // 씬 전체 탐색
                FindObjectsInactive inactive = _includeInactive
                    ? FindObjectsInactive.Include
                    : FindObjectsInactive.Exclude;

                _targets.AddRange(FindObjectsByType<StatusVfxController>(inactive, FindObjectsSortMode.None));
            }
            else
            {
                // 지정된 루트들의 자식만 탐색
                foreach (Transform root in _targetRoots)
                {
                    if (root == null) continue;

                    foreach (StatusVfxController found in root.GetComponentsInChildren<StatusVfxController>(_includeInactive))
                    {
                        if (found != null && !_targets.Contains(found)) _targets.Add(found);
                    }
                }
            }

            Debug.Log($"[StatusVfxTester] 대상 {_targets.Count}개 확보", this);
        }

        // 대상 전체에 같은 상태를 토글한다. 첫 대상의 상태를 기준으로 켤지 끌지 결정한다.
        private void ToggleAll(StatusType type)
        {
            RefreshTargets();   // 키를 누를 때마다 최신 상태로 갱신
            if (_targets.Count == 0)
            {
                Debug.Log("[StatusVfxTester] 대상이 없음 — StatusVfxController가 붙은 적이 씬에 있는지 확인", this);
                return;
            }

            bool turnOff = false;
            foreach (StatusVfxController target in _targets)
            {
                if (target == null) continue;
                turnOff = target.IsActive(type);   // 기준이 될 첫 유효 대상
                break;
            }

            foreach (StatusVfxController target in _targets)
            {
                if (target == null) continue;
                if (turnOff) target.StopStatus(type);
                else target.StartStatus(type);
            }

            // 켜는 경우에만 자동 해제 예약
            if (!turnOff && _duration > 0f) StartCoroutine(AutoStop(type));
        }

        private void ClearAll()
        {
            RefreshTargets();
            foreach (StatusVfxController target in _targets)
            {
                if (target != null) target.StopAll();
            }
        }

        private IEnumerator AutoStop(StatusType type)
        {
            yield return new WaitForSeconds(_duration);

            foreach (StatusVfxController target in _targets)
            {
                if (target != null) target.StopStatus(type);
            }
        }
    }
}