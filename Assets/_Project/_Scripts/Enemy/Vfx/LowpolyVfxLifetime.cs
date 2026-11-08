using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public sealed class LowpolyVfxLifetime : MonoBehaviour
    {
        public float Lifetime = 1.5f;
        private float started;
        private void OnEnable() => started = Time.time;
        private void Update() { if (Time.time - started >= Mathf.Max(.05f, Lifetime)) Destroy(gameObject); }
    }
}
