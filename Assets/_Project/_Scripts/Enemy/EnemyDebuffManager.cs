using System;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    /// <summary>적 하나에 걸린 디버프 하나의 상태.</summary>
    public class EnemyDebuffState
    {
        public EnemyDebuffType Type;
        public float EndTime;

        public float Remaining => Mathf.Max(0f, EndTime - Time.time);
        public bool Expired => Time.time >= EndTime;
    }

    /// <summary>
    /// 적 디버프의 지속시간만 관리한다. ElementManager와 같은 역할이되, 속성이 아닌 디버프를 다룬다.
    /// 지금은 실제 스탯 변화를 적용하지 않고 "걸려 있다"는 사실만 들고 있다.
    /// 아이콘·VFX를 먼저 확인하기 위한 단계이며, 나중에 BuffManager와 연결하면 된다.
    /// </summary>
    public class EnemyDebuffManager : MonoBehaviour
    {
        private static EnemyDebuffManager _instance;

        /// <summary>씬에 없으면 자동으로 만든다. 수동 배치를 잊어도 동작하게 하기 위함.</summary>
        public static EnemyDebuffManager Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<EnemyDebuffManager>();
                if (_instance != null) return _instance;

                var go = new GameObject("[EnemyDebuffManager]");
                _instance = go.AddComponent<EnemyDebuffManager>();
                return _instance;
            }
        }

        [Serializable]
        private class DurationOverride
        {
            public EnemyDebuffType type;
            [Min(0.1f)] public float duration = 5f;
        }

        [Header("지속시간")]
        [SerializeField, Min(0.1f)]
        [Tooltip("아래에서 따로 지정하지 않은 디버프에 쓰이는 기본 지속시간.")]
        private float _defaultDuration = 5f;

        [SerializeField]
        [Tooltip("종류별로 다른 지속시간을 쓰고 싶을 때만 채운다.")]
        private DurationOverride[] _durationOverrides = Array.Empty<DurationOverride>();

        private readonly Dictionary<EnemyBase, Dictionary<EnemyDebuffType, EnemyDebuffState>> _active =
            new Dictionary<EnemyBase, Dictionary<EnemyDebuffType, EnemyDebuffState>>();

        private readonly List<EnemyBase> _deadKeys = new List<EnemyBase>();
        private readonly List<EnemyDebuffType> _expired = new List<EnemyDebuffType>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (_active.Count == 0) return;

            _deadKeys.Clear();

            foreach (var pair in _active)
            {
                // 적이 파괴·풀 반환된 경우 통째로 정리한다.
                if (pair.Key == null)
                {
                    _deadKeys.Add(pair.Key);
                    continue;
                }

                _expired.Clear();
                foreach (var entry in pair.Value)
                    if (entry.Value.Expired) _expired.Add(entry.Key);

                for (int i = 0; i < _expired.Count; i++)
                    pair.Value.Remove(_expired[i]);

                if (pair.Value.Count == 0) _deadKeys.Add(pair.Key);
            }

            for (int i = 0; i < _deadKeys.Count; i++)
                _active.Remove(_deadKeys[i]);
        }

        /// <summary>종류별 지속시간. 지정이 없으면 기본값.</summary>
        public float DurationOf(EnemyDebuffType type)
        {
            for (int i = 0; i < _durationOverrides.Length; i++)
                if (_durationOverrides[i] != null && _durationOverrides[i].type == type)
                    return _durationOverrides[i].duration;

            return _defaultDuration;
        }

        /// <summary>디버프를 건다. 이미 걸려 있으면 중첩하지 않고 지속시간을 갱신한다.</summary>
        /// <param name="duration">0 이하를 넣으면 종류별 기본 지속시간을 쓴다.</param>
        public EnemyDebuffState Apply(EnemyBase enemy, EnemyDebuffType type, float duration = -1f)
        {
            if (enemy == null) return null;

            if (duration <= 0f) duration = DurationOf(type);

            if (!_active.TryGetValue(enemy, out var byType))
            {
                byType = new Dictionary<EnemyDebuffType, EnemyDebuffState>();
                _active[enemy] = byType;
            }

            if (!byType.TryGetValue(type, out var state))
            {
                state = new EnemyDebuffState { Type = type };
                byType[type] = state;
            }

            state.EndTime = Time.time + duration;
            return state;
        }

        public bool TryGet(EnemyBase enemy, EnemyDebuffType type, out EnemyDebuffState state)
        {
            state = null;
            if (enemy == null) return false;
            if (!_active.TryGetValue(enemy, out var byType)) return false;
            if (!byType.TryGetValue(type, out state)) return false;

            if (state.Expired)
            {
                byType.Remove(type);
                state = null;
                return false;
            }
            return true;
        }

        public bool Has(EnemyBase enemy, EnemyDebuffType type) => TryGet(enemy, type, out _);

        public void Remove(EnemyBase enemy, EnemyDebuffType type)
        {
            if (enemy == null) return;
            if (!_active.TryGetValue(enemy, out var byType)) return;

            byType.Remove(type);
            if (byType.Count == 0) _active.Remove(enemy);
        }

        /// <summary>적이 죽거나 풀로 돌아갈 때 호출한다.</summary>
        public void Clear(EnemyBase enemy)
        {
            if (enemy == null) return;
            _active.Remove(enemy);
        }

        public void ClearAll() => _active.Clear();
    }
}