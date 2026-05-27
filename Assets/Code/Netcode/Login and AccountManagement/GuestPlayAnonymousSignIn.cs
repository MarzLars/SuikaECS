using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Suika.Scripts.Core;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.Login_and_AccountManagement
{
    /// <summary>
    /// Manages anonymous guest authentication with Unity Gaming Services
    /// </summary>
    public class GuestPlayAnonymousSignIn : MonoBehaviour
    {
        // Not using -- profiles can be used to have multiple accounts on a single device
        string m_ProfileNameInput = "Cousin Steve";
        PlayerInfo m_PlayerInfo;

        GameManagerUGS m_GameManagerUGS;
        NetworkConnectivityHandler m_NetworkConnectivityHandler;
        bool m_IsSigningInAnonymously = false;

        void Start()
        {
            m_GameManagerUGS = GameSystemLocator.Get<GameManagerUGS>();
            m_NetworkConnectivityHandler = GameSystemLocator.Get<NetworkConnectivityHandler>();
            
            AuthenticationService.Instance.SignedIn += HandleSignedIn;
            AuthenticationService.Instance.SignInFailed += HandleSignInFailed;
            AuthenticationService.Instance.SignedOut += HandleSignedOut;
        }
        
        public async void SignInAnonymousAccount()
        {
            await SignInAnonymousAccountAsync();
        }

        public async Task<bool> SignInAnonymousAccountAsync()
        {
            try
            {
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    Utilities.Logger.LogDemo("Player already signed in.");
                    return true;
                }
                m_IsSigningInAnonymously = true;
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                m_IsSigningInAnonymously = false;
                return AuthenticationService.Instance.IsSignedIn;
            }
            catch (RequestFailedException ex)
            {
                Utilities.Logger.LogWarning($"Sign in anonymously failed with error code: {ex.ErrorCode}");
                m_IsSigningInAnonymously = false;
                m_NetworkConnectivityHandler.HandleNetworkException(ex);
                return false;
            }
        }

        void HandleSignedIn()
        {
            if (m_IsSigningInAnonymously)
            {
                m_IsSigningInAnonymously = false;
                Utilities.Logger.LogDemo("Anonymous authentication successful.");
            }
        }

        void HandleSignedOut()
        {
            Utilities.Logger.LogDemo("Authentication SignedOut!");
        }

        void HandleSignInFailed(RequestFailedException ex)
        {
            Utilities.Logger.LogWarning($"Sign in anonymously failed with error code: {ex.ErrorCode}");
            m_IsSigningInAnonymously = false;
            m_NetworkConnectivityHandler.HandleNetworkException(ex);
        }

        void PlayerPrefsLog()
        {
            var sessionToken = PlayerPrefs.GetString($"{Application.cloudProjectId}.{AuthenticationService.Instance.Profile}.unity.services.authentication.session_token");
            var playerPrefsMessageResult = string.IsNullOrEmpty(sessionToken) ? "No session token for this profile" : $"Session token: {sessionToken}";
            Utilities.Logger.Log(playerPrefsMessageResult);
        }
        
        public void OnClickSignOut()
        {
            AuthenticationService.Instance.SignOut();
        }
        
        public void OnClickSwitchProfile()
        {
            try
            {
                AuthenticationService.Instance.SwitchProfile(m_ProfileNameInput);
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log(ex);
                m_ProfileNameInput = AuthenticationService.Instance.Profile;
            }
            Logger.Log($"Current Profile: {AuthenticationService.Instance.Profile}");
            PlayerPrefsLog();
        }

        void OnDisable()
        {
            AuthenticationService.Instance.SignedIn -= HandleSignedIn;
            AuthenticationService.Instance.SignInFailed -= HandleSignInFailed;
            AuthenticationService.Instance.SignedOut -= HandleSignedOut;
        }
    }
}
