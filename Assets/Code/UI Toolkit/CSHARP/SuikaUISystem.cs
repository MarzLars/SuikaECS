using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

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
    internal sealed class SuikaPanelRoot : MonoBehaviour
    {
        public VisualElement RootElement { get; private set; }
        PanelRenderer _panelRenderer;

        void OnEnable() {
            _panelRenderer = GetComponent<PanelRenderer>();
            if (_panelRenderer != null) {
                _panelRenderer.RegisterUIReloadCallback(OnUIReload);
            }
        }

        void OnDisable() {
            if (_panelRenderer != null) {
                _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            }
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
            var ecb = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var gameState = SystemAPI.GetSingletonRW<SuikaGameState>();
            
            // Initialization
            if (gameState.ValueRO.State == GameState.Init) {
                gameState.ValueRW.State = GameState.Start;

                var rootSource = Object.FindAnyObjectByType<SuikaPanelRoot>();
                if (rootSource == null) {
                    var panelRenderer = Object.FindAnyObjectByType<PanelRenderer>();
                    if (panelRenderer == null) return;
                    rootSource = panelRenderer.gameObject.AddComponent<SuikaPanelRoot>();
                }

                var root = rootSource.RootElement;
                if (root == null) return;

                var screens = new UIScreens {
                    StartScreen = StartScreen.Instantiate(root.Q<VisualElement>("StartScreen")),
                    HUDScreen = HUDScreen.Instantiate(root.Q<VisualElement>("HUDScreen")),
                    GameOverScreen = GameOverScreen.Instantiate(root.Q<VisualElement>("GameOverScreen"))
                };

                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, screens);
                screens.StartScreen.Value.Show();

                return;
            }

            if (!SystemAPI.TryGetSingleton<UIScreens>(out var screensData)) return;

            // 1. Process Score Events
            int totalGain = 0;
            foreach (var (scoreEvent, entity) in SystemAPI.Query<ScoreEvent>().WithEntityAccess())
            {
                totalGain += scoreEvent.Amount;
                ecb.DestroyEntity(entity);
            }

            if (totalGain > 0 && SystemAPI.TryGetSingletonRW<SuikaScore>(out var score))
            {
                score.ValueRW.Value += totalGain;
            }

            // Handle Click Events
            foreach (var (_, entity) in SystemAPI.Query<PlayClickEvent>().WithEntityAccess()) {
                gameState.ValueRW.State = GameState.Playing;
                screensData.StartScreen.Value.Hide();
                screensData.HUDScreen.Value.Show();
                ecb.DestroyEntity(entity);
            }

            foreach (var (_, entity) in SystemAPI.Query<RestartClickEvent>().WithEntityAccess()) {
                gameState.ValueRW.State = GameState.Start;
                screensData.GameOverScreen.Value.Hide();
                screensData.StartScreen.Value.Show();
                
                // reset score if exists
                if (SystemAPI.HasSingleton<SuikaScore>()) {
                    SystemAPI.GetSingletonRW<SuikaScore>().ValueRW.Value = 0;
                }

                ecb.DestroyEntity(entity);
            }

            // Update Score in HUD
            if (SystemAPI.TryGetSingleton<SuikaScore>(out var scoreData)) {
                screensData.HUDScreen.Value.SetScore(scoreData.Value);
            }

            // Optional: Handle Transition to Game Over (e.g. from a GameOverEvent)
            // ... implementation here ...
        }
    }
}
