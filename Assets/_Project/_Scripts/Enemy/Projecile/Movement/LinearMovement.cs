using System.Collections;
using UnityEngine;

namespace UJam.Runtime.Enemy.Projectiles
{
    [CreateAssetMenu(menuName = "UJam/Projectile Movement/Linear")]
    public sealed class LinearMovement : ProjectileMovement
    {
        public override IEnumerator Move(Transform projectile, Vector3 destination, float speed)
        {
            // 매프레임마다, 도착지점에 도달할 때까지 직선으로 이동함
            while (projectile.position != destination)
            {
                projectile.position = Vector3.MoveTowards(projectile.position, destination, speed * Time.deltaTime);

                yield return null;
            }
        }
    }
}
