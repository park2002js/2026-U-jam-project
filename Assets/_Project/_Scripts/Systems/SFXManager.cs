using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    /// <summary>UI/아이템/적이 함께 사용하는 효과음 재생기. 모든 요청을 중복 허용하여 즉시 재생한다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource)), DefaultExecutionOrder(-1000)]
    public sealed class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }
        private AudioSource source;
        private readonly List<AudioSource> spatialSources = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                Destroy(this);
                return;
            }

            Instance = this;
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        /// <summary>위치에 관계없는 2D 효과음. 같은 클립의 연속 요청도 기존 소리 위에 겹쳐 재생한다.</summary>
        public void Play(AudioClip clip, [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (!HasClip(clip, caller, file, line)) return;

            source.PlayOneShot(clip);
        }

        /// <summary>요청한 월드 위치에서 독립 재생한다. 호출자나 PlayerTurret이 움직여도 발음 위치를 유지한다.</summary>
        public void PlayAt(AudioClip clip, Vector3 position, [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (!HasClip(clip, caller, file, line)) return;

            var instance = new GameObject($"SFX: {clip.name}");
            instance.transform.position = position;
            var spatial = instance.AddComponent<AudioSource>();
            spatial.playOnAwake = false;
            spatial.clip = clip;
            spatial.spatialBlend = 1f;
            spatial.dopplerLevel = 0f;
            spatial.volume = source.volume;
            spatial.outputAudioMixerGroup = source.outputAudioMixerGroup;
            spatialSources.Add(spatial);
            spatial.Play();
            StartCoroutine(ReleaseWhenFinished(spatial));
        }

        private bool HasClip(AudioClip clip, string caller, string file, int line)
        {
            if (clip != null) return true;

            Debug.LogWarning($"[SFXManager] {Path.GetFileNameWithoutExtension(file)}.{caller}:{line}에서 null AudioClip을 전달했습니다. 재생을 건너뜁니다.", this);
            return false;
        }

        private IEnumerator ReleaseWhenFinished(AudioSource spatial)
        {
            // 백그라운드 로딩과 오디오 일시 정지 중에는 재생 종료로 판단하지 않는다.
            yield return null;
            while (spatial != null && (spatial.isPlaying || AudioListener.pause ||
                   spatial.clip.loadState == AudioDataLoadState.Loading))
                yield return null;

            spatialSources.Remove(spatial);
            if (spatial != null) Destroy(spatial.gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (source != null) source.Stop();
            foreach (var spatial in spatialSources)
                if (spatial != null) Destroy(spatial.gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = null;
    }
}
