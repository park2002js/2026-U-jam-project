using System;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    /// <summary>
    /// 클립별 음량 보정 배율 표입니다. Tools/UJam/Sound/Analyze + Balance Clips가 측정 결과로 자동 작성합니다.
    /// 오디오 파일을 바꾸지 않고, 재생할 때 SFXManager·BackgroundMusic이 Gain(clip)을 곱해 서로 비슷한 크기로 들리게 합니다.
    /// 특정 소리를 더 키우거나 줄이고 싶으면 이 에셋에서 해당 클립의 gain만 조정하면 됩니다.
    /// </summary>
    [CreateAssetMenu(menuName = "UJam/Sound Balance", fileName = "SoundBalance")]
    public sealed class SoundBalance : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float gain;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private const string ResourcePath = "Audio/SoundBalance";
        private static Dictionary<AudioClip, float> _table;

        /// <summary>클립의 보정 배율을 반환합니다. 표에 없는 클립은 1입니다.</summary>
        public static float Gain(AudioClip clip)
        {
            if (clip == null) return 1f;
            if (_table == null) Load();
            return _table.TryGetValue(clip, out float gain) ? gain : 1f;
        }

        private static void Load()
        {
            // 에디터에서는 에셋을 불러오는 순간 OnValidate가 _table을 비우므로, 지역 변수에 만든 뒤 마지막에 대입한다.
            Dictionary<AudioClip, float> table = new Dictionary<AudioClip, float>();
            SoundBalance asset = Resources.Load<SoundBalance>(ResourcePath);
            if (asset != null && asset._entries != null)
            {
                foreach (Entry entry in asset._entries)
                {
                    if (entry.clip != null) table[entry.clip] = Mathf.Clamp01(entry.gain);
                }
            }
            _table = table;
        }

        private void OnValidate() => _table = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTable() => _table = null;
    }
}
