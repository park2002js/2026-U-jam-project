using System.Collections.Generic;
using UnityEngine;
using UJam.Runtime.Enemy;
using UJam.Runtime.Systems;

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

        [SerializeField, Tooltip("속성이 걸려 있는 동안에는 체력바를 계속 표시한다.")]
        private bool _keepWhileElementActive = true;

        // 지금 화면에 떠 있는 체력바들
        private readonly Dictionary<EnemyStatus, EnemyHealthBar> _active = new();
        // 속성 조회를 위해 EnemyStatus와 EnemyBase를 함께 보관한다
        private readonly Dictionary<EnemyStatus, EnemyBase> _owners = new();
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
                _owners[status] = status.GetComponent<EnemyBase>();   // 속성 조회용
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

                // 속성 아이콘 갱신
                _owners.TryGetValue(status, out EnemyBase enemy);
                bool hasElement = false;
                if (bar.Icons != null)
                {
                    bar.Icons.Refresh(enemy);
                    hasElement = bar.Icons.HasVisible;
                }

                // 속성이 걸려 있으면 타이머를 멈춰 체력바를 유지한다
                if (_hideDelay > 0f && !(_keepWhileElementActive && hasElement))
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
                        _owners.Remove(pair.Key);
                        return;
                    }
                }
                return;
            }

            if (_active.TryGetValue(status, out EnemyHealthBar bar))
            {
                ReturnToPool(bar);
                _active.Remove(status);
                _owners.Remove(status);
            }
        }

        private void ReturnToPool(EnemyHealthBar bar)
        {
            if (bar == null) return;

            if (bar.Icons != null) bar.Icons.Clear();   // 아이콘도 함께 정리
            bar.gameObject.SetActive(false);
            _pool.Enqueue(bar);
        }

        private EnemyHealthBar CreateBar()
        {
            return Instantiate(_barPrefab, _barParent);
        }
    }
}