using System.Linq;
using System.Text;
using UJam.Runtime.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UJam.EditorTools
{
    /// <summary>개발용(플레이 모드): 설정 슬라이더를 실제로 움직이고, 음원 볼륨·일시정지 상태를 보고합니다.</summary>
    internal static class AudioDiag
    {
        public static string SetSlider(string sliderName, float value)
        {
            Slider slider = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(s => s.name == sliderName);
            if (slider == null) return "slider not found: " + sliderName;
            slider.value = value; // onValueChanged → GameAudioSettings
            return Report();
        }

        public static string Report()
        {
            StringBuilder sb = new StringBuilder($"scene={SceneManager.GetActiveScene().name} timeScale={Time.timeScale} bgm={GameAudioSettings.BgmVolume:0.00} sfx={GameAudioSettings.SfxVolume:0.00}");
            foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                sb.Append($" | {source.gameObject.name}:{(source.clip != null ? source.clip.name : "-")} vol={source.volume:0.000}");
            return sb.ToString();
        }
    }
}
