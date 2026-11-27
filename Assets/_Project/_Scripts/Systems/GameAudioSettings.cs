using System;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    /// <summary>
    /// 설정창의 배경음(BGM)·효과음(SFX) 볼륨(0~1)을 보관하고 PlayerPrefs에 저장합니다.
    /// BackgroundMusic·SFXManager·ShootImpactFx가 이 값을 곱해 재생하며, 값이 바뀌면 Changed로 알립니다.
    /// </summary>
    public static class GameAudioSettings
    {
        private const string BgmKey = "UJam.Audio.Bgm";
        private const string SfxKey = "UJam.Audio.Sfx";
        private const float DefaultVolume = 0.8f;

        private static bool _loaded;
        private static float _bgm = DefaultVolume;
        private static float _sfx = DefaultVolume;

        /// <summary>볼륨이 바뀔 때마다 호출됩니다.</summary>
        public static event Action Changed;

        public static float BgmVolume
        {
            get { Load(); return _bgm; }
            set { Set(ref _bgm, BgmKey, value); }
        }

        public static float SfxVolume
        {
            get { Load(); return _sfx; }
            set { Set(ref _sfx, SfxKey, value); }
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _bgm = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, DefaultVolume));
            _sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, DefaultVolume));
        }

        private static void Set(ref float field, string key, float value)
        {
            Load();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(field, value)) return;
            field = value;
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            Changed = null;
        }
    }
}
