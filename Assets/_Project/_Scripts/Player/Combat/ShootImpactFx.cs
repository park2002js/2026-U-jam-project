using UnityEngine;

namespace UJam.Runtime.Player
{
    // PlayerShooter의 착탄 결과를 받아 명중 지점에 파티클을 생성한다.
    public class ShootImpactFx : MonoBehaviour
    {
        [SerializeField] private PlayerShooter _shooter;

        [Header("착탄 파티클")]
        [SerializeField] private GameObject _enemyImpactFx;   // 적 명중 — 불꽃 튐
        [SerializeField] private GameObject _worldImpactFx;   // 벽·바닥 — 먼지
        [SerializeField, Min(0.01f)] private float _fxLifetime = 1f;   // 파티클이 스스로 안 사라질 때를 대비한 안전 삭제

        private void OnEnable()
        {
            if (_shooter != null) _shooter.OnShotLanded += HandleShotLanded;
        }

        private void OnDisable()
        {
            if (_shooter != null) _shooter.OnShotLanded -= HandleShotLanded;
        }

        // 결과에 따라 다른 파티클을 맞은 표면 방향으로 생성
        private void HandleShotLanded(ShotResult result, RaycastHit hit)
        {
            GameObject prefab = result == ShotResult.Enemy ? _enemyImpactFx : _worldImpactFx;
            if (prefab == null) return;

            // 표면 법선 방향으로 튀도록 회전 (파티클 Shape가 +Z 방향으로 뿜는 기준)
            Quaternion rot = hit.normal.sqrMagnitude > 0f ? Quaternion.LookRotation(hit.normal) : Quaternion.identity;
            GameObject fx = Instantiate(prefab, hit.point, rot);
            Destroy(fx, _fxLifetime);
        }
    }
}