using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.Core
{
    /// <summary>
    /// Handles global leaderboard submission and queries through Unity Leaderboards.
    /// </summary>
    public sealed class LeaderboardManager
    {
        public const string DefaultLeaderboardId = "MergeGameLeaderboard";

        readonly GameManagerUGS m_GameManagerUGS;
        readonly SeedManager m_SeedManager;

        public event Action<LeaderboardScoresPage> GlobalLeaderboardUpdated;

        public LeaderboardManager(GameManagerUGS gameManagerUGS, SeedManager seedManager)
        {
            m_GameManagerUGS = gameManagerUGS ?? throw new ArgumentNullException(nameof(gameManagerUGS));
            m_SeedManager = seedManager ?? throw new ArgumentNullException(nameof(seedManager));
            m_GameManagerUGS.MatchComplete += NotifyRunCompleted;
        }

        public int LastKnownSeed => m_SeedManager.LastSeed;

        public void NotifyRunCompleted(int score, int seed)
        {
            _ = SubmitScoreAsync(score, seed);
        }

        public async Task<LeaderboardEntry> SubmitScoreAsync(int score, int seed)
        {
            if (m_GameManagerUGS.IsOfflineMode)
            {
                Logger.LogDemo("Leaderboard submission skipped (Offline Mode).");
                return null;
            }

            EnsureAuthenticated();

            var result = await LeaderboardsService.Instance.AddPlayerScoreAsync(
                DefaultLeaderboardId,
                score,
                new AddPlayerScoreOptions
                {
                    Metadata = new { seed }
                });

            Logger.LogDemo($"Leaderboard submitted. Rank={result.Rank} Score={result.Score}");
            return result;
        }

        public async Task<LeaderboardScoresPage> GetGlobalLeaderboardAsync(int limit = 50, int offset = 0)
        {
            EnsureAuthenticated();

            var scores = await LeaderboardsService.Instance.GetScoresAsync(
                DefaultLeaderboardId,
                new GetScoresOptions
                {
                    Limit = limit,
                    Offset = offset,
                    IncludeMetadata = true
                });

            GlobalLeaderboardUpdated?.Invoke(scores);
            return scores;
        }

        static void EnsureAuthenticated()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                throw new InvalidOperationException("Unity Services must be initialized before leaderboard calls.");

            if (!AuthenticationService.Instance.IsSignedIn)
                throw new InvalidOperationException("Unity Authentication sign-in required before leaderboard calls.");
        }
    }
}
