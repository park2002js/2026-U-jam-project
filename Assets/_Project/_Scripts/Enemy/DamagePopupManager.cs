using System.Collections.Generic;
using UnityEngine;
using UJam.Runtime.Enemy;

namespace UJam.Runtime.UI
{
    // 피해 통지를 받아 맞은 자리에 데미지 숫자를 띄운다.
    public class DamagePopupManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _camera;              // 비우면 Camera.main
        [SerializeField] private RectTransform _popupParent;  // 캔버스 아래의 빈 오브젝트
        [SerializeField] private DamagePopup _popupPrefab;

        [Header("표시 설정")]
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.5f, 0f);  // 머리 위 높이
        [SerializeField, Min(0.1f)] private float _lifetime = 0.8f;   // 유지 시간
        [SerializeField] private float _riseSpeed = 1.5f;             // 위로 떠오르는 속도
        [SerializeField] private float _spread = 0.4f;                // 좌우 흔들림 (겹침 방지)
        [SerializeField] private Color _color = Color.white;
        [SerializeField, Min(0)] private int _prewarmCount = 12;

        private readonly List<DamagePopup> _active = new();
        private readonly Queue<DamagePopup> _pool = new();

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;

            for (int i = 0; i < _prewarmCount; i++)
            {
                DamagePopup popup = CreatePopup();
                popup.gameObject.SetActive(false);
                _pool.Enqueue(popup);
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

        // 피해가 들어온 자리에 숫자 하나를 띄움
        private void HandleDamageReported(EnemyStatus status, float current, float max, float damage)
        {
            if (status == null || _popupPrefab == null || _popupParent == null) return;

            DamagePopup popup = GetFromPool();

            // 같은 자리에 겹치지 않도록 좌우로 조금씩 흩뿌림
            Vector3 jitter = new Vector3(Random.Range(-_spread, _spread), 0f, 0f);
            popup.WorldPos = status.transform.position + _worldOffset + jitter;
            popup.Elapsed = 0f;
            popup.SetValue(damage, _color);
            popup.SetAlpha(1f);

            _active.Add(popup);
        }

        private void LateUpdate()
        {
            if (_camera == null) return;
            float dt = Time.deltaTime;

            // 뒤에서부터 순회해야 제거해도 인덱스가 꼬이지 않음
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                DamagePopup popup = _active[i];
                popup.Elapsed += dt;

                if (popup.Elapsed >= _lifetime)
                {
                    ReturnToPool(popup);
                    _active.RemoveAt(i);
                    continue;
                }

                // 월드 기준으로 위로 떠오름 (적이 죽어도 그 자리에 남음)
                popup.WorldPos += Vector3.up * _riseSpeed * dt;

                Vector3 screenPos = _camera.WorldToScreenPoint(popup.WorldPos);
                if (screenPos.z <= 0f)   // 카메라 뒤쪽이면 숨김
                {
                    popup.SetAlpha(0f);
                    continue;
                }

                popup.Rect.position = screenPos;

                // 수명의 절반이 지난 뒤부터 서서히 사라짐
                float t = popup.Elapsed / _lifetime;
                popup.SetAlpha(t < 0.5f ? 1f : Mathf.InverseLerp(1f, 0.5f, t));
            }
        }

        private DamagePopup GetFromPool()
        {
            DamagePopup popup = _pool.Count > 0 ? _pool.Dequeue() : CreatePopup();
            popup.gameObject.SetActive(true);
            return popup;
        }

        private void ReturnToPool(DamagePopup popup)
        {
            if (popup == null) return;
            popup.gameObject.SetActive(false);
            _pool.Enqueue(popup);
        }

        private DamagePopup CreatePopup()
        {
            return Instantiate(_popupPrefab, _popupParent);
        }
    }
}