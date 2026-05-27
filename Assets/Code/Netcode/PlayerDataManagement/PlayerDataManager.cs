using System;
using Suika.Scripts.Core;
using Suika.Scripts.Utilities;
using Unity.Services.Authentication;
using Unity.Services.CloudCode.GeneratedBindings.SuikaUGSCloud.Models;
using UnityEngine;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.PlayerDataManagement
{
    /// <summary>
    /// Core manager for player data — local persistence and cloud sync.
    /// Simplified for the async Suika leaderboard game:
    ///   - Tracks DisplayName, HighScore, LastSeedUsed
    ///   - Removes hearts/stars/gifts/area/economy systems
    /// </summary>
    public class PlayerDataManager : IDisposable
    {
        public RandomProfilePicturesSO RandomProfilePicturesSO { get; private set; }
        public ProfilePicture ProfilePictureData { get; private set; }
        public Sprite ProfileSprite { get; private set; }
        public PlayerData PlayerDataLocal { get; private set; }
        public PlayerEconomyData PlayerEconomyDataLocal { get; private set; }
        public string PlayerId { get; private set; }
        public bool IsCloudDataInitialized { get; private set; }

        readonly GameManagerUGS m_GameManagerUGS;
        readonly LocalStorageSystem m_LocalStorageSystem;
        PlayerDataManagerClient m_PlayerDataManagerClient;

        const string k_EventEmoji = "⚡";

        public event Action<PlayerData> LocalPlayerDataUpdated;
        public event Action<Sprite> ProfilePictureUpdated;
        public event Action CloudDataInitialized;
        public event Action DeleteCachedData;
        public event Action NoGiftHeartsLeftPopup;

        public PlayerDataManager(GameManagerUGS gameManagerUGS, LocalStorageSystem localStorageSystem, RandomProfilePicturesSO profilePics)
        {
            m_GameManagerUGS = gameManagerUGS;
            m_LocalStorageSystem = localStorageSystem;
            RandomProfilePicturesSO = profilePics;
        }

        public void Initialize(PlayerDataManagerClient playerDataManagerClient)
        {
            m_PlayerDataManagerClient = playerDataManagerClient;
            m_PlayerDataManagerClient.ProfilePictureFetched += OverwriteProfilePicture;
            m_PlayerDataManagerClient.PlayerDataUpdated += OverwriteLocalPlayerData;
            m_PlayerDataManagerClient.PlayerInitialized += OnPlayerInitializationComplete;
            m_GameManagerUGS.MatchComplete += HandleMatchComplete;

            InitializeLocalPlayerData();
        }

        void InitializeLocalPlayerData()
        {
            PlayerDataLocal = m_LocalStorageSystem.LoadPlayerData();

            if (PlayerDataLocal == null)
            {
                Logger.LogDemo("No saved player data: creating new player.");
                PlayerDataLocal = CreateStartingPlayerData();
                SavePlayerDataLocal();
            }

            PlayerEconomyDataLocal = m_LocalStorageSystem.LoadEconomyData() ?? new PlayerEconomyData();

            ProfilePictureData = m_LocalStorageSystem.LoadProfilePicture();
            if (ProfilePictureData != null)
                SetProfileSprite();
            else
                Logger.LogDemo("Profile picture null, will initialize in cloud.");

            PlayerId = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : "offline-player";
            LocalPlayerDataUpdated?.Invoke(PlayerDataLocal);
        }

        void OnPlayerInitializationComplete()
        {
            IsCloudDataInitialized = true;
            CloudDataInitialized?.Invoke();
        }

        // -----------------------------------------------------------------------
        // Cloud data sync
        // -----------------------------------------------------------------------

        void OverwriteLocalPlayerData(PlayerData cloudData)
        {
            if (cloudData == null)
            {
                Logger.LogWarning("Received null player data from cloud.");
                return;
            }

            if (string.IsNullOrEmpty(PlayerId))
                PlayerId = AuthenticationService.Instance.PlayerId;

            PlayerDataLocal.DisplayName = cloudData.DisplayName;
            PlayerDataLocal.HighScore = cloudData.HighScore;
            PlayerDataLocal.LastSeedUsed = cloudData.LastSeedUsed;

            SavePlayerDataLocal();
            Logger.LogDemo($"{k_EventEmoji} LocalPlayerDataUpdated — HighScore:{PlayerDataLocal.HighScore}");
            LocalPlayerDataUpdated?.Invoke(PlayerDataLocal);
        }

        void HandleMatchComplete(int score, int seed)
        {
            if (PlayerDataLocal == null)
                return;

            if (score <= PlayerDataLocal.HighScore)
                return;

            PlayerDataLocal.HighScore = score;
            PlayerDataLocal.LastSeedUsed = seed;
            SavePlayerDataLocal();
            LocalPlayerDataUpdated?.Invoke(PlayerDataLocal);
            Logger.LogDemo($"{k_EventEmoji} Local high score saved — HighScore:{score} Seed:{seed}");
        }

        // -----------------------------------------------------------------------
        // Profile picture
        // -----------------------------------------------------------------------

        public void OverwriteProfilePicture(ProfilePicture profilePicture)
        {
            if (profilePicture == null)
            {
                Logger.LogWarning("Profile picture is null.");
                return;
            }
            ProfilePictureData = profilePicture;
            SetProfileSprite();
            m_LocalStorageSystem.SaveProfilePicture(ProfilePictureData);
        }

        void SetProfileSprite()
        {
            ProfileSprite = ProfilePictureData.Type switch
            {
                "pre-made" => RandomProfilePicturesSO.ProfilePictures[ProfilePictureData.ImageId],
                "custom" => ProfilePictureData.ImageData.ConvertBase64ToSprite(),
                _ => ProfileSprite
            };
            Logger.LogDemo($"{k_EventEmoji} ProfilePictureUpdated");
            ProfilePictureUpdated?.Invoke(ProfileSprite);
        }

        // -----------------------------------------------------------------------
        // Display name
        // -----------------------------------------------------------------------

        public void HandleUpdateDisplayName(string displayName)
        {
            PlayerDataLocal.DisplayName = displayName;
            LocalPlayerDataUpdated?.Invoke(PlayerDataLocal);
        }

        public void UpdateAnonymousStatus(bool isAnonymous)
        {
            _ = isAnonymous;
        }

        public bool ModifyGiftHearts(int delta)
        {
            if (PlayerDataLocal == null)
            {
                return false;
            }

            if (delta < 0 && PlayerDataLocal.GiftHearts <= 0)
            {
                NoGiftHeartsLeftPopup?.Invoke();
                return false;
            }

            PlayerDataLocal.GiftHearts = Mathf.Max(0, PlayerDataLocal.GiftHearts + delta);
            LocalPlayerDataUpdated?.Invoke(PlayerDataLocal);
            SavePlayerDataLocal();
            return true;
        }

        // -----------------------------------------------------------------------
        // Account deletion
        // -----------------------------------------------------------------------

        public void DeleteLocalPlayerData()
        {
            PlayerDataLocal = null;
            PlayerId = null;
            ProfilePictureData = null;
            IsCloudDataInitialized = false;
            DeleteCachedData?.Invoke();
            m_LocalStorageSystem.DeleteLocalData();
        }

        public bool IsPlayerAnonymous()
        {
            return AuthenticationService.Instance.IsSignedIn
                && AuthenticationService.Instance.IsAuthorized
                && AuthenticationService.Instance.PlayerInfo?.Identities != null
                && AuthenticationService.Instance.PlayerInfo.Identities.Count == 0;
        }

        // -----------------------------------------------------------------------
        // Persistence helpers
        // -----------------------------------------------------------------------

        void SavePlayerDataLocal() => m_LocalStorageSystem.SavePlayerData(PlayerDataLocal);

        static PlayerData CreateStartingPlayerData() => new()
        {
            DisplayName = "New Player",
            HighScore = 0,
            LastSeedUsed = 0
        };

        // -----------------------------------------------------------------------
        // Dispose
        // -----------------------------------------------------------------------

        public void Dispose()
        {
            m_GameManagerUGS.MatchComplete -= HandleMatchComplete;
            if (m_PlayerDataManagerClient == null) return;
            m_PlayerDataManagerClient.ProfilePictureFetched -= OverwriteProfilePicture;
            m_PlayerDataManagerClient.PlayerDataUpdated -= OverwriteLocalPlayerData;
            m_PlayerDataManagerClient.PlayerInitialized -= OnPlayerInitializationComplete;
        }
    }
}
