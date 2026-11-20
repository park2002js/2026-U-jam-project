using System;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    /// <summary>
    /// 키 입력으로 씬의 모든 적에게 디버프를 건다. 아이콘·VFX 확인용 임시 스크립트.
    /// 실제 아이템과 연결되면 삭제한다.
    /// </summary>
    public class EnemyDebuffTester : MonoBehaviour
    {
        [Serializable]
        private class Binding
        {
            public KeyCode key;
            public EnemyDebuffType type;

            [Tooltip("0 이하면 EnemyDebuffManager의 기본 지속시간을 쓴다.")]
            public float duration = -1f;
        }

        [Header("키 설정")]
        [SerializeField]
        private Binding[] _bindings =
        {
            new Binding { key = KeyCode.F1, type = EnemyDebuffType.AttackDown,    duration = -1f },
            new Binding { key = KeyCode.F2, type = EnemyDebuffType.DamageTakenUp, duration = -1f },
            new Binding { key = KeyCode.F3, type = EnemyDebuffType.SlowDown,      duration = -1f },
            new Binding { key = KeyCode.F4, type = EnemyDebuffType.Marked,        duration = -1f },
        };

        [SerializeField]
        [Tooltip("모든 적의 디버프를 지운다.")]
        private KeyCode _clearKey = KeyCode.F5;

        [Header("옵션")]
        [SerializeField]
        [Tooltip("체력바는 피해를 입어야 나타난다. 켜두면 디버프와 함께 1의 피해를 준다.")]
        private bool _dealTinyDamage = true;

        private void Update()
        {
            for (int i = 0; i < _bindings.Length; i++)
            {
                var b = _bindings[i];
                if (b == null || b.key == KeyCode.None) continue;
                if (!Input.GetKeyDown(b.key)) continue;

                ApplyToAll(b.type, b.duration);
            }

            if (_clearKey != KeyCode.None && Input.GetKeyDown(_clearKey))
                ClearAll();
        }

        private void ApplyToAll(EnemyDebuffType type, float duration)
        {
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            if (enemies.Length == 0)
            {
                Debug.Log($"[EnemyDebuffTester] 씬에 적이 없다. ({type})");
                return;
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;

                EnemyDebuffManager.Instance.Apply(enemy, type, duration);

                if (_dealTinyDamage && enemy.Status != null)
                    enemy.Status.ApplyDamage(1f, true);
            }

            Debug.Log($"[EnemyDebuffTester] {type} 부여 — 적 {enemies.Length}체");
        }

        private void ClearAll()
        {
            EnemyDebuffManager.Instance.ClearAll();
            Debug.Log("[EnemyDebuffTester] 전체 디버프 해제");
        }
    }
}