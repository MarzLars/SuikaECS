using Suika.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Event = Suika.UI.Event;

namespace SuikaScripts
{
    /// <summary>
    ///     Runs on the main thread. Consumes ECS RestartRequest markers and performs Scene reload.
    ///     Keeps managed SceneManager calls out of ECS systems.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SceneRestartHandler : MonoBehaviour
    {
        int _reloadSceneBuildIndex = -1;
        string _reloadSceneName;
        string _reloadScenePath;

        void Awake() {
            DontDestroyOnLoad(gameObject);
            CaptureReloadScene(SceneManager.GetActiveScene());
        }


        void Update() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            var em = world.EntityManager;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly(typeof(RestartRequest)));
            if (query.IsEmptyIgnoreFilter)
                return;

            // Ensure jobs completed before scene transition
            em.CompleteAllTrackedJobs();

            // Destroy all restart request entities
            em.DestroyEntity(query);
            ResetUiRuntimeState(em);

            ReloadCapturedScene();
        }

        void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureInstance() {
            if (FindAnyObjectByType<SceneRestartHandler>())
                return;

            var go = new GameObject(nameof(SceneRestartHandler));
            DontDestroyOnLoad(go);
            go.AddComponent<SceneRestartHandler>();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            CaptureReloadScene(scene);
        }

        void CaptureReloadScene(Scene scene) {
            if (!scene.IsValid() || IsSubScenePath(scene.path))
                return;

            _reloadSceneBuildIndex = scene.buildIndex;
            _reloadScenePath = scene.path;
            _reloadSceneName = scene.name;
        }

        static bool IsSubScenePath(string scenePath) {
            return !string.IsNullOrEmpty(scenePath) &&
                   (scenePath.Contains("/MainSubScenes/") || scenePath.Contains("\\MainSubScenes\\"));
        }

        static void ReloadCapturedScene() {
            var handler = FindAnyObjectByType<SceneRestartHandler>();
            if (!handler)
                return;

            handler.ReloadCapturedSceneInstance();
        }

        void ReloadCapturedSceneInstance() {
            if (_reloadSceneBuildIndex >= 0) {
                SceneManager.LoadScene(_reloadSceneBuildIndex);
                return;
            }

            if (!string.IsNullOrEmpty(_reloadScenePath)) {
                SceneManager.LoadScene(_reloadScenePath);
                return;
            }

            if (!string.IsNullOrEmpty(_reloadSceneName)) {
                SceneManager.LoadScene(_reloadSceneName);
                return;
            }

            SceneManager.LoadScene(0);
        }

        static void ResetUiRuntimeState(EntityManager em) {
            DestroyAll(em, ComponentType.ReadOnly<UIScreens>());
            DestroyAll(em, ComponentType.ReadOnly<Event>());

            using (var gameStateQuery = em.CreateEntityQuery(ComponentType.ReadWrite<SuikaGameState>()))
            using (var gameStateEntities = gameStateQuery.ToEntityArray(Allocator.Temp)) {
                foreach (var entity in gameStateEntities)
                    em.SetComponentData(entity, new SuikaGameState { State = GameState.Init });
            }

            using (var scoreQuery = em.CreateEntityQuery(ComponentType.ReadWrite<SuikaScore>()))
            using (var scoreEntities = scoreQuery.ToEntityArray(Allocator.Temp)) {
                foreach (var t in scoreEntities) em.SetComponentData(t, new SuikaScore { Value = 0 });
            }
        }

        static void DestroyAll(EntityManager em, ComponentType componentType) {
            using var query = em.CreateEntityQuery(componentType);
            if (!query.IsEmptyIgnoreFilter)
                em.DestroyEntity(query);
        }
    }
}