using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyAnimation : MonoBehaviour
    {
        private const float BlendSeconds = .12f;
        private Animator animator;
        private EnemyBase owner;
        private string playing;
        private float speed = 1f;
        private bool completionSent;
        private readonly HashSet<string> sentSignals = new HashSet<string>();

        public string CurrentAnimation => playing;
        public bool IsBlending => animator != null && animator.IsInTransition(0);
        public float NormalizedTime => PlayingState().normalizedTime;

        public void Initialize(EnemyBase enemy)
        {
            owner = enemy;
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;
        }

        public bool HasState(string state)
        {
            if (animator == null) animator = GetComponent<Animator>();
            return animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash(state));
        }

        public float Duration(string state)
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator.runtimeAnimatorController == null) return 0f;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == state || clip.name.EndsWith("_" + state, System.StringComparison.Ordinal))
                    return clip.length;
            return 0f;
        }

        public bool Play(string state, float playbackSpeed = 1f)
        {
            if (!HasState(state)) return false;
            playing = state;
            speed = Mathf.Max(0f, playbackSpeed);
            completionSent = false;
            sentSignals.Clear();
            animator.SetFloat(Animator.StringToHash(state + "Speed"), speed);
            animator.speed = IsPaused ? 0f : 1f;
            int hash = Animator.StringToHash(state);
            bool immediate = state == "Spawn" || state == "Death" || owner != null && owner.ImmediateAttack ||
                animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash;
            if (immediate)
            {
                animator.Play(hash, 0, 0f);
                // Spawn must apply its buried pose before a newly created enemy can be rendered.
                if (state == "Spawn") animator.Update(0f);
            }
            else
            {
                float duration = Duration(state);
                float blend = duration > 0f && speed > 0f ? Mathf.Min(BlendSeconds, duration / speed * .12f) : BlendSeconds;
                animator.CrossFadeInFixedTime(hash, blend, 0, 0f);
            }
            return true;
        }

        private bool IsPaused => owner != null && owner.Status != null &&
                                 owner.FSM.state != EnemyStateType.Dead && owner.Status.IsStunned;

        private void Update()
        {
            if (animator != null && playing != null)
                animator.speed = IsPaused ? 0f : 1f;
        }

        private void LateUpdate()
        {
            if (animator == null || playing == null || completionSent || IsPaused) return;
            AnimatorStateInfo state = PlayingState();
            if (state.shortNameHash != Animator.StringToHash(playing) || state.normalizedTime < 1f) return;
            // Exact-end events can be skipped by an interrupted evaluation; completion still belongs to this playback.
            if (playing == "Attack" || playing == "Spawn" || playing == "Summon" || playing == "Death")
                Signal(playing + "Complete");
        }

        public void OnAnimationSignal(AnimationEvent animationEvent)
        {
            if (owner == null || animator == null || playing == null) return;
            if (animationEvent.animatorStateInfo.shortNameHash != Animator.StringToHash(playing)) return;
            Signal(animationEvent.stringParameter);
        }

        private void Signal(string signal)
        {
            if (owner == null) return;
            if (signal.EndsWith("Complete", System.StringComparison.Ordinal))
            {
                if (completionSent || signal != playing + "Complete" || PlayingState().normalizedTime < .9999f) return;
                completionSent = true;
            }
            if (signal != "MoveStep" && !sentSignals.Add(signal)) return;
            owner.AnimationSignal(signal);
        }

        private AnimatorStateInfo PlayingState()
        {
            if (animator == null || playing == null) return default;
            int hash = Animator.StringToHash(playing);
            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                if (next.shortNameHash == hash) return next;
            }
            return animator.GetCurrentAnimatorStateInfo(0);
        }
    }
}
