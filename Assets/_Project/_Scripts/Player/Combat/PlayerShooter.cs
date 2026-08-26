using System.Collections;
using System;
using UnityEngine;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Item;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// Player가 마우스 클릭을 하였을 경우, 마우스의 위치로 Ray를 쏘아서 처음 맞는 대상에게 피해를 입힘과 동시에 시각적인 Bullet을 날려보내는 클래스이다.
    /// PlayerCombatSystem에 의해 초기화 된다.
    /// PlayerStatus에 기반하여 피해를 입힌다.
    /// 
    /// 적 명중 효과는 시각적 총알이 저장된 명중 위치에 도착할 때 알린다.
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        public event Action<ItemUseContext> OnShootingHit; // Ray 명중 순간이 아닌 총알 도착 순간에 통지한다.

        // Ray를 쏠 카메라 오브젝트
        private Camera _aimCamera;

        // 투사체를 생성할 월드 좌표
        private Transform _bulletSpawnPoint;

        // 투사체 발사시 생성할 오브젝트 (prefab)
        private GameObject _bulletPrefab;

        // 투사체가 날아가서 피해를 입힐 때, 효과 적용을 위해 참고할 Player의 Status 정보
        private PlayerStatus _playerStatus;

        [Header("투사체 발사 시스템 조정")]
        [SerializeField, Min(0.01f)] private float _maxDistance = 100f; // Ray 최대 길이 설정
        [SerializeField] private LayerMask _hitLayers = ~0; // Ray와 물리 상호작용을 할 물리 Layer를 설정
        [SerializeField, Min(0.01f)] private float _bulletVisualSpeed = 80f; // 투사체 발사 속도
        [SerializeField, Min(0.01f)] private float _bulletVisualLifetime = 3f; // 투사체 시각화 시간

        /// <summary>
        /// PlayerCombatSystem으로부터 호출되어 초기화된다.
        /// PlayerCombatSystem에 저장된 속성을 인자를 통해 전달받은 뒤, 아래의 함수에서 사용한다.
        /// </summary>
        public void Init(PlayerCombatManager combatSystem)
        {
            if (combatSystem == null)
            {
                return;
            }

            _aimCamera = combatSystem.AimCamera;
            _bulletSpawnPoint = combatSystem.BulletSpawnPoint;
            _bulletPrefab = combatSystem.BulletPrefab;
            _playerStatus = combatSystem.PlayerStatus;
        }

        /// <summary>
        /// 즉발형 공격으로, 가장 먼저 Ray에 맞은 적 하나에 한해서 TakeDamage를 호출하여 데미지를 입힌다.
        /// 또한, Ray가 맞은 위치로 Projectile을 날려보내서 시각적인 피드백을 만든다.
        /// </summary>
        public void TryShoot()
        {
            // 투사체 발사를 위한 최소 조건을 충족하고 있는지 확인
            if (_aimCamera == null || _playerStatus == null || (_bulletPrefab != null && _bulletSpawnPoint == null)
                || !IsPositiveFinite(_playerStatus.AttackDamage) || !IsPositiveFinite(_maxDistance)
                || !IsPositiveFinite(_bulletVisualSpeed) || !IsPositiveFinite(_bulletVisualLifetime))
            {
                Debug.Log("Ray 발사 실패");
                return;
            }

            // 1. 데미지 산정

            float damage = _playerStatus.AttackDamage;


            // 2. Ray를 통해 투사체를 날려보낼 방향 계산
            Ray ray = _aimCamera.ScreenPointToRay(Input.mousePosition); // 카메라 중앙에서 마우스의 커서를 투영시킨 곳으로 Ray를 발사
            Vector3 endPoint = ray.origin + ray.direction * _maxDistance;
            ItemUseContext hitContext = default;

            // RayCast에 처음 맞은 대상의 IDamageable 클래스를 통해 TakeDamage를 호출하여 데미지를 입힘
            if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _hitLayers, QueryTriggerInteraction.Ignore))
            {
                endPoint = hit.point;
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    DamageInfo damageInfo = new DamageInfo(damage, name, DamageSourceKind.Player);
                    EnemyBase enemy = target as EnemyBase;
                    GameObject hitEnemy = enemy != null ? enemy.gameObject : null;
                    if (hitEnemy != null) hitContext = new ItemUseContext(gameObject, hitEnemy, damageInfo, hit.point);
                    target.TakeDamage(damageInfo);
                }
            }

            // 명중 지점과 발사 당시 피해량을 보존하므로 원래 적이 죽거나 움직여도 도착 효과는 같은 지점에서 실행된다.
            if (_bulletPrefab != null || hitContext.IsShootingHit)
            {
                Vector3 start = _bulletSpawnPoint != null ? _bulletSpawnPoint.position : ray.origin;
                SpawnBulletVisual(start, endPoint, hitContext);
            }

            return;
        }

        /// <summary>
        /// Ballet이 발사될 위치에서, Ray가 도착한 지점을 향하는 벡터를 따라 일직선으로 이동하는 Projectile을 생성한 뒤,
        /// 이를 발사하는 코루틴을 호출하는 함수이다.
        /// </summary>
        private void SpawnBulletVisual(Vector3 start, Vector3 end, ItemUseContext hitContext)
        {
            Vector3 direction = end - start;
            Quaternion rotation = direction.sqrMagnitude > 0f ? Quaternion.LookRotation(direction.normalized) : Quaternion.identity;
            GameObject bullet = _bulletPrefab != null ? Instantiate(_bulletPrefab, start, rotation) : null;
            float travelTime = direction.magnitude / _bulletVisualSpeed;
            StartCoroutine(MoveBulletVisual(bullet, start, end, travelTime, hitContext));
        }

        /// <summary>
        /// 거리/속도로 계산한 시간 동안 총알을 이동시키고 도착한 프레임에 명중 효과를 실행한다.
        /// </summary>
        private IEnumerator MoveBulletVisual(GameObject bullet, Vector3 start, Vector3 end, float travelTime, ItemUseContext hitContext)
        {
            float elapsed = 0f;
            // 적에게 맞은 총알은 수명 설정이 짧아도 명중 지점까지 도착한다. 빗나간 총알만 기존 수명으로 제한한다.
            float lifetime = hitContext.IsShootingHit ? travelTime : Mathf.Min(travelTime, _bulletVisualLifetime);
            while (elapsed < lifetime)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (bullet != null) bullet.transform.position = Vector3.Lerp(start, end, elapsed / travelTime);
            }

            if (bullet != null) Destroy(bullet);
            if (hitContext.IsShootingHit) OnShootingHit?.Invoke(hitContext);
        }

        /// <summary>
        /// 음수나 무효한 값인지 체크하는 내부 helper 함수
        /// </summary>
        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
