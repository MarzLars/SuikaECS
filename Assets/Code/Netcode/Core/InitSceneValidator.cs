using UnityEngine;
using UnityEngine.SceneManagement;
namespace Suika.Scripts.Core
{
    /// <summary>
    /// Ensures the game starts from the correct initialization scene by validating the starting scene index.
    /// If the game starts from any scene other than InitBoot:
    /// - In Editor: Pauses and stops play mode
    /// - In Build: Quits the application
    /// 
    /// This validation occurs before any scene loads using Unity's RuntimeInitializeOnLoadMethod.
    /// </summary>
    public static class InitSceneValidator
    {
        static bool s_HasInitialized = false;
        const int k_InitIndex = 0;
        const string k_ErrorMessage = "<b><size=17>🚫 Game must be started from InitBoot scene.\nCurrent scene: {0}</size></b>";
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (s_HasInitialized)
            {
                return;
            }
            
            if (SceneManager.GetActiveScene().buildIndex > k_InitIndex)
            { 
                Debug.LogError(string.Format(k_ErrorMessage, SceneManager.GetActiveScene().name));
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPaused = true;  
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
                return;
            }
            
            s_HasInitialized = true;
        }
    }
}
