using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UJam.Runtime.Systems;
using UnityEditor;
using UnityEngine;

namespace UJam.EditorTools.Sound
{
    /// <summary>
    /// 프로젝트의 음악/효과음 클립 음량(게이트 RMS, 피크)을 측정하고, 서로 비슷한 크기로 들리도록
    /// 클립별 보정 배율을 Resources/Audio/SoundBalance.asset에 기록합니다.
    /// 오디오 파일 자체는 수정하지 않으며, 재생할 때 SFXManager/BackgroundMusic이 배율을 곱합니다.
    /// </summary>
    internal static class SoundBalanceTool
    {
        private static readonly string[] ClipFolders = { "Assets/_Project/Audio", "Assets/_Project/Resources/Audio" };
        private const string AssetPath = "Assets/_Project/Resources/Audio/SoundBalance.asset";

        // 목표 음량(게이트 RMS, dBFS). 음악은 효과음보다 충분히 낮게 깔리도록 합니다.
        private const float TargetSfxDb = -21f;
        private const float TargetUiDb = -22f;
        private const float TargetMusicDb = -21f; // 곡끼리 맞춘 뒤 BackgroundMusic.volume(기본 0.3)으로 전체를 낮춥니다.
        private const float MaxGain = 1f;          // AudioSource.volume은 1을 넘을 수 없으므로 줄이는 방향으로만 맞춥니다.
        private const float MinGain = 0.15f;

        [MenuItem("Tools/UJam/Sound/Analyze + Balance Clips", priority = 20)]
        public static void BalanceMenu() => Debug.Log(Balance());

        /// <summary>모든 클립을 측정하고 SoundBalance 에셋을 갱신한 뒤 보고서를 반환합니다.</summary>
        public static string Balance()
        {
            List<(string path, AudioClip clip, Category cat, float db, float peak, float length)> rows = new List<(string, AudioClip, Category, float, float, float)>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", ClipFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Category cat = Classify(path);
                Measure(path, out AudioClip clip, out float db, out float peak);
                if (clip == null) continue;
                rows.Add((path, clip, cat, db, peak, clip.length));
            }

            SoundBalance asset = AssetDatabase.LoadAssetAtPath<SoundBalance>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SoundBalance>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            List<SoundBalance.Entry> entries = new List<SoundBalance.Entry>();
            StringBuilder report = new StringBuilder("[SoundBalance] clip | category | loudness(dB) | peak | gain\n");
            foreach (var row in rows.OrderBy(r => r.cat).ThenBy(r => r.path))
            {
                float target = row.cat == Category.Music ? TargetMusicDb : row.cat == Category.Ui ? TargetUiDb : TargetSfxDb;
                float gain = Mathf.Pow(10f, (target - row.db) / 20f);
                if (row.peak > 0.0001f) gain = Mathf.Min(gain, 0.98f / row.peak);
                gain = Mathf.Clamp(gain, MinGain, MaxGain);
                entries.Add(new SoundBalance.Entry { clip = row.clip, gain = (float)Math.Round(gain, 3) });
                report.AppendLine($"{Path.GetFileName(row.path)} | {row.cat} | {row.db:0.0} | {row.peak:0.00} | {gain:0.00} -> {row.db + 20f * Mathf.Log10(gain):0.0} dB");
            }

            SerializedObject so = new SerializedObject(asset);
            SerializedProperty list = so.FindProperty("_entries");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("clip").objectReferenceValue = entries[i].clip;
                e.FindPropertyRelative("gain").floatValue = entries[i].gain;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private enum Category { Music, Ui, Sfx }

        private static Category Classify(string path)
        {
            if (path.Contains("/Audio/Music/")) return Category.Music;
            if (path.Contains("/Audio/Game/")) return Category.Ui;
            return Category.Sfx;
        }

        /// <summary>
        /// 50ms 창 단위 RMS 중 -50dB 이상인 구간만 평균(게이트)하여 체감 음량을 구합니다.
        /// Streaming 등 샘플을 읽을 수 없는 임포트 설정은 측정하는 동안만 Decompress On Load로 바꿨다가 되돌립니다.
        /// </summary>
        private static void Measure(string path, out AudioClip clip, out float db, out float peak)
        {
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings original = importer.defaultSampleSettings;
            bool switched = original.loadType != AudioClipLoadType.DecompressOnLoad;
            if (switched)
            {
                AudioImporterSampleSettings temp = original;
                temp.loadType = AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = temp;
                importer.SaveAndReimport();
            }

            try
            {
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                db = -120f;
                peak = 0f;
                if (clip == null) return;
                if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
                float[] data = new float[clip.samples * clip.channels];
                if (!clip.GetData(data, 0)) return;

                int window = Mathf.Max(1, clip.frequency / 20) * clip.channels;
                double gatedSum = 0;
                int gatedCount = 0;
                for (int start = 0; start < data.Length; start += window)
                {
                    int end = Mathf.Min(data.Length, start + window);
                    double sum = 0;
                    for (int i = start; i < end; i++)
                    {
                        float s = data[i];
                        sum += s * s;
                        float a = Mathf.Abs(s);
                        if (a > peak) peak = a;
                    }
                    double power = sum / Math.Max(1, end - start);
                    if (power > 1e-5) // -50 dBFS 게이트
                    {
                        gatedSum += power;
                        gatedCount++;
                    }
                }
                db = gatedCount > 0 ? (float)(10.0 * Math.Log10(gatedSum / gatedCount)) : -120f;
            }
            finally
            {
                if (switched)
                {
                    importer.defaultSampleSettings = original;
                    importer.SaveAndReimport();
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
        }

        /// <summary>
        /// PlayerTurret 프리팹의 SFXManager 3D 감쇠 시작 거리를 전장 거리(약 27~52)에 맞춥니다.
        /// 프리팹에 저장된 값(12)이 코드 기본값보다 우선하므로 직접 바꿉니다.
        /// </summary>
        public static string ApplySpatialDistance(float distance = 50f)
        {
            const string prefabPath = "Assets/_Project/Prefabs/Player/PlayerTurret.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SFXManager sfx = root.GetComponentInChildren<SFXManager>(true);
                if (sfx == null) return "SFXManager not found";
                SerializedObject so = new SerializedObject(sfx);
                so.FindProperty("spatialMinDistance").floatValue = distance;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return $"spatialMinDistance={distance}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>열린 씬에서 카메라(AudioListener)부터 전장까지의 거리를 측정합니다(3D 효과음 감쇠 확인용).</summary>
        public static string ReportListenerDistance()
        {
            Camera cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null) return "no camera";
            StringBuilder sb = new StringBuilder($"camera pos={cam.transform.position} fwd={cam.transform.forward}\n");
            foreach (Vector2 vp in new[] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.1f), new Vector2(0.2f, 0.5f) })
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(vp.x, vp.y, 0));
                sb.Append(Physics.Raycast(ray, out RaycastHit hit, 2000f) ? $"vp{vp}: {hit.distance:0.0} ({hit.collider.name})\n" : $"vp{vp}: no hit\n");
            }
            return sb.ToString();
        }
    }
}
