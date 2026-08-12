using System.Collections;
using UnityEngine;

namespace UJam.Runtime.Enemy.Projectiles
{
    [CreateAssetMenu(menuName = "UJam/Projectile Movement/Quadratic")]
    public sealed class QuadraticMovement : ProjectileMovement
    {
        [SerializeField, Min(0f)] private float _height = 2f;

        public override IEnumerator Move(Transform projectile, Vector3 destination, float speed)
        {
            Vector3 start = projectile.position;
            float distance = Vector3.Distance(start, destination);

            if (distance <= 0f) yield break;


            float duration = distance / speed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                Vector3 position = Vector3.Lerp(start, destination, progress);
                position.y += 4f * _height * progress * (1f - progress);
                projectile.position = position;

                yield return null;
            }

            projectile.position = destination;
        }
    }
}
