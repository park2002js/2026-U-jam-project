using System;

namespace UJam.Runtime.Phase
{
    public readonly struct StageResult
    {
        public StageResult(int stageId, bool isComplete, WaveProgress finalWaveProgress)
        {
            if (stageId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stageId));
            }

            StageId = stageId;
            IsComplete = isComplete;
            FinalWaveProgress = finalWaveProgress;
        }

        public int StageId { get; }
        public bool IsComplete { get; }
        public WaveProgress FinalWaveProgress { get; }
    }

    public readonly struct StageCompleted
    {
        public StageCompleted(StageResult result)
        {
            Result = result;
        }

        public StageResult Result { get; }
    }
}
