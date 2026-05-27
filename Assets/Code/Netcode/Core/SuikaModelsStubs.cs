using System;
using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// Suika model stubs — shared data classes used by cloud bindings and managers.
// Stripped of GemHunter-era area/hearts/stars/economy fields.
// ---------------------------------------------------------------------------

namespace Unity.Services.CloudCode.GeneratedBindings.SuikaUGSCloud.Models
{
    [Serializable]
    public class ProfilePicture
    {
        public string Type { get; set; }
        public int ImageId { get; set; }
        public string ImageData { get; set; }
    }

    /// <summary>
    /// Core player data stored locally and synced with cloud.
    /// For the Suika async leaderboard game:
    ///   - HighScore    : best score ever achieved by this player
    ///   - LastSeedUsed : seed value of the run that produced HighScore
    ///                    (also used to submit that run to the leaderboard)
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public string DisplayName { get; set; }
        public int HighScore { get; set; }
        public int LastSeedUsed { get; set; }
        public int Hearts { get; set; }
        public int GiftHearts { get; set; }
    }

    /// <summary>
    /// Minimal local economy model used by UI binding and save/load code.
    /// </summary>
    [Serializable]
    public class PlayerEconomyData
    {
        public List<int> Currencies { get; set; } = new();
        public List<string> ItemInventory { get; set; } = new();
    }

    [Serializable]
    public class PlayerInitializationResponse
    {
        public PlayerData PlayerData { get; set; }
        public ProfilePicture ProfilePicture { get; set; }
    }

    /// <summary>
    /// A player reference used by the friends system.
    /// </summary>
    [Serializable]
    public class Player
    {
        public string PlayerId { get; set; }
        public string DisplayName { get; set; }
        public ProfilePicture PlayerPortrait { get; set; } = new();
    }
}

namespace Suika.Scripts.Core
{
    /// <summary>
    /// Minimal command base type kept so local command save/load code compiles.
    /// </summary>
    [Serializable]
    public class Command { }

    public static class CustomPortraitTextureCache
    {
        public static Texture2D GetTexture(string base64) => null;
        public static void Clear() { }
    }

    // Minimal HubUIController stub referenced by FriendsMenuUIController
    public class HubUIController : UnityEngine.MonoBehaviour
    {
        public void ShowMainHub() { }
        public void HandleToggleAccountManagementMenu() { }
    }
}
