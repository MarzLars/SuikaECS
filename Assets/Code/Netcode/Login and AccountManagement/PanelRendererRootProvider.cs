using UnityEngine;
using UnityEngine.UIElements;

namespace Suika.Scripts.Login_and_AccountManagement
{
    /// <summary>
    /// Caches the root visual tree for a runtime UI Toolkit panel rendered by PanelRenderer.
    /// </summary>
    internal sealed class PanelRendererRootProvider : MonoBehaviour
    {
        PanelRenderer m_PanelRenderer;

        public VisualElement RootElement { get; private set; }

        void OnEnable()
        {
            m_PanelRenderer = GetComponent<PanelRenderer>();
            if (m_PanelRenderer != null)
            {
                m_PanelRenderer.RegisterUIReloadCallback(OnUIReload);
            }
        }

        void OnDisable()
        {
            if (m_PanelRenderer != null)
            {
                m_PanelRenderer.UnregisterUIReloadCallback(OnUIReload);
            }

            RootElement = null;
        }

        void OnUIReload(PanelRenderer renderer, VisualElement rootElement)
        {
            m_PanelRenderer = renderer;
            RootElement = rootElement;
        }
    }
}