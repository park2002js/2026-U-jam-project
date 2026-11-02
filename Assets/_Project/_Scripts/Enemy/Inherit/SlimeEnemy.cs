using UJam.Runtime.Phase;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public class SlimeEnemy : MeleeEnemy
    {
        [SerializeField] private SlimeEnemy _splitPrefab;
        [SerializeField] private bool _canSplit = true;
        private bool _splitHandled;

        protected override void ReportDeath()
        {
            // The children must be counted before the last original enemy can complete the wave.
            if (_canSplit && !_splitHandled) return;
            base.ReportDeath();
        }

        public override void Dead()
        {
            if (Animation == null || !Animation.HasState("Death")) Split();
            base.Dead();
        }

        public override void AnimationSignal(string signal)
        {
            if (FSM.state == EnemyStateType.Dead && (signal == "DeathSplit" || signal == "DeathComplete"))
            {
                Split();
                base.ReportDeath();
            }
            base.AnimationSignal(signal);
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
                child.Status.ConfigureSpawnStats(75f, 40f, 0);
                if (WaveController.Instance != null) WaveController.Instance.RegisterAdditionalEnemy(child, this);
                child.BeginCombat(target);
            }
        }

        protected override void OnDestroy()
        {
            if (FSM != null && FSM.state == EnemyStateType.Dead) base.ReportDeath();
            base.OnDestroy();
        }
    }
}
