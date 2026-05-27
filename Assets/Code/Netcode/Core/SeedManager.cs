using System;

namespace Suika.Scripts.Core
{
    /// <summary>
    /// Minimal deterministic seed service.
    /// Keeps current boot flow compiling while the existing ECS seed logic provides per-run randomness.
    /// </summary>
    public sealed class SeedManager
    {
        readonly LocalStorageSystem m_LocalStorageSystem;

        public int LastSeed { get; private set; }

        public SeedManager(LocalStorageSystem localStorageSystem)
        {
            m_LocalStorageSystem = localStorageSystem ?? throw new ArgumentNullException(nameof(localStorageSystem));
        }

        public void SetSeed(int seed)
        {
            LastSeed = seed;
        }

        public int GetSeed(int fallbackSeed = 0)
{
            if (fallbackSeed != 0)
            {
                LastSeed = fallbackSeed;
                return LastSeed;
            }

            var playerData = m_LocalStorageSystem.LoadPlayerData();
            LastSeed = playerData?.LastSeedUsed is > 0 ? playerData.LastSeedUsed : 123456789;
            return LastSeed;
        }
    }
}