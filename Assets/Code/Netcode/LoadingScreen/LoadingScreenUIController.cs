using UnityEngine;

namespace Suika.Scripts.LoadingScreen
{
    /// <summary>
    /// Controls the loading screen UI, managing visibility and progress updates
    /// during scene transitions and loading operations
    /// </summary>
    public class LoadingScreenUIController : MonoBehaviour
    {
        [SerializeField] LoadingScreenView m_View;
        [SerializeField] Camera m_LoadingScreenCamera;

        void Start()
        {
            if (m_View == null)
            {
                m_View = GetComponent<LoadingScreenView>();
            }
        }

        public void HandleSceneLoading()
        {
            m_View.ShowLoadingScreen();
            m_LoadingScreenCamera.gameObject.SetActive(true);
        }
        
        public void HideLoadingScreen()
        {
            m_View.HideLoadingScreen();
            m_LoadingScreenCamera.gameObject.SetActive(false);
        }

        public void UpdateLoadingProgress(float progress)
        {
            m_View.UpdateProgressBar(progress);
        }
    }
}
