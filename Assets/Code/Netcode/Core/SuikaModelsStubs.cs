using System;

// ---------------------------------------------------------------------------
// Suika model stubs — shared data classes used by cloud bindings and managers.
// Stripped of GemHunter-era area/hearts/stars/economy/friends fields.
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
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public string DisplayName { get; set; }
        public int HighScore { get; set; }
        public int LastSeedUsed { get; set; }
    }

    [Serializable]
    public class PlayerInitializationResponse
    {
        public PlayerData PlayerData { get; set; }
        public ProfilePicture ProfilePicture { get; set; }
    }
}
