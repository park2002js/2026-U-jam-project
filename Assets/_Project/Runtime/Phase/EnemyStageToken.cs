using System;

namespace UJam.Runtime.Phase
{
    public readonly struct EnemyStageToken : IEquatable<EnemyStageToken>
    {
        // 토큰이 발행된 스테이지 번호를 보관
        public int StageId { get; }

        // 스테이지 안에서 발행된 고유 순번을 보관
        public long Sequence { get; }

        // 양수 순번을 가진 토큰인지 확인
        public bool IsValid
        {
            get
            {
                // 스테이지와 순번이 유효한지 확인
                return StageId >= 0 && Sequence > 0;
            }
        }

        // 스테이지 번호와 순번으로 토큰을 생성
        public EnemyStageToken(int stageId, long sequence)
        {
            // 스테이지 번호가 음수인지 확인
            if (stageId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stageId));
            }

            // 순번이 양수인지 확인
            if (sequence <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            StageId = stageId;
            Sequence = sequence;
        }

        // 두 토큰의 값이 같은지 비교
        public bool Equals(EnemyStageToken other)
        {
            // 스테이지와 순번을 함께 비교
            return StageId == other.StageId && Sequence == other.Sequence;
        }

        // 객체와 토큰의 값이 같은지 비교
        public override bool Equals(object obj)
        {
            // 같은 토큰 형식인지 확인
            if (!(obj is EnemyStageToken))
            {
                // 다른 형식은 같은 값이 아님을 반환
                return false;
            }

            // 토큰 값 비교 결과를 반환
            return Equals((EnemyStageToken)obj);
        }

        // 토큰 값으로 해시 코드를 생성
        public override int GetHashCode()
        {
            // 두 값의 해시를 결합해 반환
            return HashCode.Combine(StageId, Sequence);
        }

        // 두 토큰이 같은지 비교
        public static bool operator ==(EnemyStageToken left, EnemyStageToken right)
        {
            // 값 비교 결과를 반환
            return left.Equals(right);
        }

        // 두 토큰이 다른지 비교
        public static bool operator !=(EnemyStageToken left, EnemyStageToken right)
        {
            // 값 비교 결과를 반환
            return !left.Equals(right);
        }
    }
}
