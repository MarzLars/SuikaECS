using System;
using Suika.Scripts.Core;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
namespace Suika.Scripts.Login_and_AccountManagement
{
    public class MainMenuLoginUIController : MonoBehaviour
    {
        [SerializeField] MainMenuLoginView m_MainMenuLoginView;
        [SerializeField] GuestPlayAnonymousSignIn m_GuestPlayAnonymousSignIn;
        [SerializeField] SignInOptionsUIController m_SignInOptionsController;

        GameManagerUGS m_GameManagerUGS;
        NetworkConnectivityHandler m_NetworkConnectivityHandler;
        bool m_IsUIInitialized = false;

        void OnEnable()
        {
            if (m_MainMenuLoginView != null && m_IsUIInitialized)
            {
                m_MainMenuLoginView.HideInfoPopUp();
                UpdateUIForAuthState();
            }
        }

        void Start()
        {
            m_GameManagerUGS = GameSystemLocator.Get<GameManagerUGS>();
            m_NetworkConnectivityHandler = GameSystemLocator.Get<NetworkConnectivityHandler>();

            if (!m_IsUIInitialized)
            {
                if (!m_MainMenuLoginView.InitializeLoginUI())
                {
                    return;
                }
                m_MainMenuLoginView.HideInfoPopUp();
                m_MainMenuLoginView.ShowMainMenu();
                m_IsUIInitialized = true;
            }
            UpdateUIForAuthState();
            SetupEventHandlers();
        }

        void UpdateUIForAuthState()
        {
            m_MainMenuLoginView.ShowMainMenu();
            m_MainMenuLoginView.SignUpInfoButton.text = "PLAY OFFLINE";
            m_MainMenuLoginView.GuestPlayButton.text = IsSignedInAnonymously() ? "PLAY AS GUEST" : "PLAY ANONYMOUS";
            m_MainMenuLoginView.ConnectAccountButton.text = IsSignedInWithLinkedAccount() ? "PLAY WITH UNITY ACCOUNT" : "UNITY ACCOUNT";
        }

        void SetupEventHandlers()
        {
            m_MainMenuLoginView.GuestPlayButton.clicked += HandleClickGuestPlay;
            m_MainMenuLoginView.ConnectAccountButton.clicked += HandleConnectSocialAccount;
            m_MainMenuLoginView.SignUpInfoButton.clicked += HandlePlayOffline;
            m_MainMenuLoginView.ClosePopUpButton.clicked += HandleCloseInfoPopUp;
            AuthenticationService.Instance.SignedIn += HandleAuthenticationSignedIn;
            AuthenticationService.Instance.SignInFailed += HandleAuthenticationFailed;
            m_NetworkConnectivityHandler.OnlineStatusChanged += ToggleConnectAccountButton;
        }

        async void HandleClickGuestPlay()
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                _ = m_GameManagerUGS.StartGameplay();
                return;
            }

            m_MainMenuLoginView.SetLoginButtonsEnabled(false);
            bool signedIn = await m_GuestPlayAnonymousSignIn.SignInAnonymousAccountAsync();
            m_MainMenuLoginView.SetLoginButtonsEnabled(true);

            if (signedIn)
            {
                _ = m_GameManagerUGS.StartGameplay();
            }
            else
            {
                m_MainMenuLoginView.ShowMainMenu();
            }
        }

        void HandleConnectSocialAccount()
        {
            if (IsSignedInWithLinkedAccount())
            {
                _ = m_GameManagerUGS.StartGameplay();
            }
            else
            {
                m_MainMenuLoginView.HideMainMenuUI();
                m_SignInOptionsController.ShowSocialSignUpOptions();
            }
        }

        static bool IsSignedInWithLinkedAccount()
        {
            return AuthenticationService.Instance.IsSignedIn
                && AuthenticationService.Instance.PlayerInfo?.Identities != null
                && AuthenticationService.Instance.PlayerInfo.Identities.Count > 0;
        }

        static bool IsSignedInAnonymously()
        {
            return AuthenticationService.Instance.IsSignedIn
                && AuthenticationService.Instance.PlayerInfo?.Identities != null
                && AuthenticationService.Instance.PlayerInfo.Identities.Count == 0;
        }

        void HandleOpenInfoPopUp()
        {
            m_MainMenuLoginView.ShowInfoPopUp();
        }

        void HandleCloseInfoPopUp()
        {
            m_MainMenuLoginView.HideInfoPopUp();
        }

        void HandlePlayOffline()
        {
            m_MainMenuLoginView.HideInfoPopUp();
            m_GameManagerUGS.StartOfflineGameplay();
        }

        void HandleAuthenticationSignedIn()
        {
            UpdateUIForAuthState();
        }

        void HandleAuthenticationFailed(RequestFailedException ex)
        {
            Utilities.Logger.LogWarning($"Authentication failed: {ex.ErrorCode}");
            m_MainMenuLoginView.SetLoginButtonsEnabled(true);
            m_MainMenuLoginView.ShowMainMenu();
        }

        public void OpenMainMenu()
        {
            m_MainMenuLoginView.ShowMainMenu();
            m_MainMenuLoginView.HideInfoPopUp();
        }

        void ToggleConnectAccountButton(bool isOnline)
        {
            if (isOnline)
            {
                m_MainMenuLoginView.ConnectAccountButton.SetEnabled(true);
            }
            else 
            {
                m_MainMenuLoginView.ConnectAccountButton.SetEnabled(false);
            }
        }

        void OnDisable()
        {
            if (m_MainMenuLoginView != null)
            {
                m_MainMenuLoginView.GuestPlayButton.clicked -= HandleClickGuestPlay;
                m_MainMenuLoginView.ConnectAccountButton.clicked -= HandleConnectSocialAccount;
                m_MainMenuLoginView.SignUpInfoButton.clicked -= HandlePlayOffline;
                m_MainMenuLoginView.ClosePopUpButton.clicked -= HandleCloseInfoPopUp;
            }

            AuthenticationService.Instance.SignedIn -= HandleAuthenticationSignedIn;
            AuthenticationService.Instance.SignInFailed -= HandleAuthenticationFailed;
            
            if (m_NetworkConnectivityHandler != null)
            {
                m_NetworkConnectivityHandler.OnlineStatusChanged -= ToggleConnectAccountButton;
            }
        }
    }
}
