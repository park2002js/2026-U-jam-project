using System;
using UnityEngine;

namespace UJam.Runtime.Phase
{
    public sealed class WaveInfo
    {
        [Serializable]
        public struct EnemySpawnInfo
        {
            // 생성할 Enemy Prefab
            [SerializeField] private GameObject _enemyPrefab;

            // x는 col이고 y는 row인 Grid 좌표
            [SerializeField] private Vector2Int _gridPosition;

            // Wave 시작 뒤 Enemy가 활성화될 때까지 기다릴 시간
            [Min(0f)]
            [SerializeField] private float _waitTime;

            // 생성할 Enemy Prefab
            public GameObject EnemyPrefab
            {
                get
                {
                    // 생성할 Enemy Prefab 반환
                    return _enemyPrefab;
                }
            }

            // Enemy를 배치할 Grid 좌표 (x는 col, y는 row)
            public Vector2Int GridPosition
            {
                get
                {
                    // Enemy를 배치할 Grid 좌표 반환
                    return _gridPosition;
                }
            }

            // Enemy별 활성화 대기시간
            public float WaitTime
            {
                get
                {
                    // Enemy별 활성화 대기시간 반환
                    return _waitTime;
                }
            }

            // JSON에서 읽은 정보를 바탕으로 EnemySpawnInfo 생성
            public EnemySpawnInfo(
                GameObject enemyPrefab,
                Vector2Int gridPosition,
                float waitTime)
            {
                _enemyPrefab = enemyPrefab;
                _gridPosition = gridPosition;
                _waitTime = waitTime;
            }
        }

        // 이 Wave에서 생성할 모든 Enemy 정보
        [SerializeField] private EnemySpawnInfo[] _enemies = Array.Empty<EnemySpawnInfo>();

        // 배열 길이로 계산한 총 Enemy 수
        public int TotalEnemyCount
        {
            get
            {
                // 배열 누락 시 0을 반환
                return _enemies == null ? 0 : _enemies.Length;
            }
        }

        // WaveController가 순회할 Enemy 정보 배열
        public EnemySpawnInfo[] Enemies
        {
            get
            {
                // 배열 누락 시 빈 배열을 반환
                return _enemies ?? Array.Empty<EnemySpawnInfo>();
            }
        }

        // 변환된 EnemySpawnInfo 배열을 받아 하나의 WaveInfo 생성
        public WaveInfo(EnemySpawnInfo[] enemies)
        {
            _enemies = enemies ?? Array.Empty<EnemySpawnInfo>();
        }
    }   
}
