using UnityEngine;

namespace UJam.Runtime.Player
{
    // PlayerShooter의 착탄 결과를 받아 명중 지점에 파티클과 사운드를 재생한다.
    public class ShootImpactFx : MonoBehaviour
    {
        [SerializeField] private PlayerShooter _shooter;

        [Header("착탄 파티클")]
        [SerializeField] private GameObject _enemyImpactFx;   // 적 명중 — 불꽃 튐
        [SerializeField] private GameObject _worldImpactFx;   // 벽·바닥 — 먼지
        [SerializeField, Min(0.01f)] private float _fxLifetime = 1f;   // 파티클이 스스로 안 사라질 때를 대비한 안전 삭제

        [Header("피격 사운드")]
        [SerializeField] private AudioClip[] _enemyHitClips;   // 적 피격음 (여러 개 넣으면 랜덤 재생)
        [SerializeField] private AudioClip[] _worldHitClips;   // 벽·바닥 피격음
        [SerializeField, Range(0f, 1f)] private float _volume = 0.7f;
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.95f, 1.05f);   // 피치 랜덤 범위
        [SerializeField, Range(0f, 1f)] private float _spatialBlend = 1f;           // 1이면 3D, 0이면 2D

        private void OnEnable()
        {
            if (_shooter != null) _shooter.OnShotLanded += HandleShotLanded;
        }

        private void OnDisable()
        {
            if (_shooter != null) _shooter.OnShotLanded -= HandleShotLanded;
        }

        // 결과에 따라 다른 파티클과 사운드를 재생
        private void HandleShotLanded(ShotResult result, RaycastHit hit)
        {
            bool isEnemy = result == ShotResult.Enemy;

            // 표면 법선 방향으로 튀도록 회전 (파티클 Shape가 +Z 방향으로 뿜는 기준)
            GameObject prefab = isEnemy ? _enemyImpactFx : _worldImpactFx;
            if (prefab != null)
            {
                Quaternion rot = hit.normal.sqrMagnitude > 0f ? Quaternion.LookRotation(hit.normal) : Quaternion.identity;
                GameObject fx = Instantiate(prefab, hit.point, rot);
                Destroy(fx, _fxLifetime);
            }

            PlayHitSound(isEnemy ? _enemyHitClips : _worldHitClips, hit.point);
        }

        // 클립 배열에서 하나를 골라 착탄 지점에서 재생. 적이 사라져도 소리가 끊기지 않도록 독립 오브젝트로 만든다.
        private void PlayHitSound(AudioClip[] clips, Vector3 pos)
        {
            if (clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;

            GameObject obj = new GameObject("HitSound");
            obj.transform.position = pos;

            AudioSource source = obj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = _volume;
            // 피치를 조금씩 바꿔 반복 재생할 때 단조롭지 않게 함
            source.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
            source.spatialBlend = _spatialBlend;
            source.Play();

            // 피치가 바뀌면 재생 길이도 달라지므로 보정해서 삭제
            Destroy(obj, clip.length / Mathf.Max(0.01f, source.pitch));
        }
    }
}