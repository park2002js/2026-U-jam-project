namespace UJam.Runtime.Combat
{
    public interface IAttackSource
    {
        Faction Faction { get; }

        AttackId AttackId { get; }
    }
}
