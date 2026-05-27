using System;
using System.Threading.Tasks;
using Suika.Scripts.Core;
using Suika.Scripts.Utilities;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings.SuikaUGSCloud.Models;
using Task = System.Threading.Tasks.Task;

namespace Suika.Scripts.PlayerDataManagement
{
    /// <summary>
    /// Manages player data synchronization with Cloud Code.
    /// Handles sign-in initialization, profile picture fetching, score submission,
    /// and connectivity recovery. GemHunter-era level-won/lost/hearts/area methods removed.
    /// </summary>
    public class PlayerDataManagerClient : IDisposable
    {
        public bool IsPlayerInitializedInCloud { get; private set; }
        bool m_IsInitializing;

        readonly GameManagerUGS m_GameManagerUGS;
        readonly NetworkConnectivityHandler m_NetworkHandler;
        readonly CloudBindingsProvider m_BindingsProvider;
        readonly PlayerAuthenticationManager m_AuthenticationManager;

        PlayerData m_CloudPlayerData;
        PlayerInitializationResponse m_LastInitResponse;

        const int k_MaxInitRetries = 3;
        const int k_RetryDelayMs = 1000;

        // Events
        public event Action<PlayerData> PlayerDataInitialized;
        public event Action<PlayerData> PlayerDataUpdated;
        public event Action PlayerInitialized;
        public event Action<ProfilePicture> ProfilePictureFetched;

        public PlayerDataManagerClient(
            GameManagerUGS gameManagerUGS,
            PlayerAuthenticationManager authenticationManager,
            CloudBindingsProvider bindingsProvider,
            NetworkConnectivityHandler networkHandler)
        {
            m_GameManagerUGS = gameManagerUGS;
            m_AuthenticationManager = authenticationManager;
            m_NetworkHandler = networkHandler;
            m_BindingsProvider = bindingsProvider;

            m_GameManagerUGS.MatchComplete += HandleMatchComplete;
            m_AuthenticationManager.SignedIn += HandleSignInInitialization;
            m_NetworkHandler.OnlineStatusChanged += HandleConnectivityChanged;
        }

        // -----------------------------------------------------------------------
        // Sign-in & initialization
        // -----------------------------------------------------------------------

        async void HandleSignInInitialization()
        {
            if (m_GameManagerUGS.IsOfflineMode)
            {
                Logger.LogDemo("🚫 Skipping cloud initialization (Offline Mode)");
                return;
            }

            if (m_IsInitializing)
{
                Logger.LogDemo("🚫 Initialization already in progress");
                return;
            }
            if (IsPlayerInitializedInCloud)
            {
                Logger.LogDemo("🚫 Player already initialized");
                return;
            }

            m_IsInitializing = true;
            Logger.LogDemo("🟢 Starting player initialization in CloudCode ☁...");

            try
            {
                await HandlePlayerSignIn();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Player initialization failed: {ex.Message}");
            }
            finally
            {
                m_IsInitializing = false;
            }
        }

        async Task HandlePlayerSignIn()
        {
            bool success = await InitializePlayerWithRetry();
            if (!success)
            {
                Logger.LogError("Failed to initialize player after multiple attempts.");
                return;
            }
            Logger.LogDemo("☁⚡ PlayerInitialized");

            PlayerInitialized?.Invoke();
            PlayerDataInitialized?.Invoke(m_LastInitResponse.PlayerData);
            PlayerDataUpdated?.Invoke(m_CloudPlayerData);

            if (m_LastInitResponse.ProfilePicture != null)
                ProfilePictureFetched?.Invoke(m_LastInitResponse.ProfilePicture);
            else
                Logger.LogWarning("Profile picture is null after init.");
        }

        async Task<bool> InitializePlayerWithRetry()
        {
            if (IsPlayerInitializedInCloud) return true;

            for (int attempt = 0; attempt < k_MaxInitRetries; attempt++)
            {
                try
                {
                    Logger.LogDemo("Attempting cloud initialization...");
                    var initResponse = await m_BindingsProvider.SuikaGameBindings.OnSignInHandlePlayerInitialization();

                    if (initResponse?.PlayerData == null)
                        return false;

                    IsPlayerInitializedInCloud = true;
                    m_CloudPlayerData = initResponse.PlayerData;
                    m_LastInitResponse = initResponse;

                    Logger.LogDemo($"☁⚡ Init success! HighScore={m_CloudPlayerData.HighScore} LastSeed={m_CloudPlayerData.LastSeedUsed}");
                    return true;
                }
                catch (CloudCodeException ex)
                {
                    if (ex.Message.Contains("Authentication") || ex.Message.Contains("Unauthorized"))
                    {
                        Logger.LogError($"Non-retryable CloudCode error: {ex.Message}");
                        return false;
                    }
                    Logger.LogDemo($"CloudCode error attempt {attempt + 1}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Logger.LogDemo($"General error attempt {attempt + 1}: {ex.Message}");
                }

                if (attempt < k_MaxInitRetries - 1)
                    await Task.Delay(k_RetryDelayMs);
            }

            return false;
        }

        // -----------------------------------------------------------------------
        // Score submission
        // -----------------------------------------------------------------------

        /// <summary>
        /// Called when a match ends. Submits score + seed to cloud if it is a new personal best.
        /// </summary>
        async void HandleMatchComplete(int score, int seed)
        {
            if (m_GameManagerUGS.IsOfflineMode)
            {
                Logger.LogDemo("Cloud score submission skipped (Offline Mode).");
                return;
            }

            if (!IsPlayerInitializedInCloud)
            {
                Logger.LogWarning("MatchComplete ignored — player not yet initialized in cloud.");
                return;
            }

            try
            {
                Logger.LogDemo($"☁ Submitting score {score} for seed {seed}...");
                var updated = await m_BindingsProvider.SuikaGameBindings.SubmitScore(score, seed);
                if (updated != null)
                {
                    m_CloudPlayerData = updated;
                    Logger.LogDemo($"☁⚡ Score submitted. Cloud HighScore={m_CloudPlayerData.HighScore}");
                    PlayerDataUpdated?.Invoke(m_CloudPlayerData);
                }
            }
            catch (CloudCodeException ex)
            {
                Logger.LogError($"CloudCode error submitting score: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error submitting score: {ex.Message}");
            }
        }

        // -----------------------------------------------------------------------
        // Misc public API
        // -----------------------------------------------------------------------

        public void UpdateDisplayName(string displayName)
        {
            if (m_CloudPlayerData == null) return;
            m_CloudPlayerData.DisplayName = displayName;
            PlayerDataUpdated?.Invoke(m_CloudPlayerData);
        }

        public void HandleCloudDataUpdate(PlayerData updatedPlayerData)
        {
            m_CloudPlayerData = updatedPlayerData;
            PlayerDataUpdated?.Invoke(m_CloudPlayerData);
        }

        // -----------------------------------------------------------------------
        // Connectivity recovery
        // -----------------------------------------------------------------------

        async void HandleConnectivityChanged(bool isOnline)
        {
            await Task.Delay(100);
            if (m_GameManagerUGS.IsOfflineMode || !isOnline || !IsPlayerInitializedInCloud || !AuthenticationService.Instance.IsSignedIn || m_CloudPlayerData == null)
            {
                Logger.LogDemo($"Skipping connectivity sync: OfflineMode={m_GameManagerUGS.IsOfflineMode}, Online={isOnline}, Initialized={IsPlayerInitializedInCloud}");
                return;
            }

            try
            {
                Logger.LogDemo("Syncing player data after reconnect...");
                var playerData = await m_BindingsProvider.SuikaGameBindings.GetPlayerData();
                if (playerData != null)
                {
                    m_CloudPlayerData = playerData;
                    PlayerDataUpdated?.Invoke(m_CloudPlayerData);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error syncing after reconnect: {ex.Message}");
            }
        }

        // -----------------------------------------------------------------------
        // Dispose
        // -----------------------------------------------------------------------

        public void Dispose()
        {
            m_GameManagerUGS.MatchComplete -= HandleMatchComplete;
            m_AuthenticationManager.SignedIn -= HandleSignInInitialization;
            m_NetworkHandler.OnlineStatusChanged -= HandleConnectivityChanged;
            m_CloudPlayerData = null;
        }
    }
}
