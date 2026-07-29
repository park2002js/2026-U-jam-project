using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UJam.Runtime.Combat;

namespace UJam.Runtime.Player
{
    public sealed class PlayerShooter : MonoBehaviour
    {
        // 마우스 위치의 Ray를 만들 Player 카메라
        [SerializeField] private Camera _aimCamera;

        // 시각적 Bullet이 시작할 총구 위치
        [SerializeField] private Transform _bulletSpawnPoint;

        // 사용자가 할당할 시각적 Bullet Prefab
        [SerializeField] private GameObject _bulletPrefab;

        // 발사 공격력을 제공할 Player 상태
        [SerializeField] private PlayerStatus _playerStatus;

        // Raycast 최대 사거리
        [SerializeField, Min(0.01f)] private float _maxDistance = 100f;

        // Raycast가 검사할 Layer
        [SerializeField] private LayerMask _hitLayers = ~0;

        // 시각적 Bullet 이동 속도
        [SerializeField, Min(0.01f)] private float _bulletVisualSpeed = 80f;

        // 시각적 Bullet 최대 생존 시간
        [SerializeField, Min(0.01f)] private float _bulletVisualLifetime = 3f;

        // 카메라와 총구와 Player 상태 기본 참조 보완
        private void Awake()
        {
            // Inspector 카메라가 없으면 Main Camera 사용
            if (_aimCamera == null)
            {
                _aimCamera = Camera.main;
            }

            // 총구가 없으면 Player 위치 사용
            if (_bulletSpawnPoint == null)
            {
                _bulletSpawnPoint = transform;
            }

            // Inspector 상태가 없으면 부모 또는 Scene의 Player 상태 사용
            if (_playerStatus == null)
            {
                _playerStatus = GetComponentInParent<PlayerStatus>();

                // 부모에도 없으면 활성 Scene 전체에서 Player 상태 확인
                if (_playerStatus == null)
                {
                    _playerStatus = FindFirstObjectByType<PlayerStatus>();
                }
            }
        }

        // 우클릭으로 기존 입력과 Phase를 우회해 강제 발사
        private void Update()
        {
            // 우클릭 순간과 공격력 제공 상태 확인
            if (Mouse.current != null
                && Mouse.current.rightButton.wasPressedThisFrame
                && _playerStatus != null)
            {
                // 현재 Player 공격력으로 발사
                TryShoot(_playerStatus.AttackDamage);
            }
        }

        // 마우스 위치의 Ray로 즉시 피해를 주고 시각적 Bullet 생성
        public bool TryShoot(float damage)
        {
            // 필수 카메라와 마우스와 유효한 공격 수치 확인
            if (_aimCamera == null
                || Mouse.current == null
                || !IsPositiveFinite(damage)
                || !IsPositiveFinite(_maxDistance)
                || !IsPositiveFinite(_bulletVisualSpeed)
                || !IsPositiveFinite(_bulletVisualLifetime))
            {
                // 발사 실패 반환
                Debug.Log("Ray 발사 실패");
                return false;
            }

            // 현재 마우스 커서의 화면 위치
            Vector2 cursorPosition = Mouse.current.position.ReadValue();
            // 카메라에서 마우스 커서 방향으로 생성한 Ray
            Ray ray = _aimCamera.ScreenPointToRay(cursorPosition);
            // 아무것도 맞지 않았을 때 사용할 최대 사거리 끝점
            Vector3 endPoint = ray.origin + ray.direction * _maxDistance;

            // 가장 가까운 Collider 명중 정보 확인
            if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                _maxDistance,
                _hitLayers,
                QueryTriggerInteraction.Ignore))
            {
                // 시각적 Bullet의 목적지를 실제 명중점으로 변경
                endPoint = hit.point;

                // 명중 Collider의 부모에서 피해 대상 확인
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    // Player 공격 정보 생성
                    DamageInfo damageInfo = new DamageInfo(
                        damage,
                        name,
                        DamageSourceKind.Player);

                    // Raycast 명중 대상에 즉시 피해 전달
                    target.TakeDamage(damageInfo);
                }
            }

            // 할당된 Prefab이 있을 때만 연출용 Bullet 생성
            if (_bulletPrefab != null)
            {
                // 총구에서 명중점까지 이동하는 연출 시작
                SpawnBulletVisual(_bulletSpawnPoint.position, endPoint);
            }

            // Ray 발사 성공 반환
            return true;
        }

        // 시각적 Bullet Prefab 생성과 이동 시작
        private void SpawnBulletVisual(Vector3 start, Vector3 end)
        {
            // 시작점에서 목적지로 향할 방향
            Vector3 direction = end - start;
            // 방향이 없을 때 사용할 기본 회전
            Quaternion rotation = direction.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direction.normalized)
                : Quaternion.identity;
            // 사용자가 할당한 Bullet Prefab 인스턴스
            GameObject bullet = Instantiate(_bulletPrefab, start, rotation);

            // 생성 성공한 Bullet만 이동 처리
            if (bullet != null)
            {
                StartCoroutine(MoveBulletVisual(bullet.transform, end));
            }
        }

        // 시각적 Bullet을 목적지까지 이동한 뒤 제거
        private IEnumerator MoveBulletVisual(Transform bullet, Vector3 end)
        {
            // Bullet이 이동한 누적 시간
            float elapsed = 0f;

            // 목적지 도착 또는 생존 시간 만료까지 이동
            while (bullet != null && elapsed < _bulletVisualLifetime)
            {
                // 현재 Frame 이동 뒤 위치 계산
                bullet.position = Vector3.MoveTowards(
                    bullet.position,
                    end,
                    _bulletVisualSpeed * Time.deltaTime);

                // 목적지에 도착하면 이동 종료
                if ((bullet.position - end).sqrMagnitude <= 0.0001f)
                {
                    // 목적지 도착으로 반복 종료
                    break;
                }

                // 누적 이동 시간 갱신
                elapsed += Time.deltaTime;
                // 다음 Frame까지 대기
                yield return null;
            }

            // 아직 남아 있는 연출용 Bullet 제거
            if (bullet != null)
            {
                Destroy(bullet.gameObject);
            }
        }

        // 양의 유한 float 여부 확인
        private static bool IsPositiveFinite(float value)
        {
            // NaN과 무한대와 0 이하를 제외한 결과 반환
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
