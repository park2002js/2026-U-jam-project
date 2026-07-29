using UJam.Runtime.Combat;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        // 목표 도달 전 투사체 최대 생존 시간
        [SerializeField, Min(0.01f)] private float _lifetime = 5f;

        // 투사체가 피해를 전달할 Target
        private GameObject _target;

        // Target에 전달할 Enemy 피해 정보
        private DamageInfo _damageInfo;

        // 마지막 row의 같은 col에서 계산된 고정 공격 지점
        private Vector3 _targetPoint;

        // 투사체 이동 속도
        private float _speed;

        // 투사체 제거 시각
        private float _destroyTime;

        // 중복 피해를 차단할 명중 상태
        private bool _hit;

        // 생성한 원거리 Enemy가 Target과 피해 정보 전달
        public void Initialize(
            GameObject target,
            Vector3 targetPoint,
            DamageInfo damageInfo,
            float speed)
        {
            // 투사체가 추적할 Target 저장
            _target = target;
            // Grid 거리 판정이 계산한 실제 공격 지점 저장
            _targetPoint = targetPoint;
            // 도달 시 전달할 피해 정보 저장
            _damageInfo = damageInfo;
            // 음수가 아닌 유한 이동 속도 저장
            _speed = speed > 0f && !float.IsNaN(speed) && !float.IsInfinity(speed) ? speed : 0f;
            // 현재 시각을 기준으로 제거 시각 계산
            _destroyTime = Time.time + Mathf.Max(0.01f, _lifetime);
        }

        // Target을 추적하고 도달 시 한 번 피해 적용
        private void Update()
        {
            // 이미 명중했거나 Target과 속도가 유효하지 않은지 확인
            if (_hit || _target == null || _speed <= 0f || Time.time >= _destroyTime)
            {
                // 더 이동할 수 없는 투사체 제거
                Destroy(gameObject);

                // 이번 Frame 처리 종료
                return;
            }

            // 현재 Frame의 고정 Grid 공격 지점 이동 적용
            transform.position = Vector3.MoveTowards(
                transform.position,
                _targetPoint,
                _speed * Time.deltaTime);

            // 아직 Grid 공격 지점에 도달하지 않았는지 확인
            if ((transform.position - _targetPoint).sqrMagnitude > 0.0001f)
            {
                // 다음 Frame까지 이동 유지
                return;
            }

            _hit = true;

            // Target의 피해 계약 확인
            IDamageable damageable = _target.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = _target.GetComponentInChildren<IDamageable>();
            }

            // 찾은 피해 대상에 Enemy 피해 전달
            if (damageable != null)
            {
                damageable.TakeDamage(_damageInfo);
            }

            // 피해 적용을 마친 투사체 제거
            Destroy(gameObject);
        }
    }
}
