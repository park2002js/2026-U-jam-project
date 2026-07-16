using System;

namespace UJam.Runtime.Navigation
{
    public readonly struct TraversalProfile : IEquatable<TraversalProfile>
    {
        // 이동체가 사용할 통과 능력을 저장하는 프로필 생성자
        public TraversalProfile(bool canJump, bool canFly, bool canBreakObstacles)
        {
            CanJump = canJump;
            CanFly = canFly;
            CanBreakObstacles = canBreakObstacles;
        }

        // 점프 통과 능력 여부
        public bool CanJump { get; }

        // 비행 통과 능력 여부
        public bool CanFly { get; }

        // 장애물 파괴 통과 능력 여부
        public bool CanBreakObstacles { get; }

        // 통과 능력 값 비교
        public bool Equals(TraversalProfile other)
        {
            // 세 가지 통과 능력이 모두 같은지 확인
            if (CanJump != other.CanJump || CanFly != other.CanFly || CanBreakObstacles != other.CanBreakObstacles)
            {
                // 다른 통과 프로필은 일치하지 않는 결과
                return false;
            }

            // 같은 통과 프로필 결과
            return true;
        }

        // 다른 객체와 통과 능력 값 비교
        public override bool Equals(object obj)
        {
            // 같은 타입의 통과 프로필인지 확인
            if (obj is TraversalProfile other)
            {
                // 같은 타입 값을 비교한 결과
                return Equals(other);
            }

            // 다른 타입 객체는 일치하지 않는 결과
            return false;
        }

        // 통과 능력 값을 해시 값으로 변환
        public override int GetHashCode()
        {
            // 세 가지 능력을 비트 값으로 묶은 해시 입력
            int flags = (CanJump ? 1 : 0) | (CanFly ? 2 : 0) | (CanBreakObstacles ? 4 : 0);

            // 통과 능력 해시 값 반환
            return flags;
        }

        // 통과 능력 값이 같은지 비교
        public static bool operator ==(TraversalProfile left, TraversalProfile right)
        {
            // 두 프로필의 값 비교 결과
            return left.Equals(right);
        }

        // 통과 능력 값이 다른지 비교
        public static bool operator !=(TraversalProfile left, TraversalProfile right)
        {
            // 두 프로필의 반대 비교 결과
            return !left.Equals(right);
        }
    }
}
