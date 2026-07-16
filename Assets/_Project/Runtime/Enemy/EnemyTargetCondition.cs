namespace UJam.Runtime.Enemy
{
    public readonly struct EnemyTargetCondition
    {
        // 현재 표적 존재 여부와 공격 사거리 충족 여부를 저장하는 조건 생성자
        public EnemyTargetCondition(bool hasTarget, bool isWithinAttackRange)
        {
            _hasTarget = hasTarget;
            _isWithinAttackRange = isWithinAttackRange;
        }

        // 현재 유효한 표적 존재 여부
        public bool HasTarget
        {
            get
            {
                // 표적 존재 조건 반환
                return _hasTarget;
            }
        }

        // 현재 표적의 공격 사거리 충족 여부
        public bool IsWithinAttackRange
        {
            get
            {
                // 공격 사거리 조건 반환
                return _isWithinAttackRange;
            }
        }

        // 표적 존재 조건
        private readonly bool _hasTarget;

        // 공격 사거리 조건
        private readonly bool _isWithinAttackRange;
    }
}
