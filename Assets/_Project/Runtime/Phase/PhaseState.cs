using System;

namespace UJam.Runtime.Phase
{
    public enum PhaseState
    {
        Preparation,
        Combat,
        StageClear
    }

    public readonly struct StartCombatCommand
    {
    }

    public readonly struct PhaseChanged
    {
        public PhaseChanged(PhaseState previous, PhaseState current)
        {
            Previous = previous;
            Current = current;
        }

        public PhaseState Previous { get; }
        public PhaseState Current { get; }
    }

    public readonly struct WaveStartRequested
    {
        public WaveStartRequested(int stageId)
        {
            if (stageId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stageId));
            }

            StageId = stageId;
        }

        public int StageId { get; }
    }
}
