using UJam.Runtime.Phase;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public class WitchEnemy : RangedEnemy
    {
        [SerializeField] private SkeletonEnemy _skeletonPrefab;
        [SerializeField, Min(0.1f)] private float _summonInterval = 10f;
        [SerializeField, Min(0f)] private float _summonOffset = 1.5f;
        private float _nextSummon;
        private bool _started;
        private bool _summoned;

        public override void BeginCombat(GameObject target = null)
        {
            StartSummonClock();
            base.BeginCombat(target);
        }

        public override void PrepareForTesting(GameObject target)
        {
            StartSummonClock();
            base.PrepareForTesting(target);
        }

        private void StartSummonClock()
        {
            if (_started) return;
            _started = true;
            _nextSummon = Time.time + _summonInterval;
        }

        public override bool TrySummon()
        {
            if (!_started || Time.time < _nextSummon || Status.IsStunned ||
                _skeletonPrefab == null || Animation == null || !Animation.HasState("Summon")) return false;
            if (FSM.state != EnemyStateType.Move && FSM.state != EnemyStateType.Attack) return false;
            if (!FSM.SetState(EnemyStateType.Summon)) return false;
            _nextSummon = Time.time + _summonInterval;
            return true;
        }

        public override void Summon()
        {
            _summoned = false;
            base.Summon();
        }

        public override void Dead()
        {
            base.Dead();
            GetComponent<WitchDeathVfx>()?.Begin();
        }

        public override void AnimationSignal(string signal)
        {
            if (signal == "SummonImpact" && FSM.state == EnemyStateType.Summon &&
                !_summoned && Status.HP > 0 && !Status.IsStunned)
            {
                _summoned = true;
                GameObject target = FSM.Targets.Count > 0 ? FSM.Targets[0] : null;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 position = transform.position + Vector3.right * (_summonOffset * side);
                    SkeletonEnemy skeleton = Instantiate(_skeletonPrefab, position, _skeletonPrefab.transform.rotation, transform.parent);
                    if (WaveController.Instance != null) WaveController.Instance.RegisterAdditionalEnemy(skeleton, this);
                    skeleton.BeginCombat(target);
                }
            }
            base.AnimationSignal(signal);
        }
    }
}
