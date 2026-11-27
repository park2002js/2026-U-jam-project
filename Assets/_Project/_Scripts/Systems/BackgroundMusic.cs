using UJam.Runtime.Phase;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    [DisallowMultipleComponent]
    public sealed class BackgroundMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip idleTrack;
        [SerializeField] private AudioClip combatTrack;
        [SerializeField, Range(0f, 1f)] private float volume = .35f;

        private AudioSource source;
        private GameManager game;

        private void Start()
        {
            game = combatTrack != null ? GameManager.Instance : null;
            if (combatTrack != null && game == null) return;

            // 음악은 효과음 OneShot과 별도로 전환한다.
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = volume * GameAudioSettings.BgmVolume;
            source.priority = 32;
            GameAudioSettings.Changed += ApplyVolume;

            if (game != null)
            {
                game.OnPhaseChanged += ChangeTrack;
                game.OnGameOver += StopMusic;
                if (!game.IsGameOver) ChangeTrack(game.CurrentPhase);
            }
            else PlayTrack(idleTrack);
        }

        private void ChangeTrack(PhaseState phase)
        {
            if (phase == PhaseState.Combat) PlayTrack(combatTrack);
            if (phase == PhaseState.Preparation) PlayTrack(idleTrack);
        }

        private void PlayTrack(AudioClip track)
        {
            if (track == null || source.clip == track) return;
            source.clip = track;
            ApplyVolume();
            source.Play();
        }

        // 곡마다 원본 음량이 달라 SoundBalance 배율로 맞춘 뒤, 음악 기본 볼륨과 설정창의 배경음 볼륨을 곱한다.
        private void ApplyVolume()
        {
            if (source == null) return;
            source.volume = volume * SoundBalance.Gain(source.clip) * GameAudioSettings.BgmVolume;
        }

        private void StopMusic() => source.Stop();

        private void OnDestroy()
        {
            GameAudioSettings.Changed -= ApplyVolume;
            if (game != null)
            {
                game.OnPhaseChanged -= ChangeTrack;
                game.OnGameOver -= StopMusic;
            }
            if (source != null) Destroy(source);
        }
    }
}
