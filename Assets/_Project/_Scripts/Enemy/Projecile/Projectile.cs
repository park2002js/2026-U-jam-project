using System.Collections;
using UJam.Runtime.Combat;
using UnityEngine;

namespace UJam.Runtime.Enemy.Projectiles
{
    public sealed class Projectile : MonoBehaviour
    {
        private EnemyBase _owner;
        private IDamageable _pastTarget;
        private UnityEngine.Object _pastTargetObject;
        private Vector3 _destination;
        private float _speed;
        private ProjectileMovement _movement;

        public void Launch(EnemyBase owner, IDamageable pastTarget, Vector3 destination, float speed, ProjectileMovement movement)
        {
            _owner = owner;
            _pastTarget = pastTarget;
            _pastTargetObject = pastTarget as UnityEngine.Object;
            _destination = destination;
            _speed = speed;
            _movement = movement;

            StartCoroutine(Fly());
        }

        private IEnumerator Fly()
        {
            if (_movement == null || _speed <= 0f)
            {
                Debug.LogError($"{name}의 이동 설정이 유효하지 않습니다.", this);
                Destroy(gameObject);
                yield break;
            }

            yield return _movement.Move(transform, _destination, _speed);

            ApplyDamage();
            Destroy(gameObject);
        }

        private void ApplyDamage()
        {
            // 발사자가 사라졌거나 사망했다면, 혹은 발사 당시 대상이 존재하지 않는다면, 현재 공격력을 읽거나 피해를 호출하지 않는다.
            if (_owner == null || _owner.FSM == null || _owner.FSM.state == EnemyStateType.Dead)
            {
                return;
            }

            // 인터페이스 참조는 대상이 파괴되어도 CLR null로 바뀌지 않으므로
            // UnityEngine.Object 참조를 통해 실제 게임 오브젝트의 생존 여부를 확인한다.
            if (_pastTargetObject == null || _pastTarget == null) return;

            DamageInfo damageInfo = new DamageInfo(_owner.Status.AttackDamage, _owner.name, DamageSourceKind.Enemy);

            _pastTarget.TakeDamage(damageInfo);
        }
    }
}
