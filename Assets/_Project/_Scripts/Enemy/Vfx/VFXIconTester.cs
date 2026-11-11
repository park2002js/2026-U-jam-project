using UJam.Runtime.Enemy;
using UJam.Runtime.Systems;
using UnityEngine;

namespace UJam.Runtime.Test
{
    /// <summary>
    /// 속성 부여 확인용 임시 코드. 씬의 모든 적에게 키로 속성을 건다.
    /// 실제 ElementManager를 통하므로 속성 VFX와 체력바 아이콘이 함께 표시된다.
    /// </summary>
    public class ElementTester : MonoBehaviour
    {
        [Header("테스트 키")]
        [SerializeField] private KeyCode _burnKey = KeyCode.Z;      // 화상
        [SerializeField] private KeyCode _freezeKey = KeyCode.X;    // 빙결
        [SerializeField] private KeyCode _shockKey = KeyCode.C;     // 감전
        [SerializeField] private KeyCode _bleedKey = KeyCode.V;     // 출혈

        [Header("옵션")]
        [SerializeField, Tooltip("속성과 함께 1의 피해를 주어 체력바가 뜨게 한다.")]
        private bool _dealTinyDamage = true;

        private void Update()
        {
            if (Input.GetKeyDown(_burnKey)) ApplyToAll(ElementType.Burn);
            if (Input.GetKeyDown(_freezeKey)) ApplyToAll(ElementType.Freeze);
            if (Input.GetKeyDown(_shockKey)) ApplyToAll(ElementType.Shock);
            if (Input.GetKeyDown(_bleedKey)) ApplyToAll(ElementType.Bleed);
        }

        private void ApplyToAll(ElementType type)
        {
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);

            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null || enemy.Status == null || enemy.Status.HP <= 0f) continue;

                ElementManager.Instance.Apply(enemy, type);

                // 체력바는 피해를 받아야 생성되므로 최소 피해를 함께 준다
                if (_dealTinyDamage) enemy.Status.ApplyDamage(1f, true);
            }

            Debug.Log($"[ElementTester] {type} 적용 → {enemies.Length}마리", this);
        }
    }
}