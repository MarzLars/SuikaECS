using System;
using System.Collections.Generic;
using System.Linq;
using Suika.Scripts.Core;
using Suika.Scripts.PlayerDataManagement;
using Unity.Services.Authentication;
using UnityEngine;
using Logger = Suika.Scripts.Utilities.Logger;
using Player = Unity.Services.CloudCode.GeneratedBindings.SuikaUGSCloud.Models.Player;

namespace Suika.Scripts.Friends
{
    /// <summary>
    /// Manages friend relationships and player interactions in the game.
    /// Handles friend list updates, player filtering, and heart gifting functionality.
    /// </summary>
    public class FriendsManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        FriendsClient m_FriendsClient;
        [SerializeField] FriendsMenuUIController m_FriendsMenuUIController;

        PlayerDataManager m_PlayerDataManager;
        
        // Player Lists
        public List<Player> Friends { get; private set; }
        public List<Player> AllPlayers => FilteredPlayerList();
        List<Player> m_AllPlayers;
        
        public event Action<List<Player>> AllPlayersListUpdated;
        public event Action<List<Player>> FriendsListUpdated;

        void Start()
        {
            SetupEventHandlers();
            
            m_AllPlayers = new List<Player>();
            Friends = new List<Player>();
            m_PlayerDataManager = GameSystemLocator.Get<PlayerDataManager>();
        }

        void SetupEventHandlers()
        {
            m_FriendsClient.FetchedPlayerListFromCloud += UpdateAllPlayers;
            m_FriendsClient.FetchedFriendsListFromCloud += UpdateFriendsList;
            
            m_FriendsMenuUIController.FriendAdded += AddFriendLocally;
            m_FriendsMenuUIController.FriendRemoved += RemoveFriendLocal;
            m_FriendsMenuUIController.GiftingHeart += HandleGiftHeart;
        }

        void UpdateAllPlayers(List<Player> players)
        {
            if (players == null)
            {
                Utilities.Logger.LogWarning("No players found");
                return;
            }
            if (players.Count == 0) return;
            
            m_AllPlayers = players;
            var filteredList = FilteredPlayerList();
            
            AllPlayersListUpdated?.Invoke(filteredList);
        }

        void UpdateFriendsList(List<Player> friends)
        {
            Friends = friends ?? new List<Player>();
            Utilities.Logger.Log($"\u26A1 Local FriendsListUpdated with {Friends.Count} friends");
            
            // Update both lists since friend status affects filtered players
            AllPlayersListUpdated?.Invoke(FilteredPlayerList());
            FriendsListUpdated?.Invoke(Friends);
        }

        List<Player> FilteredPlayerList()
        {
            var currentPlayerId = AuthenticationService.Instance.PlayerId;
            var filteredPlayerList = m_AllPlayers
                .Where(player => 
                    !IsCurrentPlayer(player, currentPlayerId) && 
                    !IsFriend(player))
                .ToList();
            
            // Logger.Log($"Filtered all players list count: {filteredPlayerList.Count}");
            // foreach(var player in filteredPlayerList)
            // {
            //     Logger.Log($"Player in list: {player.DisplayName} ({player.PlayerId})");
            // }
            
            return filteredPlayerList;
        }

        bool IsCurrentPlayer(Player player, string currentPlayerId)
        {
            return player.PlayerId == currentPlayerId;
        }

        bool IsFriend(Player player)
        {
            if (Friends == null)
                return false;
        
            return Friends.Any(friend => friend.PlayerId == player.PlayerId);
        }

        void AddFriendLocally(Player otherPlayer)
        {
            if (Friends.Any(f => f.PlayerId == otherPlayer.PlayerId))
            {
                Utilities.Logger.LogWarning($"Player {otherPlayer.DisplayName} is already a friend");
                return;
            }
            
            Utilities.Logger.Log($"Adding friend locally: {otherPlayer.DisplayName}");
            Friends.Add(otherPlayer);
            
            // Remove from all players list
            // m_AllPlayers = m_AllPlayers.Where(p => p.PlayerId != player.PlayerId).ToList();
            
            var filteredList = FilteredPlayerList();
            Utilities.Logger.Log($"After adding friend - All players count: {m_AllPlayers.Count}, Filtered count: {filteredList.Count}");
            
            AllPlayersListUpdated?.Invoke(filteredList);
            FriendsListUpdated?.Invoke(Friends);
        }

        void RemoveFriendLocal(Player otherPlayer)
        {
            Utilities.Logger.Log("Removing friend...");
            Friends.Remove(otherPlayer);
            AllPlayersListUpdated?.Invoke(FilteredPlayerList());
            FriendsListUpdated?.Invoke(Friends);
        }

        void HandleGiftHeart(string playerId)
        {
            if (!IsGiftDataValid(playerId))
            {
                Utilities.Logger.LogWarning($"Player {playerId} is not valid for data");
                return;
            }
            
            if (m_PlayerDataManager.ModifyGiftHearts(-1))
            {
                Utilities.Logger.Log($"Local Gift Heart deducted -1 to gift playerId: {playerId} a heart");
            }
        }

        /// <summary>
        /// Validates if a player can receive a gift heart
        /// </summary>
        /// <returns>True if the player can receive a gift, false otherwise</returns>
        bool IsGiftDataValid(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                Utilities.Logger.LogWarning("Player ID is empty");
                return false;
            }

            if (m_AllPlayers.Find(p => p.PlayerId == playerId) == null)
            {
                Utilities.Logger.LogWarning($"Player {playerId} does not exist");
                return false;
            }

            if (playerId == AuthenticationService.Instance.PlayerId)
            {
                Logger.LogWarning($"Player cannot send gift to self");
                return false;
            }
            
            return true;
        }

        void OnDestroy()
        {
            if (m_FriendsClient != null)
            {
                m_FriendsClient.FetchedPlayerListFromCloud -= UpdateAllPlayers;
                m_FriendsClient.FetchedFriendsListFromCloud -= UpdateFriendsList;
            }
    
            m_AllPlayers?.Clear();
            Friends?.Clear();
        }
    }
}

