using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using Suika.Scripts.Core;
using Suika.Scripts.PlayerDataManagement;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.Login_and_AccountManagement
{
    /// <summary>
    /// Handles Unity ID sign-in and linking functionality.
    /// Uses PlayerAccountService for Unity authentication.
    /// </summary>
    public class UnityPlayerAccountSignIn : MonoBehaviour
    {
        PlayerDataManager m_PlayerDataManager;
        GameManagerUGS m_GameManagerUGS;
        AccountManagementUIController m_AccountManagementUIController;

        string m_ExternalIds;
        bool m_IsWaitingForSignIn = false;

        void Start()
        {
            m_PlayerDataManager = GameSystemLocator.Get<PlayerDataManager>();
            m_GameManagerUGS = GameSystemLocator.Get<GameManagerUGS>();
            m_AccountManagementUIController = GetComponent<AccountManagementUIController>();
        }
        
        // Unity ID sign-in from the main menu
        public async void StartSignInOrLink()
        {
            try
            {
                // First, ensure we're signed in to PlayerAccountService
                if (!PlayerAccountService.Instance.IsSignedIn)
                {
                    // Prevent multiple simultaneous sign-in attempts
                    if (m_IsWaitingForSignIn)
                    {
                        Utilities.Logger.LogDemo("Already waiting for Unity Player Account sign-in");
                        return;
                    }
                    
                    Utilities.Logger.LogDemo("Starting Unity Player Account sign-in...");
                    
                    // Clean up any existing subscription first
                    CleanupEventSubscription();

                    // Subscribe to the SignedIn and SignInFailed events
                    m_IsWaitingForSignIn = true;
                    PlayerAccountService.Instance.SignedIn += OnPlayerAccountSignedIn;
                    PlayerAccountService.Instance.SignInFailed += OnPlayerAccountSignInFailed;
                    await PlayerAccountService.Instance.StartSignInAsync();
                }
                else
                {
                    // Already signed in to PlayerAccountService, process immediately
                    Utilities.Logger.LogDemo("Already signed in to Unity Player Account, processing...");
                    await ProcessUnityToken(PlayerAccountService.Instance.AccessToken);
                }
            }
            catch (RequestFailedException ex)
            {
                Utilities.Logger.LogException(ex);
                CleanupEventSubscription();
                HandleAuthenticationFailure($"Unity Player Account error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Utilities.Logger.LogException(ex);
                CleanupEventSubscription();
                HandleAuthenticationFailure($"Unexpected error: {ex.Message}");
            }
        }

        void OnPlayerAccountSignInFailed(RequestFailedException ex)
        {
            Utilities.Logger.LogError($"Unity Player Account sign-in failed: {ex.Message}");
            CleanupEventSubscription();
            HandleAuthenticationFailure(ex.Message);
        }
            
        /// <summary>
        /// Callback for when PlayerAccountService sign-in completes
        /// This is required by Unity Player Account service
        /// </summary>
        async void OnPlayerAccountSignedIn()
        {
            try
            {
                Utilities.Logger.LogDemo("Unity Player Account sign-in completed");
                // Unsubscribe immediately to avoid duplicate calls
                CleanupEventSubscription();
                await ProcessUnityToken(PlayerAccountService.Instance.AccessToken);
            }
            catch (Exception ex)
            {
                Utilities.Logger.LogException(ex);
                CleanupEventSubscription();
                HandleAuthenticationFailure($"Error processing Unity token: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clean up event subscription and reset waiting state
        /// </summary>
        void CleanupEventSubscription()
        {
            if (m_IsWaitingForSignIn)
            {
                PlayerAccountService.Instance.SignedIn -= OnPlayerAccountSignedIn;
                PlayerAccountService.Instance.SignInFailed -= OnPlayerAccountSignInFailed;
                m_IsWaitingForSignIn = false;
            }
        }
        
        /// <summary>
        /// Routes Unity token to appropriate method based on authentication state
        /// </summary>
        async Task ProcessUnityToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                Utilities.Logger.LogError("Unity access token is null or empty");
                HandleAuthenticationFailure("No Unity access token available");
                return;
            }
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await SignInWithUnityAsync(accessToken);
            }
            else
            {
                await LinkWithUnityAsync(accessToken);
            }
        }

        /// <summary>
        /// Sign in with Unity ID for new users
        /// </summary>
        async Task SignInWithUnityAsync(string accessToken)
        {
            try
            {
                Utilities.Logger.LogDemo("Signing in with Unity ID...");
                await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
                
                m_ExternalIds = GetExternalIds(AuthenticationService.Instance.PlayerInfo);
                Utilities.Logger.LogDemo("Successfully signed in with Unity ID!");

                await m_GameManagerUGS.StartGameplay();
            }
            catch (AuthenticationException ex)
            {
                Utilities.Logger.LogException(ex);
                Utilities.Logger.LogError($"Unity ID sign-in failed: {ex.ErrorCode} - {ex.Message}");
                HandleAuthenticationFailure(ex.Message);
            }
            catch (Exception ex)
            {
                Utilities.Logger.LogException(ex);
                Utilities.Logger.LogError($"Unexpected error during Unity ID sign-in: {ex.Message}");
                HandleAuthenticationFailure(ex.Message);
            }
        }
        
        /// <summary>
        /// Link Unity Player Account for existing users
        /// </summary>
        async Task LinkWithUnityAsync(string accessToken)
        {
            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Utilities.Logger.LogWarning("Player must be signed in before linking Unity ID");
                    HandleAuthenticationFailure("Not signed in to Unity Authentication");
                    return;
                }
                
                Utilities.Logger.LogDemo("Linking Unity ID account...");
                await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
                Utilities.Logger.LogDemo("Successfully linked with Unity ID!");
                
                // Update player data and UI
                m_PlayerDataManager?.UpdateAnonymousStatus(false);
                m_AccountManagementUIController?.UpdateAccounts("Unity");
                await m_GameManagerUGS.StartGameplay();
            }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                Utilities.Logger.LogWarning("Unity ID is already linked to another account, switching accounts...");
                await HandleExistingLinkedAccount(accessToken);
            }
            catch (Exception ex)
            {
                Utilities.Logger.LogException(ex);
                Utilities.Logger.LogError($"Unity ID linking failed: {ex.Message}");
                m_AccountManagementUIController?.OnAuthenticationError("Unity", ex.Message);
            }
        }
        
        /// <summary>
        /// Handle case where Unity ID is already linked to another account
        /// </summary>
        async Task HandleExistingLinkedAccount(string accessToken)
        {
            try
            {
                Utilities.Logger.LogDemo("Switching to existing linked Unity account...");
                
                // Sign out of current account
                AuthenticationService.Instance.SignOut();
                
                // Sign in with the Unity account
                await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
                
                m_ExternalIds = GetExternalIds(AuthenticationService.Instance.PlayerInfo);
                m_AccountManagementUIController?.UpdateAccounts("Unity");
                Utilities.Logger.LogDemo("Successfully switched to existing Unity account");
                await m_GameManagerUGS.StartGameplay();
            }
            catch (AuthenticationException ex)
            {
                Utilities.Logger.LogException(ex);
                Utilities.Logger.LogError($"Failed to switch to existing Unity account: {ex.ErrorCode} - {ex.Message}");
                m_AccountManagementUIController?.OnAuthenticationError("Unity", ex.Message);
            }
        }
        
        /// <summary>
        /// Handle authentication failures for both sign-in and linking
        /// </summary>
        void HandleAuthenticationFailure(string error)
        {
            // Ensure event cleanup on any failure
            CleanupEventSubscription();
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                // Failed during sign-in - return to main menu
                Utilities.Logger.LogDemo("Unity ID sign-in failed, returning to main menu");
                _ = m_GameManagerUGS.RequestLoadMainMenu();
            }
            else
            {
                // Failed during linking - update account management UI
                m_AccountManagementUIController?.OnAuthenticationError("Unity", error);
            }
        }
        
        /// <summary>
        /// Logs out the user from both services
        /// </summary>
        public void SignOut()
        {
            PlayerAccountService.Instance.SignOut();
            AuthenticationService.Instance.SignOut();
            LogAuthenticationStatus();
        }

        void LogAuthenticationStatus()
        {
            Utilities.Logger.LogDemo($"Unity player Accounts State: {(PlayerAccountService.Instance.IsSignedIn ? "Signed in" : "Signed out")}");
            Utilities.Logger.LogDemo($"Authentication Service State: {(AuthenticationService.Instance.IsSignedIn ? "Signed in" : "Signed out")}");
            Utilities.Logger.LogDemo($"Is Anonymous: {m_PlayerDataManager.IsPlayerAnonymous()}");
            Utilities.Logger.LogDemo($"PlayerId: {AuthenticationService.Instance.PlayerId}");
            Utilities.Logger.LogDemo($"PlayerToken: {PlayerAccountService.Instance.AccessToken}");
            Utilities.Logger.LogDemo(GetPlayerInfoText());
        }
        
        string GetPlayerInfoText()
        {
            return $"ExternalIds: <b>{m_ExternalIds}</b>";
        }
        
        /// <summary>
        /// Retrieves external IDs associated with the player, that is, what identity credentials they have
        /// </summary>
        string GetExternalIds(PlayerInfo playerInfo)
        {
            if (playerInfo == null)
            {
                Logger.LogWarning("PlayerInfo is null in GetExternalIds method.");
                return "None (PlayerInfo is null)";
            }
            
            if (playerInfo.Identities == null)
            {
                return "None";
            }
            
            if (playerInfo.Identities.Count == 0)
            {
                return "None (No identities)";
            }
            
            var sb = new StringBuilder();
            foreach (var id in playerInfo.Identities)
            {
                sb.Append(" " + id.TypeId);
            }

            return sb.ToString();
        }

        void OnDestroy()
        {
            CleanupEventSubscription(); // Prevents memory leaks
        }
    }
}
