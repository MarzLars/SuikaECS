using Suika.Scripts.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Suika.Scripts.Utilities.Logger;
namespace Suika.Scripts.Login_and_AccountManagement
{
    /// <summary>
    /// Handles the sign-in options UI and interactions for the Gem Hunter game.
    /// This class manages the display of sign-in options and delegates sign-in actions to specific handlers.
    /// </summary>
    public class SignInOptionsView : MonoBehaviour
    {
        [SerializeField] UIDocument m_SignInOptionsDocument;
        [SerializeField] VisualTreeAsset m_LoginUxml;
        
        [SerializeField] UnityPlayerAccountSignIn m_UnityPlayerAccountSignIn;

        VisualElement m_Root;
        VisualElement m_SignInOptions;

        Button m_ButtonClose;
        Button m_ButtonUnityID;
        
        public Button ButtonClose => m_ButtonClose;
        public Button ButtonUnityID => m_ButtonUnityID;
        
        VisualElement ResolveRoot()
        {
            if (m_SignInOptionsDocument != null && m_SignInOptionsDocument.rootVisualElement != null) return m_SignInOptionsDocument.rootVisualElement;
            m_SignInOptionsDocument = GetComponent<UIDocument>()
                ?? GetComponentInChildren<UIDocument>(true)
                ?? GetComponentInParent<UIDocument>(true);
            
            if (m_SignInOptionsDocument != null)
            {
                if (m_SignInOptionsDocument.visualTreeAsset == null && m_LoginUxml != null)
                {
                    m_SignInOptionsDocument.visualTreeAsset = m_LoginUxml;
                }
                m_SignInOptionsDocument.enabled = true;
                return m_SignInOptionsDocument.rootVisualElement;
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

        public bool Initialize()
        {
            m_Root = ResolveRoot();
            if (m_Root == null)
            {
                Logger.LogError($"{nameof(SignInOptionsView)} requires a UIDocument or PanelRenderer in inspector, parent, or child GameObject.");
                return false;
            }

            m_SignInOptions = m_Root.Q<VisualElement>("SignInElement");
            if (m_SignInOptions == null)
            {
                Logger.LogError("SignInElement not found in sign-in UI.");
                return false;
            }

            var signInBackground = m_SignInOptions.Q<VisualElement>("SignInBackground");
            if (signInBackground == null)
            {
                Logger.LogError("SignInBackground not found in sign-in UI.");
                return false;
            }
            
            m_ButtonClose = signInBackground.Q<Button>("ButtonClose");
            
            var signUpButtonContainer = signInBackground.Q<VisualElement>("SignUpButtonContainer");
            if (signUpButtonContainer == null)
            {
                Logger.LogError("SignUpButtonContainer not found in sign-in UI.");
                return false;
            }
            
            m_ButtonUnityID = signUpButtonContainer.Q<Button>("ButtonUnityID");
            if (m_ButtonUnityID == null)
            {
                Logger.LogError("ButtonUnityID not found in sign-in UI.");
                return false;
            }
            
            m_SignInOptions.style.display = DisplayStyle.None;
            return true;
        }

        public void ShowSignInOptions()
        {
            m_Root.style.display = DisplayStyle.Flex;
            m_SignInOptions.style.display = DisplayStyle.Flex;
        }

        public void HideSignInOptions()
        {
            m_SignInOptions.style.display = DisplayStyle.None;
        }
    }
}
