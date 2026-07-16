using UJam.Runtime.Combat;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Player
{
    public interface IPlayerAttackTargetProvider
    {
        // 주입된 Grid 조회 포트로 공격 대상 HitZone을 찾음
        bool TryGetAttackTarget(IGridAreaQuery gridAreaQuery, out HitZoneReceiver target);
    }
}
