namespace UJam.Runtime.BuildingPlacement
{
    public interface IDefenseFactory
    {
        // Defense 생성과 점유 핸들 소유권 이전을 요청
        bool TryCreate(DefenseSpawnRequest request);
    }
}
