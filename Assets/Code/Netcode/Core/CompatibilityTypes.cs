using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudCode.GeneratedBindings.SuikaUGSCloud.Models;
using UnityEngine;

// ---------------------------------------------------------------------------
// Compatibility layer — namespace aliases and cloud binding stubs.
// GemHunter-era stubs (AreaManager, AreaCompleteCommand, CommandReward,
// GemHunterUGS.* namespaces) have been removed.
// ---------------------------------------------------------------------------

namespace GemHunterUGS.Scripts.Core { }
namespace GemHunterUGS.Scripts.Friends { }
namespace GemHunterUGS.Scripts.PlayerDataManagement { }
namespace Suika.Scripts.AreaUpgradables { }
namespace Suika.Scripts.PlayerEconomyManagement { }

namespace Suika.Scripts.Utilities
{
    public static class Base64TextureExtensions
    {
        public static Texture2D ConvertBase64ToTexture2D(this string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return null;
            try
            {
                var bytes = Convert.FromBase64String(base64);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                return texture.LoadImage(bytes) ? texture : null;
            }
            catch
            {
                return null;
            }
        }

        public static Sprite ConvertBase64ToSprite(this string base64)
        {
            var texture = base64.ConvertBase64ToTexture2D();
            if (texture == null)
                return null;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}

namespace Unity.Services.Authentication
{
    public static class AuthenticationPlayerInfoExtensions
    {
        static string GetIdentityId(PlayerInfo playerInfo, params string[] typeIds)
        {
            if (playerInfo?.Identities == null)
                return null;
            return playerInfo.Identities
                .FirstOrDefault(identity => typeIds.Any(typeId => string.Equals(identity.TypeId, typeId, StringComparison.OrdinalIgnoreCase)))
                ?.TypeId;
        }

        public static string GetUnityId(this PlayerInfo playerInfo) => GetIdentityId(playerInfo, "unity", "unityplayeraccount");
        public static string GetGooglePlayGamesId(this PlayerInfo playerInfo) => GetIdentityId(playerInfo, "googleplaygames", "google-play-games");
    }
}

namespace Suika.Scripts.PlayerDataManagement
{
    public class RandomProfilePicturesSO : ScriptableObject
    {
        public Sprite[] ProfilePictures = Array.Empty<Sprite>();
    }
}

namespace Suika.Scripts.Core
{
    /// <summary>
    /// Stub for Suika cloud game bindings.
    /// In production these are replaced by generated Cloud Code bindings.
    /// Only methods relevant to the async leaderboard game are retained.
    /// </summary>
    public sealed class SuikaGameBindings
    {
        readonly PlayerData m_PlayerData = new()
        {
            DisplayName = "New Player",
            HighScore = 0,
            LastSeedUsed = 0
        };

        readonly ProfilePicture m_ProfilePicture = new();

        public Task<PlayerInitializationResponse> OnSignInHandlePlayerInitialization()
        {
            return Task.FromResult(new PlayerInitializationResponse
            {
                PlayerData = Clone(m_PlayerData),
                ProfilePicture = Clone(m_ProfilePicture)
            });
        }

        /// <summary>
        /// Submits a score to the cloud for the given seed.
        /// Only replaces the stored high-score if the new score is better.
        /// </summary>
        public Task<PlayerData> SubmitScore(int score, int seed)
        {
            if (score > m_PlayerData.HighScore)
            {
                m_PlayerData.HighScore = score;
                m_PlayerData.LastSeedUsed = seed;
            }
            return Task.FromResult(Clone(m_PlayerData));
        }

        public Task<PlayerData> GetPlayerData() => Task.FromResult(Clone(m_PlayerData));

        static PlayerData Clone(PlayerData src) => new()
        {
            DisplayName = src.DisplayName,
            HighScore = src.HighScore,
            LastSeedUsed = src.LastSeedUsed
        };

        static ProfilePicture Clone(ProfilePicture src) => new()
        {
            Type = src.Type,
            ImageId = src.ImageId,
            ImageData = src.ImageData
        };
    }

    // Minimal HubUIController stub referenced by AccountManagementUIController
    public class HubUIController : UnityEngine.MonoBehaviour
    {
        public void ShowMainHub() { }
        public void HandleToggleAccountManagementMenu() { }
    }
}
