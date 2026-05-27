using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Suika.Scripts.Core;
using Suika.Scripts.PlayerDataManagement;
using Unity.Services.Authentication;
using Unity.Services.CloudCode.GeneratedBindings.SuikaUGSCloud.Models;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Models;
using UnityEngine;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.Friends
{
    /// <summary>
    /// Handles Unity Friends SDK communication for friend list, friend requests,
    /// and add/remove friend operations.
    /// </summary>
    public class FriendsClient : MonoBehaviour
    {
        [SerializeField] FriendsMenuUIController m_FriendsMenuUIController;

        PlayerDataManagerClient m_PlayerDataManagerClient;

        bool m_SubscribedToInitialization;
        bool m_IsFriendsServiceInitialized;

        public event Action<List<Player>> FetchedPlayerListFromCloud;
        public event Action<List<Player>> FetchedFriendsListFromCloud;
        public event Action FriendsMenuDataInitialized;
        public event Action HeartGiftGiven;

        void Start()
        {
            InitializeDependencies();
            SetupEventHandlers();

            if (m_PlayerDataManagerClient.IsPlayerInitializedInCloud)
                StartCoroutine(DelayedInitialization());
            else
            {
                m_PlayerDataManagerClient.PlayerInitialized += InitializeFriendsMenuData;
                m_SubscribedToInitialization = true;
            }
        }

        IEnumerator DelayedInitialization()
        {
            yield return new WaitForEndOfFrame();
            InitializeFriendsMenuData();
        }

        void InitializeDependencies()
        {
            m_PlayerDataManagerClient = GameSystemLocator.Get<PlayerDataManagerClient>();

            if (m_FriendsMenuUIController == null)
                m_FriendsMenuUIController = GetComponent<FriendsMenuUIController>();
        }

        void SetupEventHandlers()
        {
            if (m_FriendsMenuUIController == null)
            {
                Logger.LogWarning("FriendsMenuUIController is null; skipping event subscription.");
                return;
            }

            m_FriendsMenuUIController.FriendAdded += AddFriendByPlayer;
            m_FriendsMenuUIController.FriendRemoved += RemoveFriend;
            m_FriendsMenuUIController.AddFriendByTag += AddFriendByUsernameTag;
        }

        void InitializeFriendsMenuData()
        {
            GetFriends();
            FriendsMenuDataInitialized?.Invoke();
        }

        async void GetFriends()
        {
            try
            {
                await EnsureFriendsServiceInitializedAsync();

                Logger.LogDemo("Fetching friends list...");
                await FriendsService.Instance.ForceRelationshipsRefreshAsync();
                var friends = FriendsService.Instance.Friends.Select(ToPlayer).ToList();
                Logger.LogDemo($"Fetched {friends.Count} friends.");

                FetchedPlayerListFromCloud?.Invoke(new List<Player>());
                FetchedFriendsListFromCloud?.Invoke(friends);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }
        }

        async void AddFriendByPlayer(Player player)
        {
            try
            {
                await EnsureFriendsServiceInitializedAsync();
                await FriendsService.Instance.AddFriendAsync(player.PlayerId);
                await RefreshAndPublishFriends();
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to add friend: {e.Message}");
            }
        }

        /// <summary>
        /// Sends a friend request by Unity player name.
        /// </summary>
        public async void AddFriendByUsernameTag(string usernameTag)
        {
            if (string.IsNullOrWhiteSpace(usernameTag))
            {
                Logger.LogWarning("AddFriendByUsernameTag: empty input.");
                return;
            }

            try
            {
                await EnsureFriendsServiceInitializedAsync();
                Logger.LogDemo($"Searching for player: {usernameTag}");
                await FriendsService.Instance.AddFriendByNameAsync(usernameTag);
                Logger.LogDemo($"Friend request sent to {usernameTag}.");
                await RefreshAndPublishFriends();
            }
            catch (FriendsServiceException ex)
            {
                Logger.LogError($"FriendsService error adding {usernameTag}: {ex.Message}");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to add friend by tag: {e.Message}");
            }
        }

        async void RemoveFriend(Player player)
        {
            try
            {
                await EnsureFriendsServiceInitializedAsync();
                await FriendsService.Instance.DeleteFriendAsync(player.PlayerId);
                await RefreshAndPublishFriends();
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to remove friend: {e.Message}");
            }
        }

        public void RefreshFriendsList() => GetFriends();

        async Task RefreshAndPublishFriends()
        {
            await FriendsService.Instance.ForceRelationshipsRefreshAsync();
            var updated = FriendsService.Instance.Friends.Select(ToPlayer).ToList();
            HeartGiftGiven?.Invoke();
            FetchedPlayerListFromCloud?.Invoke(new List<Player>());
            FetchedFriendsListFromCloud?.Invoke(updated);
        }

        async Task EnsureFriendsServiceInitializedAsync()
        {
            if (m_IsFriendsServiceInitialized)
                return;

            if (!AuthenticationService.Instance.IsSignedIn
                || AuthenticationService.Instance.PlayerInfo?.Identities == null
                || AuthenticationService.Instance.PlayerInfo.Identities.Count == 0)
                throw new InvalidOperationException("Friends service requires a linked Unity account sign-in first.");

            await FriendsService.Instance.InitializeAsync();
            m_IsFriendsServiceInitialized = true;
        }

        static Player ToPlayer(Relationship relationship)
        {
            var member = relationship.Member;
            return new Player
            {
                PlayerId = member.Id,
                DisplayName = string.IsNullOrWhiteSpace(member.Profile?.Name) ? member.Id : member.Profile.Name,
                PlayerPortrait = new ProfilePicture
                {
                    Type = "pre-made",
                    ImageId = 0
                }
            };
        }

        void OnDisable()
        {
            if (m_PlayerDataManagerClient != null && m_SubscribedToInitialization)
                m_PlayerDataManagerClient.PlayerInitialized -= InitializeFriendsMenuData;

            if (m_FriendsMenuUIController != null)
            {
                m_FriendsMenuUIController.FriendAdded -= AddFriendByPlayer;
                m_FriendsMenuUIController.FriendRemoved -= RemoveFriend;
                m_FriendsMenuUIController.AddFriendByTag -= AddFriendByUsernameTag;
            }
        }
    }
}
