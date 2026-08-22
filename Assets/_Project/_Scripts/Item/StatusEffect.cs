using UnityEngine;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;

namespace UJam.Runtime.Item
{
    // 효과 종류
    public enum EffectType
    {
        DamageOverTime,   // 도트 — 매 틱 피해
        Slow,             // 슬로우 — 이동속도 감소
        AttackSpeedUp     // 공속 증가 (현재 미구현, 로그만)
    }

    [CreateAssetMenu(menuName = "Game/Items/Effects/StatusEffect")]
    public sealed class StatusEffect : ItemEffect
    {
        [Header("효과 설정")]
        [SerializeField] private EffectType effectType;
        [SerializeField] private float amount = 5f;   // 도트=피해량, 슬로우=감소량, 공속=증가량

        // 효과가 걸릴 때 1회
        public override void Apply(ItemUseContext context)
        {
            switch (effectType)
            {
                case EffectType.DamageOverTime:
                    Debug.Log($"<color=lime>[도트] 지속 피해 시작 — 틱당 {amount}</color>");
                    break;

                case EffectType.Slow:
                    ModifyStat(context, -amount);   // 감소
                    Debug.Log($"<color=cyan>[슬로우] 이동속도 -{amount} 적용</color>");
                    break;

                case EffectType.AttackSpeedUp:
                    Debug.Log($"<color=orange>[공속증가] 공격속도 +{amount} 적용 예정 (미구현)</color>");
                    break;
            }
        }

        // 지속 중 TickInterval마다
        public override void Tick(ItemUseContext context)
        {
            // 도트만 반복 동작
            if (effectType != EffectType.DamageOverTime) return;
            if (context.Target == null) return;

            EnemyStatus status = context.Target.GetComponent<EnemyStatus>();
            if (status == null)
            {
                Debug.Log("[도트] EnemyStatus 없음");
                return;
            }

            status.ApplyDamage(amount);
        }

        // 지속시간이 끝날 때 1회 (되돌리기)
        public override void Remove(ItemUseContext context)
        {
            switch (effectType)
            {
                case EffectType.DamageOverTime:
                    Debug.Log("<color=grey>[도트] 지속 피해 종료</color>");
                    break;

                case EffectType.Slow:
                    ModifyStat(context, amount);   // 걸었던 만큼 원복
                    Debug.Log($"<color=grey>[슬로우] 이동속도 원복 (+{amount})</color>");
                    break;

                case EffectType.AttackSpeedUp:
                    Debug.Log("<color=grey>[공속증가] 효과 종료 (미구현)</color>");
                    break;
            }
        }

        // 대상의 스탯을 인터페이스로 증감 (적·아군 공통)
        private void ModifyStat(ItemUseContext context, float delta)
        {
            if (context.Target == null) return;

            IStatModifiable stat = context.Target.GetComponent<IStatModifiable>();
            if (stat == null)
            {
                Debug.Log("[효과] IStatModifiable 없음");
                return;
            }

            stat.ModifySpeed(delta);
        }
    }
}