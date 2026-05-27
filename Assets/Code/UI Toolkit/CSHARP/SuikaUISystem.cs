using Code.InputHandling;
using SuikaScripts;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using Suika.Scripts.Core;
using System;

namespace Suika.UI
{
/*
     * MOBILE CONSIDERATIONS FOR UI TOOLKIT (Unity 6 / ECS):
     * 1. Resolution & Scaling:
     *    - Use PanelSettings with 'Scale With Screen Size'.
     *    - Reference Resolution: 1080x1920 (Portrait) or 1920x1080 (Landscape).
     * 2. Touch Targets:
     *    - Minimum size of 44-48px for interactive elements.
     *    - Use 'suika-button' class with 100px height for comfortable touch on high-DPI screens.
     * 3. Performance:
     *    - Use UQuery (RootElement.Q<T>) to minimize allocations and lookups.
     *    - Minimize deep VisualElement trees to reduce layout calculation overhead.
     * 4. Safe Areas:
     *    - Consider notches and home bars. Use Screen.safeArea in a separate helper to apply padding to the root container.
     * 5. Colors & Gamma:
     *    - Unity 6 adds 'Force Gamma Rendering' in Linear projects for UI Toolkit. This ensures colors look correct on mobile displays.
     */

    // ScriptableObject approach for managing UI root and screen instantiation
    public sealed class SuikaPanelRoot : MonoBehaviour
    {
        PanelRenderer _panelRenderer;
        public VisualElement RootElement { get; private set; }
        public VisualTreeAsset LeaderboardEntryTemplate;

        void OnEnable() {
            _panelRenderer = GetComponent<PanelRenderer>();
            if (_panelRenderer != null) _panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable() {
            if (_panelRenderer != null) _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            RootElement = null;
        }

        void OnUIReload(PanelRenderer renderer, VisualElement rootElement) {
            _panelRenderer = renderer;
            RootElement = rootElement;
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct SuikaUISystem : ISystem
    {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<BeginPresentationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SuikaGameState>();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            // Read game state as a value to avoid holding a RefRW across structural changes
            var gameStateRO = SystemAPI.GetSingleton<SuikaGameState>();

            if (!TryGetRoot(out var root))
                return;

            bool hasScreens = SystemAPI.TryGetSingleton<UIScreens>(out var screensData);
            if (gameStateRO.State == GameState.Init || !hasScreens || !ScreensMatchRoot(screensData, root)) {
                DestroyScreens(ref state);
                screensData = CreateScreens(ref state, root);
                // Acquire RW after structural changes
                if (SystemAPI.TryGetSingletonRW<SuikaGameState>(out var gameState))
                    gameState.ValueRW.State = GameState.Start;
                screensData.StartScreen.Value.Show();
                return;
            }
            
            // Check authentication for social buttons
            if (GameSystemLocator.IsInitialized) {
                var authManager = GameSystemLocator.Get<PlayerAuthenticationManager>();
                if (authManager != null) {
                    bool shouldEnable = authManager.IsSignedInWithUnityAccount;
#if UNITY_EDITOR
                    shouldEnable = true; // Always enable in editor for verification
#endif
                    screensData.StartScreen.Value.SetSocialButtonsEnabled(shouldEnable);
                    screensData.SettingsScreen.Value.SetSocialButtonsEnabled(shouldEnable);
                }
            }

            // 1. Process Score Events
var totalGain = 0;
            foreach (var (scoreEvent, entity) in SystemAPI.Query<ScoreEvent>().WithEntityAccess()) {
                totalGain += scoreEvent.Amount;
                ecb.DestroyEntity(entity);
            }

            if (totalGain > 0 && SystemAPI.TryGetSingletonRW<SuikaScore>(out var score))
                score.ValueRW.Value += totalGain;

            // Handle Click Events
            foreach (var (_, entity) in SystemAPI.Query<PlayClickEvent>().WithEntityAccess()) {
                if (SystemAPI.TryGetSingletonRW<SuikaGameState>(out var gameState))
                    gameState.ValueRW.State = GameState.Playing;
                screensData.StartScreen.Value.Hide();
                screensData.HUDScreen.Value.Show();
                ecb.DestroyEntity(entity);

                // Initialize opponent score (autoplay)
                UpdateOpponentScoreFromAutoplay(screensData.HUDScreen.Value);
            }

            foreach (var (_, entity) in SystemAPI.Query<GameOverEvent>().WithEntityAccess()) {
                if (SystemAPI.TryGetSingletonRW<SuikaGameState>(out var gameState))
                    gameState.ValueRW.State = GameState.GameOver;
                screensData.StartScreen.Value.Hide();
                screensData.HUDScreen.Value.Hide();
                screensData.SettingsScreen.Value.Hide();
                screensData.GameOverScreen.Value.Show();

                int finalScore = 0;
                if (SystemAPI.TryGetSingleton<SuikaScore>(out var gameOverScore)) {
                    finalScore = gameOverScore.Value;
                    screensData.GameOverScreen.Value.SetFinalScore(finalScore);
                }

                SubmitScoreToLeaderboard(finalScore);

                ecb.DestroyEntity(entity);
            }

            foreach (var (openSettingsEvent, entity) in SystemAPI.Query<OpenSettingsEvent>().WithEntityAccess()) {
                screensData.StartScreen.Value.Hide();
                screensData.HUDScreen.Value.Hide();
                screensData.GameOverScreen.Value.Hide();

                uint seed = 0;
                var aimMoveSpeed = 0f;
                var aimMinX = 0f;
                var aimMaxX = 0f;

                if (SystemAPI.TryGetSingleton<SuikaGameConfig>(out var gameConfig)) {
                    seed = gameConfig.Seed;

                    foreach (var aimConfig in SystemAPI.Query<RefRO<DropperAimConfig>>()) {
                        aimMoveSpeed = aimConfig.ValueRO.MoveSpeed;
                        aimMinX = aimConfig.ValueRO.MinX;
                        aimMaxX = aimConfig.ValueRO.MaxX;
                        break;
                    }
                }

                screensData.SettingsScreen.Value.SetReturnToState(openSettingsEvent.ReturnToState);
                screensData.SettingsScreen.Value.SetValues(
                    seed,
                    aimMoveSpeed,
                    aimMinX,
                    aimMaxX,
                    GameInput.GetAccelerometerDeadZone(),
                    GameInput.GetAccelerometerScale(),
                    GameInput.GetAccelerometerSmoothing());

                screensData.SettingsScreen.Value.Show();
                ecb.DestroyEntity(entity);
            }

            foreach (var (_, entity) in SystemAPI.Query<OpenLeaderboardEvent>().WithEntityAccess()) {
                if (GameSystemLocator.IsInitialized) {
                    var leaderboardManager = GameSystemLocator.Get<LeaderboardManager>();
                    if (leaderboardManager != null) {
                        FetchAndShowLeaderboard(screensData.LeaderboardScreen.Value, leaderboardManager);
                    }
                }
                ecb.DestroyEntity(entity);
            }

            foreach (var (_, entity) in SystemAPI.Query<OpenFriendsEvent>().WithEntityAccess()) {
                screensData.FriendsScreen.Value.Show();
                ecb.DestroyEntity(entity);
            }

            foreach (var (challengeEvent, entity) in SystemAPI.Query<ChallengeEvent>().WithEntityAccess()) {
                screensData.HUDScreen.Value.SetOpponentScore(challengeEvent.ScoreToBeat, "Challenge High Score");

                // Update SeedManager with the challenge seed
                if (GameSystemLocator.IsInitialized) {
                    var seedManager = GameSystemLocator.Get<SeedManager>();
                    if (seedManager != null) {
                        seedManager.SetSeed((int)challengeEvent.Seed);
                    }
                }

                ecb.DestroyEntity(entity);
            }

            foreach (var (_, entity) in SystemAPI.Query<CloseSettingsEvent>().WithEntityAccess()) {
screensData.SettingsScreen.Value.Hide();

                switch (screensData.SettingsScreen.Value.GetReturnToState()) {
                    case GameState.Playing:
                        screensData.HUDScreen.Value.Show();
                        break;
                    case GameState.GameOver:
                        screensData.GameOverScreen.Value.Show();
                        break;
                    default:
                        screensData.StartScreen.Value.Show();
                        break;
                }

                ecb.DestroyEntity(entity);
            }

            foreach (var (settingsEvent, entity) in SystemAPI.Query<ApplySettingsEvent>().WithEntityAccess()) {
                if (SystemAPI.TryGetSingletonRW<SuikaGameConfig>(out var gameConfig))
                    gameConfig.ValueRW.Seed = settingsEvent.Seed;

                foreach (var aimConfig in SystemAPI.Query<RefRW<DropperAimConfig>>()) {
                    aimConfig.ValueRW.MoveSpeed = settingsEvent.AimMoveSpeed;
                    aimConfig.ValueRW.MinX = settingsEvent.AimMinX;
                    aimConfig.ValueRW.MaxX = settingsEvent.AimMaxX;
                }

                GameInput.ApplyAccelerometerSettings(
                    settingsEvent.AccelerometerDeadZone,
                    settingsEvent.AccelerometerScale,
                    settingsEvent.AccelerometerSmoothing,
                    settingsEvent.InvertAccelerometer);

                screensData.SettingsScreen.Value.Hide();

                switch (screensData.SettingsScreen.Value.GetReturnToState()) {
                    case GameState.Playing:
                        screensData.HUDScreen.Value.Show();
                        break;
                    case GameState.GameOver:
                        screensData.GameOverScreen.Value.Show();
                        break;
                    default:
                        screensData.StartScreen.Value.Show();
                        break;
                }

                ecb.DestroyEntity(entity);
            }

            // Update Score in HUD
            if (SystemAPI.TryGetSingleton<SuikaScore>(out var scoreData))
                screensData.HUDScreen.Value.SetScore(scoreData.Value);

            // Optional: Handle Transition to Game Over (e.g. from a GameOverEvent)
            // ... implementation here ...
        }

        static bool TryGetRoot(out VisualElement root) {
            root = null;

            var rootSource = UnityEngine.Object.FindAnyObjectByType<SuikaPanelRoot>();
            if (rootSource == null) {
                var panelRenderer = UnityEngine.Object.FindAnyObjectByType<PanelRenderer>();
                if (panelRenderer == null)
                    return false;

                rootSource = panelRenderer.gameObject.AddComponent<SuikaPanelRoot>();
            }

            root = rootSource.RootElement;
            return root != null;
        }

        static UIScreens CreateScreens(ref SystemState state, VisualElement root) {
            var rootSource = UnityEngine.Object.FindAnyObjectByType<SuikaPanelRoot>();
            var entryTemplate = rootSource != null ? rootSource.LeaderboardEntryTemplate : null;

            var screens = new UIScreens {
StartScreen = StartScreen.Instantiate(root.Q<VisualElement>("StartScreen")),
                HUDScreen = HUDScreen.Instantiate(root.Q<VisualElement>("HUDScreen")),
                GameOverScreen = GameOverScreen.Instantiate(root.Q<VisualElement>("GameOverScreen")),
                SettingsScreen = SettingsScreen.Instantiate(root.Q<VisualElement>("SettingsScreen")),
                LeaderboardScreen = LeaderboardScreen.Instantiate(root.Q<VisualElement>("LeaderboardScreen"), entryTemplate),
                FriendsScreen = FriendsScreen.Instantiate(root.Q<VisualElement>("FriendsScreen"))
            };

            screens.HUDScreen.Value.Hide();
            screens.GameOverScreen.Value.Hide();
            screens.SettingsScreen.Value.Hide();
            screens.LeaderboardScreen.Value.Hide();
            screens.FriendsScreen.Value.Hide();

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, screens);
            return screens;
        }

        static void DestroyScreens(ref SystemState state) {
            using (var query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UIScreens>())) {
                if (!query.IsEmptyIgnoreFilter)
                    state.EntityManager.DestroyEntity(query);
            }
        }

        static bool ScreensMatchRoot(UIScreens screens, VisualElement root) {
            var start = screens.StartScreen.Value;
            var hud = screens.HUDScreen.Value;
            var gameOver = screens.GameOverScreen.Value;
            var settings = screens.SettingsScreen.Value;
            var leaderboard = screens.LeaderboardScreen.Value;
            var friends = screens.FriendsScreen.Value;

            return start && hud && gameOver && settings && leaderboard && friends &&
                   start.RootElement == root.Q<VisualElement>("StartScreen") &&
                   hud.RootElement == root.Q<VisualElement>("HUDScreen") &&
                   gameOver.RootElement == root.Q<VisualElement>("GameOverScreen") &&
                   settings.RootElement == root.Q<VisualElement>("SettingsScreen") &&
                   leaderboard.RootElement == root.Q<VisualElement>("LeaderboardScreen") &&
                   friends.RootElement == root.Q<VisualElement>("FriendsScreen");
        }

        private static async void FetchAndShowLeaderboard(LeaderboardScreen screen, LeaderboardManager manager) {
            screen.Show();
            screen.SetStatus("Connecting to services...");

            try {
                if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized) {
                    screen.SetStatus("Unity Services not initialized. Please start from InitBoot scene.");
                    return;
                }

                if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn) {
                    screen.SetStatus("Not signed in. Please log in to view scores.");
                    return;
                }

                var scores = await manager.GetGlobalLeaderboardAsync();
                if (scores == null) {
                    screen.SetStatus("Could not fetch scores. Offline mode active.");
                    return;
                }
                screen.Populate(scores);
            }
            catch (Exception e) {
                Debug.LogError($"Error fetching leaderboard: {e.Message}");
                screen.SetStatus("Error connecting to leaderboard.");
            }
        }

        private static async void UpdateOpponentScoreFromAutoplay(HUDScreen hud) {
            if (GameSystemLocator.IsInitialized) {
                var leaderboardManager = GameSystemLocator.Get<LeaderboardManager>();
                if (leaderboardManager != null) {
                    try {
                        var scores = await leaderboardManager.GetGlobalLeaderboardAsync(limit: 1);
                        if (scores != null && scores.Results.Count > 0) {
                            hud.SetOpponentScore((int)scores.Results[0].Score, "Opponent to beat");
                        }
                    }
                    catch (Exception e) {
                        Debug.LogWarning($"Could not fetch autoplay opponent: {e.Message}");
                    }
                }
            }
        }

        private static async void SubmitScoreToLeaderboard(int score) {
try {
                if (GameSystemLocator.IsInitialized)
                {
                    var gameManager = GameSystemLocator.Get<GameManagerUGS>();
                    if (gameManager != null && gameManager.IsOfflineMode)
                    {
                        Debug.Log("[UGS Leaderboards] Skipping score submission (Offline Mode).");
                        return;
                    }
                }

                if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized && 
                    Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn) {
Debug.Log($"[UGS Leaderboards] Submitting score: {score} to 'MergeGameLeaderboard'...");
                    await Unity.Services.Leaderboards.LeaderboardsService.Instance.AddPlayerScoreAsync("MergeGameLeaderboard", score);
                    Debug.Log("[UGS Leaderboards] Score submitted successfully!");
                } else {
                    Debug.LogWarning("[UGS Leaderboards] Cannot submit: Unity Services not initialized or player not signed in.");
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"[UGS Leaderboards] Error submitting score: {e.Message}");
            }
        }
    }
}