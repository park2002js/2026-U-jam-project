using System;

namespace UJam.Runtime.Navigation
{
    public readonly struct ObstacleHandle : IEquatable<ObstacleHandle>
    {
        // 공통 장애물을 식별하는 양수 Handle 생성자
        public ObstacleHandle(long value)
        {
            // 유효하지 않은 장애물 식별자 차단
            if (value <= 0)
            {
                // 장애물 Handle은 양수만 허용
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        // 공통 장애물 식별자
        public long Value { get; }

        // 장애물 Handle이 유효한지 확인
        public bool IsValid
        {
            get
            {
                // 양수 식별자만 유효한 장애물 Handle
                return Value > 0;
            }
        }

        // 장애물 Handle 값 비교
        public bool Equals(ObstacleHandle other)
        {
            // 식별자 값 비교 결과
            return Value == other.Value;
        }

        // 다른 객체와 장애물 Handle 값 비교
        public override bool Equals(object obj)
        {
            // 같은 타입의 장애물 Handle인지 확인
            if (obj is ObstacleHandle other)
            {
                // 같은 타입 값 비교 결과
                return Equals(other);
            }

            // 다른 타입 객체는 일치하지 않는 결과
            return false;
        }

        // 장애물 Handle 값을 해시 값으로 변환
        public override int GetHashCode()
        {
            // 식별자 해시 값 반환
            return Value.GetHashCode();
        }

        // 장애물 Handle 값이 같은지 비교
        public static bool operator ==(ObstacleHandle left, ObstacleHandle right)
        {
            // 두 장애물 Handle의 값 비교 결과
            return left.Equals(right);
        }

        // 장애물 Handle 값이 다른지 비교
        public static bool operator !=(ObstacleHandle left, ObstacleHandle right)
        {
            // 두 장애물 Handle의 반대 비교 결과
            return !left.Equals(right);
        }
    }
}
