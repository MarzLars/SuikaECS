using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.Core
{
    /// <summary>
    /// Routes app flow across InitBoot, MainLogin, and MergeGame.
    /// Emits game-level events consumed by PlayerDataManager and LeaderboardManager.
    /// </summary>
    public class GameManagerUGS : MonoBehaviour
    {
        SceneLoader m_SceneLoader;
        PlayerAuthenticationManager m_AuthenticationManager;

        public bool IsOfflineMode { get; private set; }

        readonly struct SceneInfo
        {
            public readonly string Name;
            public readonly int BuildIndex;
            public SceneInfo(string name, int buildIndex) { Name = name; BuildIndex = buildIndex; }
        }

        static readonly SceneInfo MainMenu = new("MainLogin", 1);
        static readonly SceneInfo GameScene = new("MergeGame", 2);

        /// <summary>Fired when the player finishes a run, carrying the score and seed used.</summary>
        public event Action<int, int> MatchComplete;

        bool IsCurrentScene(int buildIndex) => SceneManager.GetActiveScene().buildIndex == buildIndex;

        public void Initialize(PlayerAuthenticationManager authenticationManager, SceneLoader sceneLoader)
        {
            m_AuthenticationManager = authenticationManager;
            m_SceneLoader = sceneLoader;
            HandleStartupAuthentication();
        }

        async void HandleStartupAuthentication()
        {
            await m_AuthenticationManager.SignInCachedPlayerAsync();
            await RequestLoadMainMenu();
        }

        public async Task RequestLoadPlayerHub()
        {
            if (IsCurrentScene(GameScene.BuildIndex))
            {
                Logger.Log("MergeGame already loaded");
                return;
            }
            await m_SceneLoader.LoadScene(GameScene.Name);
        }

        public async Task RequestLoadMainMenu()
        {
            if (IsCurrentScene(MainMenu.BuildIndex))
                return;
            await m_SceneLoader.LoadScene(MainMenu.Name);
        }

        public async Task StartGameplay() => await RequestLoadPlayerHub();

        public void ForceOfflineMode() => IsOfflineMode = true;

        public void StartOfflineGameplay()
        {
            IsOfflineMode = true;
            _ = RequestLoadPlayerHub();
        }

        /// <summary>
/// Call when a match ends. Score and seed are forwarded to
        /// PlayerDataManager and LeaderboardManager via the MatchComplete event.
        /// </summary>
        public void RaiseMatchComplete(int score, int seed)
        {
            Logger.LogDemo($"⚡ MatchComplete — score:{score}  seed:{seed}");
            MatchComplete?.Invoke(score, seed);
        }
    }
}
