using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UJam.Runtime.Phase
{
    public class WaveInfoFileReader
    {
        // Prefab 이름과 실제 GameObject Prefab을 연결하기 위한 Dictionary
        // 예: "Goblin" -> Goblin Prefab
        private readonly Dictionary<string, GameObject> _enemyPrefabs;

        // WaveInfoFileReader를 생성할 때 Prefab 정보를 전달받음
        public WaveInfoFileReader(Dictionary<string, GameObject> enemyPrefabs)
        {
            _enemyPrefabs = enemyPrefabs;
        }

        // 특정 폴더에 있는 모든 JSON 파일을 읽고 WaveInfo 배열로 변환
        public WaveInfo[] LoadWaveData(string directoryPath)
        {
            // 지정된 폴더가 존재하지 않는 경우
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogError(
                    $"Wave 폴더를 찾을 수 없습니다: {directoryPath}");

                return Array.Empty<WaveInfo>();
            }

            // 폴더 안에 있는 모든 .json 파일을 가져옴
            string[] jsonFiles =
                Directory.GetFiles(directoryPath, "*.json");

            // Wave1.json, Wave2.json, Wave3.json 순서로 정렬
            Array.Sort(jsonFiles, CompareWaveFileNames);

            // 변환된 WaveInfo들을 임시로 저장할 List
            List<WaveInfo> waves = new List<WaveInfo>();

            foreach (string filePath in jsonFiles)
            {
                // JSON 파일을 읽어 WaveJson 객체로 역직렬화
                WaveJson waveJson = ReadJsonFile(filePath);

                if (waveJson == null)
                {
                    waves.Add(null); // 잘못된 파일을 건너뛰어 다음 번호 Wave가 대신 실행되지 않도록 한다.
                    continue;
                }

                // WaveJson을 실제 게임에서 사용할 WaveInfo로 변환
                WaveInfo waveInfo = ConvertToWaveInfo(waveJson);

                waves.Add(waveInfo);
                if (waveInfo != null) Debug.Log($"[WaveInfoFileReader] {Path.GetFileName(filePath)}: Enemy {waveInfo.TotalEnemyCount}명");
                else Debug.LogError($"[WaveInfoFileReader] 잘못된 Wave 설정: {filePath}");
            }

            // List를 배열로 변환해서 반환
            return waves.ToArray();
        }

        // JSON 파일 하나를 읽어 WaveJson 객체로 변환
        private WaveJson ReadJsonFile(string filePath)
        {
            // 해당 경로에 파일이 존재하지 않는 경우
            if (!File.Exists(filePath))
            {
                Debug.LogError(
                    $"Wave JSON 파일을 찾을 수 없습니다: {filePath}");

                return null;
            }

            try
            {
                // JSON 파일 전체 내용을 문자열로 읽음
                string json = File.ReadAllText(filePath);

                // JSON 문자열을 WaveJson 객체로 역직렬화
                WaveJson waveJson =
                    JsonUtility.FromJson<WaveJson>(json);

                return waveJson;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Wave JSON 파일을 읽는 중 오류 발생: {filePath}\n{exception}");

                return null;
            }
        }

        // JSON에서 읽은 데이터를 하나의 WaveInfo로 변환
        private WaveInfo ConvertToWaveInfo(WaveJson waveJson)
        {
            // 해당 Wave에 포함될 EnemySpawnInfo들을 저장
            List<WaveInfo.EnemySpawnInfo> enemySpawnInfos =
                new List<WaveInfo.EnemySpawnInfo>();

            // 생성 목록이 없는 Wave는 실행하지 않는다.
            if (waveJson.enemies == null || waveJson.enemies.Length == 0)
            {
                return null;
            }

            // JSON에 정의된 적 종류를 하나씩 확인
            foreach (EnemyJson enemyJson in waveJson.enemies)
            {
                if (enemyJson == null)
                {
                    return null;
                }

                // JSON에 적힌 Prefab 이름으로 실제 Prefab을 찾음
                if (string.IsNullOrWhiteSpace(enemyJson.prefabName) || !_enemyPrefabs.TryGetValue(enemyJson.prefabName, out GameObject enemyPrefab) || enemyPrefab == null)
                {
                    Debug.LogError(
                        $"Prefab을 찾을 수 없습니다: {enemyJson.prefabName}");

                    return null;
                }

                if (enemyJson.statusSettings == null || enemyJson.statusSettings.Length == 0)
                {
                    return null;
                }

                // 해당 Prefab의 각 Status 설정을 확인
                foreach (StatusSettingJson statusSetting
                         in enemyJson.statusSettings)
                {
                    if (statusSetting == null ||
                        statusSetting.spawns == null || statusSetting.spawns.Length == 0)
                    {
                        return null;
                    }

                    /*
                     * statusSetting.statusDelta에는
                     * JSON의 Status 변경 값이 들어있지만,
                     * 요구사항에 따라 이번 구현에서는
                     * 실제 EnemyStatus에 적용하지 않음.
                     */

                    // 해당 Status 설정으로 생성할 Enemy들을 확인
                    foreach (SpawnJson spawn in statusSetting.spawns)
                    {
                        if (spawn == null || spawn.x < 0 || spawn.z < 0 || spawn.waitTime < 0f || !float.IsFinite(spawn.waitTime))
                        {
                            return null;
                        }

                        // JSON에서 읽은 Prefab, 좌표, 대기시간으로
                        // EnemySpawnInfo 하나 생성
                        WaveInfo.EnemySpawnInfo enemySpawnInfo =
                            new WaveInfo.EnemySpawnInfo(
                                enemyPrefab,
                                new Vector2Int(spawn.x, spawn.z),
                                spawn.waitTime
                            );

                        // 현재 Wave의 적 생성 정보 목록에 추가
                        enemySpawnInfos.Add(enemySpawnInfo);
                    }
                }
            }

            // 만들어진 EnemySpawnInfo들을 하나의 WaveInfo로 묶음
            return new WaveInfo(enemySpawnInfos.ToArray());
        }

        // Wave 파일의 숫자를 기준으로 정렬
        // 예: Wave1.json -> Wave2.json -> Wave10.json
        private static int CompareWaveFileNames(
            string left,
            string right)
        {
            int leftNumber = GetWaveNumber(left);
            int rightNumber = GetWaveNumber(right);

            int comparison = leftNumber.CompareTo(rightNumber);
            return comparison != 0 ? comparison : string.CompareOrdinal(left, right);
        }

        // 파일 이름에서 Wave 번호를 추출
        private static int GetWaveNumber(string filePath)
        {
            string fileName =
                Path.GetFileNameWithoutExtension(filePath);

            Match match = Regex.Match(fileName, @"\d+");

            if (match.Success &&
                int.TryParse(match.Value, out int number))
            {
                return number;
            }

            // 숫자가 없는 파일은 가장 뒤로 보냄
            return int.MaxValue;
        }


        // =========================
        // JSON 역직렬화용 자료형
        // =========================

        // JSON 파일 하나의 전체 구조
        [Serializable]
        private class WaveJson
        {
            // 해당 Wave에 등장할 적 종류 목록
            public EnemyJson[] enemies;
        }

        // 적 한 종류에 대한 정보
        [Serializable]
        private class EnemyJson
        {
            // 생성할 Enemy Prefab의 이름
            public string prefabName;

            // 같은 Prefab에 적용할 Status 설정 목록
            public StatusSettingJson[] statusSettings;
        }

        // 하나의 Status 설정과
        // 해당 설정으로 생성될 Enemy들의 정보
        [Serializable]
        private class StatusSettingJson
        {
            // Prefab 기본 Status에 더하거나 뺄 변화량
            // 요구사항에 따라 실제 Enemy에는 적용하지 않음
            public StatusDeltaJson statusDelta;

            // 이 설정을 사용하는 Enemy들의
            // 생성 좌표와 대기시간
            public SpawnJson[] spawns;
        }

        // Prefab의 기본 Status에서 변경할 값
        [Serializable]
        private class StatusDeltaJson
        {
            // 이동속도 변화량
            public float speed;

            // 공격력 변화량
            public float attackDamage;
        }

        // Enemy 하나의 생성 정보
        [Serializable]
        private class SpawnJson
        {
            // Grid의 col 좌표
            public int x;

            // Grid의 row 좌표
            public int z;

            // Wave 시작 후 실제 생성까지 기다릴 시간
            public float waitTime;
        }
    }
}
