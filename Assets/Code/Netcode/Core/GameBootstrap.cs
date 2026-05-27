using UnityEngine;
using UnityEngine.EventSystems;
namespace Suika.Scripts.Core
{
    /// <summary>
    /// Handles the initialization and event system switching between the two integrated demos:
    /// 1. The UGS (Unity Gaming Services) meta-game demo
    /// 2. The Match3 gameplay demo
    /// 
    /// Each demo has its own GameManager and EventSystem. This class ensures proper initialization
    /// and handles switching between their respective event systems when transitioning to gameplay.
    /// </summary>
    public class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] EventSystem m_UGSEventSystem;
        [SerializeField] EventSystem m_GameEventSystem;
        
        public void InitializeGameplayManager()
        {
            if (m_UGSEventSystem != null)
                m_UGSEventSystem.enabled = false;

            if (m_GameEventSystem != null)
                m_GameEventSystem.enabled = true;
        }
    }
}
