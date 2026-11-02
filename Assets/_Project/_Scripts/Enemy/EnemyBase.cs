using System.Collections;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy.Movement;
using UJam.Runtime.Grid;
using UJam.Runtime.Phase;
using UJam.Runtime.Shop;
using UnityEngine;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;
using Ujam.Runtime.Item;

namespace UJam.Runtime.Enemy
{
    [RequireComponent(typeof(EnemyStatus))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyStatus _status;
        private PlayerStatus lastAttacker;
        private EnemyFSM _fsm;
        private EnemyMovement _movement;
        private GameObject _defaultTarget;
        private bool _initialized;
        private bool _combatStarted;
        private bool _dead;
        private bool _deathReported;
        private bool _attackImpactPending;

        public EnemyStatus Status => _status;
        public EnemyFSM FSM => _fsm;
        public EnemyMovement Movement => _movement;
        public EnemyAnimation Animation { get; private set; }
        public GameObject AttackTarget { get; private set; }
        public Vector3 AttackTargetPosition { get; private set; }
        public bool AttackTargetWasBaseCore { get; private set; }
        public bool IsAttackPlaying { get; private set; }
        public virtual bool ImmediateAttack => false;

        protected virtual void Awake()
        {
            _fsm = new EnemyFSM(this);
            _fsm.SetState(EnemyStateType.Init);
        }

        protected virtual void Start()
        {
            if (!_combatStarted && _fsm.state == EnemyStateType.Idle) BeginCombat();
        }

        protected virtual void Update()
        {
            if (_combatStarted && _fsm.state == EnemyStateType.Idle && !_status.IsStunned &&
                _fsm.TGV.TryGetTarget(out _))
                _fsm.SetState(EnemyStateType.Move);
            if ((_fsm.state == EnemyStateType.Move || _fsm.state == EnemyStateType.Attack) &&
                !_status.IsStunned && !IsAttackPlaying)
                TrySummon();
        }

        protected virtual void OnDestroy()
        {
            if (_status != null && _fsm != null) _status.Died -= _fsm.SetDead;
            if (_dead) ReportDeath();
        }

        public virtual void Init()
        {
            if (_initialized) return;
            _initialized = true;
            if (_status == null) _status = GetComponent<EnemyStatus>();
            _status.init();
            _movement = _status.Movement;
            if (_movement == null)
            {
                _movement = GetComponent<EnemyMovement>();
                if (_movement == null) _movement = gameObject.AddComponent<GridColMovement>();
            }
            _movement.init(this);
            _status.Died += _fsm.SetDead;
            Animation = GetComponentInChildren<EnemyAnimation>(true);
            if (Animation != null) Animation.Initialize(this);
        }

        // Idle resets targets only; entering it never heals or reinitializes the enemy.
        public virtual void Idle()
        {
            _fsm.Targets.Clear();
            GameObject target = _defaultTarget != null ? _defaultTarget : GameObject.FindGameObjectWithTag("BaseCore");
            if (target != null) _fsm.Targets.Add(target);
        }

        public virtual void BeginCombat(GameObject target = null)
        {
            if (_combatStarted || _dead) return;
            _combatStarted = true;
            _defaultTarget = target;
            Idle();
            _fsm.SetState(Animation != null && Animation.HasState("Spawn")
                ? EnemyStateType.Spawn
                : EnemyStateType.Move);
        }

        public virtual void PrepareForTesting(GameObject target)
        {
            if (_dead) return;
            _combatStarted = true;
            _defaultTarget = target;
            Idle();
        }

        public virtual void Move()
        {
            Animation?.Play("Move");
            _movement.Enter();
        }

        public virtual void Spawn()
        {
            if (Animation == null || !Animation.Play("Spawn"))
                _fsm.SetState(EnemyStateType.Move);
        }

        public virtual bool TrySummon() => false;

        public virtual void Summon()
        {
            if (Animation == null || !Animation.Play("Summon"))
                _fsm.SetState(EnemyStateType.Move);
        }

        public virtual void BeginAttack()
        {
            if (_dead || IsAttackPlaying || _fsm.state != EnemyStateType.Attack || _status.IsStunned ||
                !_fsm.TGV.TryGetTarget(out GameObject target) || !_fsm.TGV.Check(target))
                return;

            AttackTarget = target;
            AttackTargetPosition = target.transform.position;
            AttackTargetWasBaseCore = target.CompareTag("BaseCore");
            CaptureAttackDestination();
            IsAttackPlaying = true;
            _attackImpactPending = true;
            Vector3 direction = target.CompareTag("BaseCore")
                ? Vector3.forward
                : target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0f) transform.rotation = Quaternion.LookRotation(direction);

            float duration = Animation != null ? Animation.Duration("Attack") : 0f;
            if (Animation == null || !Animation.Play("Attack", duration * _status.AttackSpeed))
            {
                AnimationSignal("AttackImpact");
                AnimationSignal("AttackComplete");
            }
        }

        public void CancelAttack()
        {
            _attackImpactPending = false;
            IsAttackPlaying = false;
            AttackTarget = null;
            AttackTargetPosition = Vector3.zero;
            AttackTargetWasBaseCore = false;
        }

        protected virtual void CaptureAttackDestination() { }

        public bool TryGetAttackTarget(out GameObject target, out IDamageable damageable)
        {
            target = AttackTarget;
            damageable = null;
            if (!IsAttackPlaying || target == null || !_fsm.TGV.Check(target)) return false;
            IDamageable receiver = target.GetComponent<IDamageable>();
            if (receiver == null || receiver is Behaviour behaviour && !behaviour.isActiveAndEnabled) return false;
            if (!AttackTargetWasBaseCore)
            {
                GridSystem grid = GridSystem.Instance;
                Vector3 offset = target.transform.position - AttackTargetPosition;
                var moved = new Vector2(offset.x / grid.CellWidth, offset.z / grid.CellHeight);
                if (moved.sqrMagnitude > .01f) return false;
            }
            damageable = receiver;
            return true;
        }

        // Only the animation impact signal executes the effect of an animated attack.
        public virtual void Attack()
        {
            if (_dead || !TryGetAttackTarget(out GameObject target, out IDamageable damageable))
                return;
            damageable.TakeDamage(new DamageInfo(_status.OutgoingDamage, name, DamageSourceKind.Enemy));
        }

        public virtual void AnimationSignal(string signal)
        {
            switch (signal)
            {
                case "AttackImpact":
                    if (_fsm.state != EnemyStateType.Attack || !IsAttackPlaying || !_attackImpactPending) return;
                    _attackImpactPending = false;
                    if (!_dead) Attack();
                    break;
                case "AttackComplete":
                    if (_fsm.state != EnemyStateType.Attack || !IsAttackPlaying) return;
                    CancelAttack();
                    Animation?.Play("Idle");
                    _fsm.CompleteAttack();
                    break;
                case "SpawnComplete":
                    if (_fsm.state == EnemyStateType.Spawn) _fsm.SetState(EnemyStateType.Move);
                    break;
                case "SummonComplete":
                    if (_fsm.state == EnemyStateType.Summon) _fsm.SetState(EnemyStateType.Move);
                    break;
                case "DeathComplete":
                    if (_fsm.state == EnemyStateType.Dead)
                    {
                        ReportDeath();
                        Destroy(gameObject);
                    }
                    break;
            }
        }

        public virtual void Dead()
        {
            if (_dead) return;
            _dead = true;
            CancelAttack();
            _movement?.Exit();
            StopAllCoroutines();
            foreach (Collider targetCollider in GetComponentsInChildren<Collider>())
                targetCollider.enabled = false;

            if (Wallet.Instance != null)
            {
                var player = lastAttacker != null ? lastAttacker : PlayerStatus.Instance;
                Wallet.Instance.AddCurrency(player != null ? player.KillReward(_status.Credits) : _status.Credits);
            }

            ReportDeath();
            StartCoroutine(DeadAnim());
        }

        protected virtual void ReportDeath()
        {
            if (_deathReported) return;
            _deathReported = true;
            if (WaveController.Instance != null) WaveController.Instance.ReportEnemyDead(gameObject);
        }

        public virtual IEnumerator DeadAnim()
        {
            if (Animation != null && Animation.Play("Death"))
            {
                // Completion events normally remove the corpse; this also handles a missing end event.
                yield return new WaitForSeconds(Animation.Duration("Death"));
                AnimationSignal("DeathComplete");
                yield break;
            }

            Vector3 start = transform.position;
            Vector3 end = start + Vector3.down * 1.5f;
            const float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            AnimationSignal("DeathComplete");
        }

        public float TakeDamage(DamageInfo info)
        {
            if (_status == null || _status.HP <= 0 || _dead) return 0;
            Vector3 deathPosition = transform.position;
            lastAttacker = info.SourceKind == DamageSourceKind.Player
                ? (info.Attacker != null ? info.Attacker : PlayerStatus.Instance)
                : null;
            float applied = _status.ApplyDamage(info.Damage, info.IgnoreDamageTaken);
            if (applied > 0 && _status.HP <= 0 && lastAttacker != null)
                EventManager.Instance.Publish(new ItemEvent(ItemTrigger.EnemyKilled, lastAttacker, deathPosition, new[] { this }));
            return applied;
        }

        public void Knockback(Vector3 offset)
        {
            if (_status == null || _status.HP <= 0 || _dead) return;
            transform.position += offset;
            if (_fsm.state == EnemyStateType.Move) _movement.Enter();
            else ReTargeting();
        }

        public void ReTargeting() => _fsm.ReTargeting();
    }
}
