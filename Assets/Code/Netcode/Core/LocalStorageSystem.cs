using System;
using System.IO;
using Newtonsoft.Json;
using Unity.Services.CloudCode.GeneratedBindings.SuikaUGSCloud.Models;
using UnityEngine;
using Suika.Scripts.Utilities;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.Core
{
    /// <summary>
    /// Handles local persistence of player data and profile pictures.
    /// Provides methods to save and load JSON files to/from the application's persistent data path,
    /// with error handling and logging for debugging.
    /// </summary>
    public class LocalStorageSystem
    {
        const string k_PlayerDataFileName = "player_data.json";
        const string k_ProfilePictureFileName = "profile_picture.json";
        const string k_SaveEmoji = "💾";
        
        /// <summary>
        /// Loads the player's saved data from local storage.
        /// Returns null if no data exists or if loading fails.
        /// </summary>
        public PlayerData LoadPlayerData()
        {
            string path = Path.Combine(Application.persistentDataPath, k_PlayerDataFileName);
            if (!File.Exists(path))
            {
                return null;
            }
            try
            {
                string jsonData = File.ReadAllText(path);
                var playerData = JsonConvert.DeserializeObject<PlayerData>(jsonData);
                Utilities.Logger.LogDemo($"{k_SaveEmoji} Loaded player data for player: {playerData?.DisplayName}");
                
                return playerData;
            }
            catch (Exception e)
            {
                Utilities.Logger.LogError($"Error loading local player data: {e.Message}");
                return null;
            }
        }
        
        public ProfilePicture LoadProfilePicture()
        {
            string path = Path.Combine(Application.persistentDataPath, k_ProfilePictureFileName);
            if (File.Exists(path))
            {
                try
                {
                    string jsonData = File.ReadAllText(path);
                    var profilePicture = JsonConvert.DeserializeObject<ProfilePicture>(jsonData);
                    Utilities.Logger.LogDemo($"{k_SaveEmoji} Loaded profile picture of type: {profilePicture?.Type}");
                    
                    return profilePicture;
                }
                catch (Exception e)
                {
                    Utilities.Logger.LogError($"Error loading local profile picture: {e.Message}");
                }
            }
            
            return null;
        }
        
        public void SavePlayerData(PlayerData playerData)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(playerData);
                string path = Path.Combine(Application.persistentDataPath, k_PlayerDataFileName);
                File.WriteAllText(path, jsonData);
                
                Utilities.Logger.LogDemo($"{k_SaveEmoji} PlayerData saved locally for player: {playerData.DisplayName}");
            }
            catch (Exception e)
            {
                Utilities.Logger.LogError($"Error saving player data locally: {e.Message}");
            }
        }
        
        public void SaveProfilePicture(ProfilePicture profilePicture)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(profilePicture);
                string path = Path.Combine(Application.persistentDataPath, k_ProfilePictureFileName);
                File.WriteAllText(path, jsonData);
                
                Utilities.Logger.LogDemo($"{k_SaveEmoji} Profile picture saved locally of type: {profilePicture.Type}");
            }
            catch (Exception e)
            {
                Utilities.Logger.LogError($"Error saving profile picture locally: {e.Message}");
            }
        }
        
        public void DeleteLocalData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            DeleteFile(k_PlayerDataFileName);
            DeleteFile(k_ProfilePictureFileName);
            
            Utilities.Logger.LogDemo("All local data deleted");
        }

        void DeleteFile(string fileName)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(path))
            {
                return;
            }
            try
            {
                File.Delete(path);
                
                Utilities.Logger.LogVerbose($"Deleted local file: {fileName}");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error deleting {fileName}: {e.Message}");
            }
        }
    }
}
