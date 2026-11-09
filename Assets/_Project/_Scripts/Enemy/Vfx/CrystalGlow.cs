using UnityEngine;

namespace UJam.Runtime.Enemy.Vfx
{
    [DefaultExecutionOrder(100)]
    public sealed class CrystalGlow : MonoBehaviour
    {
        public Renderer Source;
        public MeshRenderer Surface;
        public MeshRenderer Halo;
        public Transform Bone;
        public Matrix4x4 BindPose = Matrix4x4.identity;
        public float Visibility { get; private set; }

        private static readonly int Dissolve = Shader.PropertyToID("_Dissolve");
        private static readonly int Fade = Shader.PropertyToID("_Visibility");
        private MaterialPropertyBlock sourceBlock;
        private MaterialPropertyBlock glowBlock;

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (sourceBlock == null) sourceBlock = new MaterialPropertyBlock();
            if (glowBlock == null) glowBlock = new MaterialPropertyBlock();
            if (Source == null)
            {
                Visibility = 0;
                if (Surface != null) Surface.enabled = false;
                if (Halo != null) Halo.enabled = false;
                return;
            }

            // A rigid skin's vertices follow bone * bindpose, not the renderer's unposed bounds.
            Matrix4x4 world = Bone != null ? Bone.localToWorldMatrix * BindPose : Source.transform.localToWorldMatrix;
            Matrix4x4 local = transform.parent.worldToLocalMatrix * world;
            transform.localPosition = local.GetColumn(3);
            transform.localRotation = local.rotation;
            transform.localScale = local.lossyScale;

            Source.GetPropertyBlock(sourceBlock);
            Visibility = Source.enabled && Source.gameObject.activeInHierarchy
                ? 1f - Mathf.Clamp01(sourceBlock.GetFloat(Dissolve)) : 0;
            glowBlock.SetFloat(Fade, Visibility);
            if (Surface != null)
            {
                Surface.SetPropertyBlock(glowBlock);
                Surface.enabled = Visibility > 0;
            }
            if (Halo != null)
            {
                Halo.SetPropertyBlock(glowBlock);
                Halo.enabled = Visibility > 0;
            }
        }

        private void OnDisable()
        {
            if (Surface != null) Surface.enabled = false;
            if (Halo != null) Halo.enabled = false;
        }
    }
}
