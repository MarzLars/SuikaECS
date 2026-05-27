using UnityEngine;

namespace Code.CodeMonkey_EcsTutorial.RotationAndMovement
{
    public class PlayerShootManager : MonoBehaviour
    {
        /*
        public GameObject shootPopupPrefab;
        public static PlayerShootManager Instance { get; private set; }

        void Awake() {
            Instance = this;
        }

        // Ensure Instance exists after scene load for systems/tools expecting RuntimeInitializeOnLoadMethod
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInstanceAfterSceneLoad()
        {
            if (Instance != null) return;
            var existing = Object.FindAnyObjectByType<PlayerShootManager>();
            if (existing != null) {
                Instance = existing;
                return;
            }

            // create manager GameObject automatically if missing
            var go = new GameObject("PlayerShootManager");
            go.AddComponent<PlayerShootManager>();
        }

        public void PlayerShoot(Vector3 playerPosition) {
            Instantiate(shootPopupPrefab, playerPosition, Quaternion.identity);
        }

        //Alternative way of communicating with ECS system, using events:
        /*
        void Start() {
            PlayerShootingSystem playerShootingSystem =
                World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerShootingSystem>();

            playerShootingSystem.OnShoot += PlayerShootingSystem_OnShoot;
        }

        void PlayerShootingSystem_OnShoot(object sender, EventArgs e) {
            Entity playerEntity = (Entity)sender;
            World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<LocalTransform>(playerEntity);
            Instantiate(shootPopupPrefab, transform.position, Quaternion.identity);
        }
        */
    }
}