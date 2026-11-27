using UJam.Runtime.Phase;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public class SlimeEnemy : MeleeEnemy
    {
        [SerializeField] private SlimeEnemy _splitPrefab;
        [SerializeField] private bool _canSplit = true;
        private bool _splitHandled;
        public bool CanSplit => _canSplit;

        public override void Dead()
        {
            // Count both children before base.Dead reports the parent; its sinking animation reveals them.
            Split();
            base.Dead();
        }

        private void Split()
        {
            if (_splitHandled) return;
            _splitHandled = true;
            if (!_canSplit) return;
            if (_splitPrefab == null)
            {
                Debug.LogError($"{name}의 분열체 Prefab이 없습니다.", this);
                return;
            }
            GameObject target = FSM.Targets.Count > 0 ? FSM.Targets[0] : null;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 position = transform.position + Vector3.right * (0.8f * side);
                SlimeEnemy child = Instantiate(_splitPrefab, position, _splitPrefab.transform.rotation, transform.parent);
                child._canSplit = false;
                child.transform.localScale = _splitPrefab.transform.localScale * 0.5f;
                // 분열체 체력은 본체 최대 체력의 절반(본체 60 → 분열체 30, 평타 3방)으로 맞춘다.
                child.Status.ConfigureSpawnStats(Mathf.Max(1f, Status.MaxHealth * 0.5f), 40f, 0);
                if (WaveController.Instance != null) WaveController.Instance.RegisterAdditionalEnemy(child, this);
                child.BeginCombat(target);
            }
            GetComponent<EnemyVfx>()?.Signal("SplitStart");
        }
    }
}
