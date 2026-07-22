using UnityEngine;
using UJam.Runtime.Combat;

namespace UJam.Runtime.Defense
{
    [RequireComponent(typeof(Health))]
    public sealed class Barricade : DefenseBase, IDamageable
    {
        // Barricade 체력을 관리할 Component
        [SerializeField] private Health _health;

        // 공통 설정과 Health 연결 준비
        protected override void Awake()
        {
            // Defense 공통 설정 먼저 보정
            base.Awake();

            // Inspector 참조가 없을 때 같은 GameObject에서 Health를 확인
            if (_health == null)
            {
                // 같은 GameObject의 Health를 대체 참조로 저장
                _health = GetComponent<Health>();
            }

            // Health가 준비됐을 때만 사망 이벤트 연결
            if (_health != null)
            {
                _health.Died += HandleHealthDied;
            }
        }

        // 외부 피해 요청을 Barricade Health에 전달
        public float TakeDamage(DamageInfo info)
        {
            // Enemy가 아닌 공격과 파괴 뒤 피해 차단
            if (info.SourceKind != DamageSourceKind.Enemy || IsDestroyed || _health == null)
            {
                // 적용되지 않은 피해량 반환
                return 0f;
            }

            // Health가 실제로 적용한 피해량 반환
            return _health.ApplyDamage(info.Damage);
        }

        // Health 사망을 공통 Defense 파괴로 연결
        private void HandleHealthDied()
        {
            // 설치 해제 알림을 포함한 공통 파괴 요청
            DestroyDefense();
        }

        // Barricade 파괴 시 Health 이벤트 연결 해제
        protected override void OnDefenseDestroyed()
        {
            // 연결된 Health가 있을 때만 이벤트 제거
            if (_health != null)
            {
                _health.Died -= HandleHealthDied;
            }

            // 추가 Barricade 파괴 동작 확장 지점 유지
            base.OnDefenseDestroyed();
        }
    }
}
