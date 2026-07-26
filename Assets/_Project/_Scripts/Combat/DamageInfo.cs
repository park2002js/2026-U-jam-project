namespace UJam.Runtime.Combat
{
    // 피해를 전달한 전투 주체 종류
    public enum DamageSourceKind
    {
        // 종류가 지정되지 않은 피해
        Unknown,

        // Player가 전달한 피해
        Player,

        // Enemy가 전달한 피해
        Enemy
    }

    public readonly struct DamageInfo
    {
        // 필수 피해량과 선택적인 전달 주체 저장
        public DamageInfo(  
            float damage,   // 피해량
            string source = null,   // 데미지를 준 주체 (즉, TakeDamage를 호출한 사람)
            DamageSourceKind sourceKind = DamageSourceKind.Unknown)
        {
            // 외부에서 결정한 피해량 저장
            Damage = damage;

            // 생략할 수 있는 피해 전달 주체 저장
            Source = source;

            // 피해를 전달한 전투 주체 종류 저장
            SourceKind = sourceKind;
        }

        // Health에 전달할 피해량
        public float Damage { get; }

        // 피해를 보낸 선택적인 주체 이름
        public string Source { get; }

        // 피해를 보낸 전투 주체 종류
        public DamageSourceKind SourceKind { get; }
    }
}
