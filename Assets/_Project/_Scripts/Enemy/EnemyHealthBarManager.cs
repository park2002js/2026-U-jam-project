using System.Collections.Generic;
using UnityEngine;
using UJam.Runtime.Enemy;

namespace UJam.Runtime.UI
{
    // 피해를 받은 적들의 체력바를 모아서 생성·배치·회수한다.
    public class EnemyHealthBarManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _camera;              // 비우면 Camera.main
        [SerializeField] private RectTransform _barParent;    // Screen Space - Overlay 캔버스 아래의 빈 오브젝트
        [SerializeField] private EnemyHealthBar _barPrefab;

        [Header("표시 설정")]
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2f, 0f);  // 머리 위 높이
        [SerializeField, Min(0f)] private float _hideDelay = 3f;   // 피격 후 유지 시간 (0이면 계속 표시)
        [SerializeField, Min(0)] private int _prewarmCount = 8;    // 미리 만들어둘 개수

        // 지금 화면에 떠 있는 체력바들
        private readonly Dictionary<EnemyStatus, EnemyHealthBar> _active = new();
        // 재사용 대기 중인 체력바들
        private readonly Queue<EnemyHealthBar> _pool = new();
        // 순회 중 제거를 모아두는 버퍼
        private readonly List<EnemyStatus> _removeBuffer = new();

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;

            // 초반 끊김을 줄이기 위해 미리 생성해 풀에 넣어둠
            for (int i = 0; i < _prewarmCount; i++)
            {
                EnemyHealthBar bar = CreateBar();
                bar.gameObject.SetActive(false);
                _pool.Enqueue(bar);
            }
        }

        private void OnEnable()
        {
            EnemyStatus.DamageReported += HandleDamageReported;
        }

        private void OnDisable()
        {
            EnemyStatus.DamageReported -= HandleDamageReported;
        }

        // 피해 통지를 받아 체력바를 만들거나 갱신
        private void HandleDamageReported(EnemyStatus status, float current, float max, float damage)
        {
            if (status == null || _barPrefab == null || _barParent == null) return;

            // 이미 떠 있으면 재사용, 없으면 풀에서 꺼냄
            if (!_active.TryGetValue(status, out EnemyHealthBar bar))
            {
                bar = GetFromPool();
                _active.Add(status, bar);
            }

            bar.SetRatio(max > 0f ? current / max : 0f);
            bar.HideTimer = _hideDelay;
            UpdateBarPosition(status, bar);
        }

        // 위치 갱신은 카메라 이동이 끝난 뒤 처리
        private void LateUpdate()
        {
            if (_camera == null) return;

            foreach (KeyValuePair<EnemyStatus, EnemyHealthBar> pair in _active)
            {
                EnemyStatus status = pair.Key;
                EnemyHealthBar bar = pair.Value;

                // 적이 사라졌거나 죽었으면 회수 대상
                if (status == null || status.HP <= 0f)
                {
                    _removeBuffer.Add(status);
                    continue;
                }

                // 일정 시간 지나면 숨김 (HideDelay가 0이면 계속 표시)
                if (_hideDelay > 0f)
                {
                    bar.HideTimer -= Time.deltaTime;
                    if (bar.HideTimer <= 0f)
                    {
                        _removeBuffer.Add(status);
                        continue;
                    }
                }

                UpdateBarPosition(status, bar);
            }

            // 순회가 끝난 뒤 한꺼번에 회수
            for (int i = 0; i < _removeBuffer.Count; i++) Release(_removeBuffer[i]);
            _removeBuffer.Clear();
        }

        // 적의 머리 위 월드 좌표를 화면 좌표로 변환해 배치
        private void UpdateBarPosition(EnemyStatus status, EnemyHealthBar bar)
        {
            Vector3 worldPos = status.transform.position + _worldOffset;
            Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);

            // z가 음수면 카메라 뒤쪽이므로 숨김
            if (screenPos.z <= 0f)
            {
                bar.SetVisible(false);
                return;
            }

            bar.SetVisible(true);
            bar.Rect.position = screenPos;
        }

        private EnemyHealthBar GetFromPool()
        {
            EnemyHealthBar bar = _pool.Count > 0 ? _pool.Dequeue() : CreateBar();
            bar.gameObject.SetActive(true);
            return bar;
        }

        // 체력바를 숨기고 풀로 되돌림
        private void Release(EnemyStatus status)
        {
            if (status == null)
            {
                // 이미 파괴된 적의 항목을 찾아 정리
                foreach (KeyValuePair<EnemyStatus, EnemyHealthBar> pair in _active)
                {
                    if (pair.Key == null)
                    {
                        ReturnToPool(pair.Value);
                        _active.Remove(pair.Key);
                        return;
                    }
                }
                return;
            }

            if (_active.TryGetValue(status, out EnemyHealthBar bar))
            {
                ReturnToPool(bar);
                _active.Remove(status);
            }
        }

        private void ReturnToPool(EnemyHealthBar bar)
        {
            if (bar == null) return;
            bar.gameObject.SetActive(false);
            _pool.Enqueue(bar);
        }

        private EnemyHealthBar CreateBar()
        {
            return Instantiate(_barPrefab, _barParent);
        }
    }
}