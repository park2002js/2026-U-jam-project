using System.Collections;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    // 적이 피해를 받을 때마다 잠깐 색을 번쩍인다. 총·스킬·도트 등 피해 경로와 무관하게 작동.
    public class EnemyHitFlash : MonoBehaviour
    {
        [SerializeField] private EnemyStatus _status;          // 비우면 자동 탐색
        [SerializeField] private Color _flashColor = Color.red;
        [SerializeField, Min(0.01f)] private float _flashTime = 0.1f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private int _colorId;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            if (_status == null) _status = GetComponentInParent<EnemyStatus>();
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();

            // 셰이더마다 색 속성 이름이 다름 (URP: _BaseColor, Built-in: _Color)
            Material mat = _renderers.Length > 0 ? _renderers[0].sharedMaterial : null;
            _colorId = mat != null && mat.HasProperty("_BaseColor")
                ? Shader.PropertyToID("_BaseColor")
                : Shader.PropertyToID("_Color");
        }

        private void OnEnable()
        {
            if (_status != null) _status.OnEnemyHpChanged += HandleHpChanged;
        }

        private void OnDisable()
        {
            if (_status != null) _status.OnEnemyHpChanged -= HandleHpChanged;
            ClearFlash();
        }

        // 체력이 변할 때마다 플래시 (연속으로 맞으면 처음부터 다시)
        private void HandleHpChanged(float current, float max)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            // 머티리얼을 복제하지 않고 렌더러의 색만 덮어씀
            _block.Clear();
            _block.SetColor(_colorId, _flashColor);
            foreach (Renderer r in _renderers)
                if (r != null) r.SetPropertyBlock(_block);

            yield return new WaitForSeconds(_flashTime);

            ClearFlash();
            _flashRoutine = null;
        }

        // 덮어쓴 값을 지워 원래 머티리얼 색으로 복귀
        private void ClearFlash()
        {
            if (_renderers == null) return;
            foreach (Renderer r in _renderers)
                if (r != null) r.SetPropertyBlock(null);
        }
    }
}