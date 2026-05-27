using Suika.Scripts.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Suika.Scripts.Utilities.Logger;
namespace Suika.Scripts.Login_and_AccountManagement
{
    /// <summary>
    /// Handles the main menu login functionality.
    /// This class manages the UI elements for login options, including guest play and cloud save loading.
    /// </summary>
    public class MainMenuLoginView : MonoBehaviour
    {
        [SerializeField] UIDocument m_MainMenuUI;
        [SerializeField] VisualTreeAsset m_LoginUxml;
        VisualElement m_Root;
        VisualElement m_MainMenuContainer;
        VisualElement m_LoginButtonsElement;
        
        public Button SignUpInfoButton { get; private set; }
        VisualElement m_SignUpInfoPopUp;
        public  Button ClosePopUpButton { get; private set; }
        
        public Button ConnectAccountButton { get; private set; }
        public Button GuestPlayButton { get; private set; }

        /// <summary>
        /// Sets up the game login UI by initializing UI elements and registering button click events.
        /// </summary>
        VisualElement ResolveRoot()
        {
            if (m_MainMenuUI != null && m_MainMenuUI.rootVisualElement != null) return m_MainMenuUI.rootVisualElement;
            m_MainMenuUI = GetComponent<UIDocument>()
                ?? GetComponentInChildren<UIDocument>(true)
                ?? GetComponentInParent<UIDocument>(true);
            
            if (m_MainMenuUI != null)
            {
                if (m_MainMenuUI.visualTreeAsset == null && m_LoginUxml != null)
                {
                    m_MainMenuUI.visualTreeAsset = m_LoginUxml;
                }
                return m_MainMenuUI.rootVisualElement;
            }
            
            // Unity 6: PanelRenderer is used for world-space UI instead of UIDocument
            var pr = GetComponent<PanelRenderer>()
                ?? GetComponentInChildren<PanelRenderer>(true)
                ?? GetComponentInParent<PanelRenderer>(true);
            if (pr == null) return null;

            var rootProvider = pr.GetComponent<PanelRendererRootProvider>();
            if (rootProvider == null)
            {
                rootProvider = pr.gameObject.AddComponent<PanelRendererRootProvider>();
            }

            return rootProvider.RootElement;
        }

        public bool InitializeLoginUI()
        {
            m_Root = ResolveRoot();
            if (m_Root == null)
            {
                Logger.LogError($"{nameof(MainMenuLoginView)} requires a UIDocument or PanelRenderer in inspector, parent, or child GameObject.");
                return false;
            }

            m_MainMenuContainer = m_Root.Q<VisualElement>("MainMenuContainer");
            if (m_MainMenuContainer == null)
            {
                Logger.LogError("Required UI element 'MainMenuContainer' not found in MainMenuLoginView UI.");
                return false;
            }

            m_LoginButtonsElement = m_MainMenuContainer.Q<VisualElement>("LoginButtonsElement");
            if (m_LoginButtonsElement == null)
            {
                Logger.LogError("Required UI element 'LoginButtonsElement' not found in MainMenuLoginView UI.");
                return false;
            }

            GuestPlayButton = m_LoginButtonsElement.Q<Button>("GuestPlayButton");
            ConnectAccountButton = m_LoginButtonsElement.Q<Button>("ConnectAccountButton");
            if (GuestPlayButton == null || ConnectAccountButton == null)
            {
                Logger.LogError("Required login buttons not found in MainMenuLoginView UI.");
                return false;
            }
            
            // Info Pop Up
            SignUpInfoButton = m_MainMenuContainer.Q<Button>("SignUpInfoButton");
            m_SignUpInfoPopUp = m_MainMenuContainer.Q<VisualElement>("SignUpInfoPopUp");
            if (SignUpInfoButton == null)
            {
                Logger.LogError("Required UI element 'SignUpInfoButton' not found in MainMenuLoginView UI.");
                return false;
            }
            if (m_SignUpInfoPopUp == null)
            {
                Logger.LogError("Required UI element 'SignUpInfoPopUp' not found in MainMenuLoginView UI.");
                return false;
            }

            ClosePopUpButton = m_SignUpInfoPopUp.Q<Button>("ClosePopUpButton");
            if (ClosePopUpButton == null)
            {
                Logger.LogError("Required UI element 'ClosePopUpButton' not found in MainMenuLoginView UI.");
                return false;
            }

            return true;
        }
        
        public void HideMainMenuUI()
        {
            m_MainMenuContainer.style.display = DisplayStyle.None;
        }
        
        /// <summary>
        /// Displays the start login options and hides the sign-in options.
        /// </summary>
        public void ShowMainMenu()
        {
            if (m_Root != null) m_Root.style.display = DisplayStyle.Flex;
            if (m_MainMenuContainer != null) m_MainMenuContainer.style.display = DisplayStyle.Flex;
        }

        public void SetLoginButtonsEnabled(bool enabled)
        {
            GuestPlayButton.SetEnabled(enabled);
            ConnectAccountButton.SetEnabled(enabled);
            SignUpInfoButton.SetEnabled(enabled);
        }

        public void ShowInfoPopUp()
        {
            m_SignUpInfoPopUp.style.display = DisplayStyle.Flex;
        }

        public void HideInfoPopUp()
        {
            m_SignUpInfoPopUp.style.display = DisplayStyle.None;
        }
    }
}
