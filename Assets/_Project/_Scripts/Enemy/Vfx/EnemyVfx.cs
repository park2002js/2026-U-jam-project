using System;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public sealed class EnemyVfx : MonoBehaviour
    {
        [Serializable]
        public sealed class Binding
        {
            public string Signal;
            public GameObject Effect;
            public Transform Anchor;
            public bool AtTarget;
            public bool UseAnchorPosition;
            public bool Follow;
            public Vector3 TargetOffset = Vector3.up * .8f;
        }

        public Binding[] Bindings = Array.Empty<Binding>();
        public Renderer[] Eyes = Array.Empty<Renderer>();
        public TrailRenderer[] Trails = Array.Empty<TrailRenderer>();
        private readonly List<GameObject> attached = new List<GameObject>();
        private EnemyBase owner;
        private MaterialPropertyBlock block;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        public int PlayedEffects { get; private set; }

        private void Awake() { owner = GetComponent<EnemyBase>(); block = new MaterialPropertyBlock(); }
        private void OnEnable()
        {
            PlayedEffects = 0;
            foreach (Renderer eye in Eyes) if (eye != null) eye.SetPropertyBlock(null);
            SetTrails(false, true);
        }

        public void Signal(string signal, bool targetValid = true, Vector3? targetPosition = null)
        {
            if (signal == "SplitStart" && owner is SlimeEnemy slime && !slime.CanSplit) return;
            if (signal == "AttackCancel" || signal == "AttackComplete" || signal == "SummonComplete" || signal == "DeathStart") CancelAttached();
            if (signal == "AttackTrailStart") SetTrails(true, false);
            if (signal == "AttackTrailEnd") SetTrails(false, false);
            if (signal == "EyesOff")
                foreach (Renderer eye in Eyes)
                {
                    if (eye == null) continue;
                    eye.GetPropertyBlock(block);
                    block.SetColor(BaseColor, Color.black);
                    block.SetColor(ColorId, Color.black);
                    block.SetColor("_EmissionColor", Color.black);
                    eye.SetPropertyBlock(block);
                }
            foreach (Binding binding in Bindings)
            {
                if (binding.Signal != signal || binding.Effect == null) continue;
                if (binding.AtTarget && !targetValid) continue;
                Transform anchor = binding.Anchor != null ? binding.Anchor : transform;
                Vector3 position = binding.AtTarget && !binding.UseAnchorPosition && owner != null
                    ? (targetPosition ?? owner.AttackTargetPosition) + binding.TargetOffset : anchor.position;
                // Summoned skeletons use world X offsets, regardless of the Witch's facing.
                if ((signal == "SummonImpact" || signal == "SplitStart") && binding.Anchor != null)
                    position = transform.position + binding.Anchor.localPosition;
                GameObject effect = Instantiate(binding.Effect, position, binding.AtTarget ? Quaternion.identity : anchor.rotation);
                if (binding.Follow && !binding.AtTarget)
                {
                    effect.transform.SetParent(anchor, true);
                    attached.Add(effect);
                }
                PlayedEffects++;
            }
        }

        private void LateUpdate()
        {
            if (owner != null && ((!owner.IsAttackPlaying && owner.FSM.state != EnemyStateType.Summon) || owner.Status.IsStunned))
            {
                SetTrails(false, false);
                if (attached.Count > 0) CancelAttached();
            }
            attached.RemoveAll(item => item == null);
        }

        private void SetTrails(bool enabled, bool clear)
        {
            foreach (TrailRenderer trail in Trails)
            {
                if (trail == null) continue;
                trail.emitting = enabled;
                if (clear) trail.Clear();
            }
        }

        private void CancelAttached()
        {
            SetTrails(false, false);
            foreach (GameObject effect in attached) if (effect != null) Destroy(effect);
            attached.Clear();
        }

        private void OnDisable() { CancelAttached(); SetTrails(false, true); }
    }
}
