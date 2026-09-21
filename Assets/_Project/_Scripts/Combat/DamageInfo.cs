using UJam.Runtime.Player;

namespace UJam.Runtime.Combat
{
    public enum DamageSourceKind { Unknown, Player, Enemy }

    /// <summary>최종 전달 피해와 처치 주체. 기존 3인자 호출도 유지하며 출혈 고정 피해는 IgnoreDamageTaken을 켠다.</summary>
    public readonly struct DamageInfo
    {
        /// <summary>공격 시 생성한다. attacker는 보상/처치 패시브에 필요하며 ignoreDamageTaken은 출혈 등 고정 피해 전용이다.</summary>
        public DamageInfo(float damage, string source = null, DamageSourceKind sourceKind = DamageSourceKind.Unknown,
            PlayerStatus attacker = null, bool ignoreDamageTaken = false)
        { Damage = damage; Source = source; SourceKind = sourceKind; Attacker = attacker; IgnoreDamageTaken = ignoreDamageTaken; }
        public float Damage { get; }
        public string Source { get; }
        public DamageSourceKind SourceKind { get; }
        public PlayerStatus Attacker { get; }
        public bool IgnoreDamageTaken { get; }
    }
}
