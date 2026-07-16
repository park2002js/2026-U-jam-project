using System;

namespace UJam.Runtime.Phase
{
    public readonly struct WaveProgress
    {
        public WaveProgress(int totalSpawned, int alive, int defeated)
        {
            if (totalSpawned < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalSpawned));
            }

            if (alive < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(alive));
            }

            if (defeated < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(defeated));
            }

            if ((long)alive + defeated > totalSpawned)
            {
                throw new ArgumentException("Alive and defeated enemies cannot exceed total spawned enemies.");
            }

            TotalSpawned = totalSpawned;
            Alive = alive;
            Defeated = defeated;
        }

        public int TotalSpawned { get; }
        public int Alive { get; }
        public int Defeated { get; }
    }

    public readonly struct EnemySpawned
    {
    }

    public readonly struct EnemyDefeated
    {
    }
}
