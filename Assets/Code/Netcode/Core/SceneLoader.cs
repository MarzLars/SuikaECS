using System.Threading.Tasks;
using Suika.Scripts.LoadingScreen;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.Core
{
    /// <summary>
    /// Handles scene loading operations with loading screen management.
    /// Provides smooth progress updates and ensures proper scene initialization
    /// through controlled loading phases and scene activation.
    /// 
    /// Key features:
    /// - Loading screen integration
    /// - Progress bar smoothing
    /// - Support for single and additive scene loading
    /// - Safe scene activation with initialization checks
    /// </summary>
    public class SceneLoader
    {
        readonly LoadingScreenUIController m_LoadingScreenController;
        
        // Scene loading shows real progress up to 90%, then smoothly animates the final 10%
        const float k_LoadProgressThreshold = 0.9f;
        const float k_FinalProgressSpeed = 2f;
        
        public SceneLoader(LoadingScreenUIController loadingScreenController)
        {
            m_LoadingScreenController = loadingScreenController;
        }
        
        public Task LoadSceneAdditive(string sceneName) => LoadScene(sceneName, LoadSceneMode.Additive);

        public async Task LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            Utilities.Logger.Log($"Loading scene: {sceneName}");
            if (m_LoadingScreenController != null)
                m_LoadingScreenController.HandleSceneLoading();

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
            
            if (m_LoadingScreenController != null)
                m_LoadingScreenController.HideLoadingScreen();
        }

        async Task HandleLoadingProgress(AsyncOperation loadOperation)
        {
            float progress = 0f;
            
            // Phase 1: Show real loading progress up to threshold
            while (loadOperation.progress < k_LoadProgressThreshold)
            {
                progress = loadOperation.progress / k_LoadProgressThreshold;
                if (m_LoadingScreenController != null)
                    m_LoadingScreenController.UpdateLoadingProgress(progress);
                await Task.Yield();
            }
            
            // Phase 2: Smoothly animate remaining progress
            while (progress < 1f)
            {
                progress = Mathf.MoveTowards(progress, 1f, Time.deltaTime * k_FinalProgressSpeed);
                if (m_LoadingScreenController != null)
                    m_LoadingScreenController.UpdateLoadingProgress(progress);
                await Task.Yield();
            }
            
            if (m_LoadingScreenController != null)
                m_LoadingScreenController.UpdateLoadingProgress(1f);
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

