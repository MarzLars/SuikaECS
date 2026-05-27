using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Suika.Scripts.PlayerDataManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suika.Scripts.Core
{
    /// <summary>
    /// Core initialization class. Bootstraps Unity Gaming Services, authentication,
    /// player data, and the async leaderboard/seed systems for the Suika merge game.
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Tooltip("Dev: clears session token and local data on start (auto-disables on play-stop).")]
        [SerializeField] bool m_DeleteAccountOnStart;
        [SerializeField] VisualTreeAsset m_BootTitleTemplate;
        [SerializeField] PanelSettings m_BootTitlePanelSettings;

        const string k_Environment = "production";

        GameManagerUGS m_GameManagerUGS;
        NetworkConnectivityHandler m_NetworkConnectivityHandler;
        PlayerDataManager m_PlayerDataManager;
        PlayerDataManagerClient m_PlayerDataManagerClient;
        CloudBindingsProvider m_BindingsProvider;
        bool m_IsInitBootScene;
        bool m_ShowBootTitle = true;
        UIDocument m_BootTitleDocument;
        VisualElement m_BootTitleRoot;
        Label m_BootTitleLabel;
        Label m_BootSubtitleLabel;
        const float k_BootTitleDuration = 1.25f;
        static GameInitializer s_Instance;

        async void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCoreComponents();
            m_IsInitBootScene = SceneManager.GetActiveScene().name == "InitBoot";
            if (m_IsInitBootScene)
                InitializeBootTitleUI();

            try
            {
                await InitializeUnityServices();

                if (m_DeleteAccountOnStart)
                    await DeleteAccountAsync();
                
                if (m_IsInitBootScene)
                {
                    StartCoroutine(InitializeAfterBootTitle());
                }
                else
                {
                    InitializeCoreGameSystems();
                }
            }
            catch (Exception e)
            {
                Utilities.Logger.LogException(e);
            }
        }

        void Start()
        {
        }

        IEnumerator InitializeAfterBootTitle()
        {
            yield return new WaitForSeconds(k_BootTitleDuration);
            m_ShowBootTitle = false;
            if (m_BootTitleRoot != null)
                m_BootTitleRoot.style.display = DisplayStyle.None;
            InitializeCoreGameSystems();
        }

        void InitializeBootTitleUI()
        {
            if (m_BootTitleTemplate == null)
            {
                Utilities.Logger.LogError("Boot title UXML not assigned on GameInitializer.");
                return;
            }

            if (m_BootTitlePanelSettings == null)
            {
                Utilities.Logger.LogError("Boot title PanelSettings not assigned on GameInitializer.");
                return;
            }

            m_BootTitleDocument = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            m_BootTitleDocument.panelSettings = m_BootTitlePanelSettings;

            m_BootTitleRoot = m_BootTitleDocument.rootVisualElement;
            if (m_BootTitleRoot == null)
            {
                Utilities.Logger.LogError("Boot title UIDocument rootVisualElement not available.");
                return;
            }

            m_BootTitleRoot.Clear();
            m_BootTitleTemplate.CloneTree(m_BootTitleRoot);

            m_BootTitleLabel = m_BootTitleRoot.Q<Label>("BootTitleLabel");
            m_BootSubtitleLabel = m_BootTitleRoot.Q<Label>("BootSubtitleLabel");

            ApplyBootTitleTypography();
            m_BootTitleRoot.style.display = DisplayStyle.Flex;
        }

        void ApplyBootTitleTypography()
        {
            if (m_BootTitleLabel != null)
                m_BootTitleLabel.style.fontSize = Mathf.Max(28, Screen.height / 18);

            if (m_BootSubtitleLabel != null)
                m_BootSubtitleLabel.style.fontSize = Mathf.Max(12, Screen.height / 42);
        }

        void EnsureCoreComponents()
        {
            if (!TryGetComponent(out GameSystemLocator _))
                gameObject.AddComponent<GameSystemLocator>();

            m_GameManagerUGS = GetComponent<GameManagerUGS>() ?? gameObject.AddComponent<GameManagerUGS>();
            m_NetworkConnectivityHandler = GetComponent<NetworkConnectivityHandler>() ?? gameObject.AddComponent<NetworkConnectivityHandler>();
        }

        async Task InitializeUnityServices()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Initialized)
                {
                    Utilities.Logger.Log("Unity Gaming Services already initialized.");
                    return;
                }

                var options = new InitializationOptions();
#if !UNITY_EDITOR
                options.SetEnvironmentName(k_Environment);
#endif
                await UnityServices.InitializeAsync(options);
                Utilities.Logger.Log("✅ Unity Gaming Services initialized successfully.");
            }
            catch (Exception e)
            {
                Utilities.Logger.LogError($"Error initializing Unity Services: {e.Message}");
                Utilities.Logger.Log("Proceeding with offline mode...");
                if (m_GameManagerUGS != null)
                {
                    m_GameManagerUGS.ForceOfflineMode();
                }
            }
        }

        /// <summary>
        /// Creates and wires all core game systems, then registers them with GameSystemLocator.
        ///
        /// Architecture:
        ///   - Managers hold local state; *Client classes talk to Cloud Code.
        ///   - SeedManager generates deterministic per-match seeds.
        ///   - LeaderboardManager handles async score submission and leaderboard queries.
        /// </summary>
        void InitializeCoreGameSystems()
        {
            m_BindingsProvider = new CloudBindingsProvider();
            var authenticationManager = new PlayerAuthenticationManager();
            var localStorageSystem = new LocalStorageSystem();
            var profilePictures = ScriptableObject.CreateInstance<RandomProfilePicturesSO>();

            m_PlayerDataManager = new PlayerDataManager(m_GameManagerUGS, localStorageSystem, profilePictures);
            m_PlayerDataManagerClient = new PlayerDataManagerClient(m_GameManagerUGS, authenticationManager, m_BindingsProvider, m_NetworkConnectivityHandler);

            var seedManager = new SeedManager(localStorageSystem);
            var leaderboardManager = new LeaderboardManager(m_GameManagerUGS, seedManager);

            RegisterCoreGameSystems(authenticationManager, seedManager, leaderboardManager);

            m_GameManagerUGS.Initialize(authenticationManager, new SceneLoader());
            m_PlayerDataManager.Initialize(m_PlayerDataManagerClient);
        }

        void RegisterCoreGameSystems(
            PlayerAuthenticationManager authenticationManager,
            SeedManager seedManager,
            LeaderboardManager leaderboardManager)
        {
            GameSystemLocator.Register<CloudBindingsProvider>(m_BindingsProvider);
            GameSystemLocator.Register<PlayerAuthenticationManager>(authenticationManager);
            GameSystemLocator.Register<GameManagerUGS>(m_GameManagerUGS);
            GameSystemLocator.Register<NetworkConnectivityHandler>(m_NetworkConnectivityHandler);
            GameSystemLocator.Register<PlayerDataManager>(m_PlayerDataManager);
            GameSystemLocator.Register<PlayerDataManagerClient>(m_PlayerDataManagerClient);
            GameSystemLocator.Register<SeedManager>(seedManager);
            GameSystemLocator.Register<LeaderboardManager>(leaderboardManager);
        }

        /// <summary>
        /// Dev utility: wipes local data and session token so the new-player flow can be tested.
        /// Cloud Save data must still be manually cleared via the Unity Dashboard.
        /// </summary>
        async Task DeleteAccountAsync()
        {
            try
            {
                Utilities.Logger.LogWarning("GameInitializer: DeleteAccountOnStart is ON — disable before shipping!");
                if (AuthenticationService.Instance.SessionTokenExists)
                {
                    Utilities.Logger.Log("Clearing session token...");
                    AuthenticationService.Instance.ClearSessionToken();
                }
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    Utilities.Logger.Log("Deleting account...");
                    await AuthenticationService.Instance.DeleteAccountAsync();
                }
                Utilities.Logger.Log("Deleting local data...");
                new LocalStorageSystem().DeleteLocalData();
            }
            catch (Exception e)
            {
                Utilities.Logger.LogException(e);
            }
        }

        void OnDisable()
        {
            if (m_DeleteAccountOnStart)
                Utilities.Logger.LogWarning("🟡 REMINDER: 'Delete Account On Start' is still ON.");
        }

    }
}
