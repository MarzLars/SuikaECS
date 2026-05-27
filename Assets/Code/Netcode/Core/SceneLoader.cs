using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.Core
{
    /// <summary>
    /// Handles scene loading operations.
    /// Provides smooth asynchronous scene loading and proper scene initialization.
    /// </summary>
    public class SceneLoader
    {
        // Scene loading shows progress up to 90%, then smoothly completes
        const float k_LoadProgressThreshold = 0.9f;
        const float k_FinalProgressSpeed = 2f;
        
        public SceneLoader()
        {
        }
        
        public Task LoadSceneAdditive(string sceneName) => LoadScene(sceneName, LoadSceneMode.Additive);

        public async Task LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            Utilities.Logger.Log($"Loading scene: {sceneName}");

            var loadOperation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (loadOperation == null)
            {
                Logger.LogError($"Failed to load scene {sceneName}");
                return;
            }

            loadOperation.allowSceneActivation = false;
            await HandleLoadingProgress(loadOperation);
            await WaitForSceneActivation(loadOperation);
            
            // First yield ensures scene is loaded
            await Task.Yield();
            // Second yield ensures Awake/Start have completed
            await Task.Yield();
        }

        async Task HandleLoadingProgress(AsyncOperation loadOperation)
        {
            float progress = 0f;
            
            // Phase 1: Show real loading progress up to threshold
            while (loadOperation.progress < k_LoadProgressThreshold)
            {
                progress = loadOperation.progress / k_LoadProgressThreshold;
                await Task.Yield();
            }
            
            // Phase 2: Smoothly animate remaining progress
            while (progress < 1f)
            {
                progress = Mathf.MoveTowards(progress, 1f, Time.deltaTime * k_FinalProgressSpeed);
                await Task.Yield();
            }
        }

        async Task WaitForSceneActivation(AsyncOperation loadOperation)
        {
            loadOperation.allowSceneActivation = true;
            while (!loadOperation.isDone)
            {
                await Task.Yield();
            }
        }

        public async Task LoadGameLevel(int levelIndex)
        {
            string sceneName = SceneUtility.GetScenePathByBuildIndex(levelIndex);
            sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);
            await LoadScene(sceneName);
        }
    }
}
