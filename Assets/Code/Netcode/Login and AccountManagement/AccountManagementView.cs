using UnityEngine;
using UnityEngine.UIElements;
namespace Suika.Scripts.Login_and_AccountManagement
{
    public class AccountManagementView : MonoBehaviour
    {
        // Main Elements
        [SerializeField] UIDocument m_Document;
        VisualElement m_Root;
        VisualElement m_AccountMenu;
        VisualElement m_AccountsContainer;

        // Testing Bar
        VisualElement m_TestTop;
        public Button TestInfoButton { get; private set; }
        public TextField TestingTextField { get; private set; }
        public Button TestSaveButton { get; private set; }
        public Button TestLoadButton { get; private set; }
        public Button ClosePopupButton { get; private set; }
        VisualElement TestSaveInfoPopUp;

        VisualElement m_ConfirmationDialogDarken;
        VisualElement m_ConfirmationDialog;
        Label m_ConfirmationDialogLabel;
        
        public Button ConfirmDialogButton { get; private set; }
        public Button CancelDialogButton { get; private set; }
        
        // Account Management
        [SerializeField] Sprite m_LinkedStatusBackground;
        [SerializeField] Sprite m_UnlinkedStatusBackground;
        
        public Button CloseAccountMenuButton { get; private set; }
        public Button UnityIDButton { get; private set; }
        public Button GoogleButton { get; private set; }
        
        public Button UnlinkUnityButton { get; private set; }
        public Button UnlinkGoogleButton { get; private set; }

        VisualElement m_LinkGoogleAccountContainer;

        VisualElement m_UnityIDStatus;
        VisualElement m_GoogleStatus;

        VisualElement m_UnityIDLinkedCheck;
        VisualElement m_GoogleLinkedCheck;
        VisualElement m_AppleLinkedCheck;

        VisualElement m_AccountActionContainer;
        public Button DeleteAllAccountsButton { get; private set; }
        public Button AccountActionCancelButton { get; private set; }
        Label m_PlayerIDLabel;
        
        public void Initialize()
        {
            SetupMainElements();
            SetupAccountTesting();
            SetupAccountLinking();
            SetupConfirmationDialog();
            SetupDeleteAccountElements();
        }

        void SetupMainElements()
        {
            m_Root = m_Document.rootVisualElement;
            m_AccountMenu = m_Root.Q<VisualElement>("AccountManagementMenu");
            m_AccountsContainer = m_Root.Q<VisualElement>("AccountsContainer");
            CloseAccountMenuButton = m_AccountMenu.Q<Button>("CloseAccountMenuButton");
        }

        void SetupAccountTesting()
        {
            m_TestTop = m_AccountMenu.Q<VisualElement>("TestTop");
            TestInfoButton  = m_TestTop.Q<Button>("InfoButton");
            TestingTextField = m_TestTop.Q<TextField>("TestingTextField");
            TestSaveButton = m_TestTop.Q<Button>("SaveButton");
            TestLoadButton = m_TestTop.Q<Button>("LoadButton");
            TestSaveInfoPopUp = m_AccountMenu.Q<VisualElement>("TestSaveInfoPopUp");
            ClosePopupButton = TestSaveInfoPopUp.Q<Button>("ClosePopUpButton");
        }

        void SetupAccountLinking()
        {
            UnityIDButton = m_AccountsContainer.Q<Button>("UnityIDButton");
            GoogleButton = m_AccountsContainer.Q<Button>("GoogleButton");
            
            m_UnityIDStatus = m_AccountsContainer.Q<VisualElement>("UnityIDStatus");
            m_GoogleStatus = m_AccountsContainer.Q<VisualElement>("GoogleStatus");
            
            m_UnityIDLinkedCheck = m_UnityIDStatus.Q<VisualElement>("LinkedCheck");
            m_GoogleLinkedCheck = m_GoogleStatus.Q<VisualElement>("LinkedCheck");
            // m_AppleLinkedCheck = m_AppleStatus.Q<VisualElement>("LinkedCheck");
            
            UnlinkUnityButton = m_AccountsContainer.Q<Button>("UnlinkUnityIDButton");
            UnlinkGoogleButton = m_AccountsContainer.Q<Button>("UnlinkGoogleButton");
            
            m_LinkGoogleAccountContainer = m_AccountsContainer.Q<VisualElement>("LinkGoogleAccountContainer");
            
            #if UNITY_ANDROID
            if (m_LinkGoogleAccountContainer != null)
            {
                m_LinkGoogleAccountContainer.style.display = DisplayStyle.Flex;
            }
            #else
            // Hide Google button on non-Android platforms
            if (m_LinkGoogleAccountContainer != null)
            {
                m_LinkGoogleAccountContainer.style.display = DisplayStyle.None;
            }
            #endif
        }

        void SetupConfirmationDialog()
        {
            m_ConfirmationDialog = m_AccountMenu.Q<VisualElement>("ConfirmationDialog");
            m_ConfirmationDialogDarken = m_AccountMenu.Q<VisualElement>("ConfirmationDialogDarken");
            m_ConfirmationDialogLabel = m_AccountMenu.Q<Label>("ConfirmationDialogLabel");
            ConfirmDialogButton = m_AccountMenu.Q<Button>("ConfirmDialogButton");
            CancelDialogButton = m_AccountMenu.Q<Button>("CancelDialogButton");
        }

        void SetupDeleteAccountElements()
        {
            m_AccountActionContainer = m_AccountMenu.Q<VisualElement>("AccountActionContainer");
            DeleteAllAccountsButton = m_AccountActionContainer.Q<Button>("DeleteAllAccountsButton");
            AccountActionCancelButton = m_AccountActionContainer.Q<Button>("CancelButton");
            m_PlayerIDLabel = m_AccountActionContainer.Q<Label>("PlayerIDLabel");
        }

        public void UnlinkStatusForAllAccounts()
        {
            UpdateAccountStatusVisuals(m_UnityIDStatus,m_UnityIDLinkedCheck, false);
            UpdateAccountStatusVisuals(m_UnityIDStatus,m_GoogleLinkedCheck, false);
            
            UnlinkUnityButton.SetEnabled(false);
            UnlinkGoogleButton.SetEnabled(false);
        }
        
        public void UpdateButtonState(Button linkButton, string buttonText, Button unlinkButton, bool isLinked)
        {
            linkButton.SetEnabled(!isLinked);
            unlinkButton.SetEnabled(isLinked);
            linkButton.text = isLinked ? $"{buttonText} Linked" : $"Link {buttonText}";
            unlinkButton.style.unityBackgroundImageTintColor = isLinked ? new StyleColor(Color.white): new StyleColor(Color.grey);
        }
        
        public void SetAccountStatus(LinkType accountType, bool isLinked)
        {
            switch (accountType)
            {
                case LinkType.UnityPlayerAccount:
                    UpdateAccountStatusVisuals(m_UnityIDStatus,m_UnityIDLinkedCheck, isLinked);
                    break;
                case LinkType.GooglePlayGames:
                    UpdateAccountStatusVisuals(m_GoogleStatus, m_GoogleLinkedCheck, isLinked);
                    break;
                
                // case LinkType.Apple:
                //     UpdateAccountStatusVisuals(m_AppleStatus,m_AppleLinkedCheck, isLinked);
                //     break;
            }
        }

        void UpdateAccountStatusVisuals(VisualElement statusContainer, VisualElement checkmark, bool isLinked)
        {
            statusContainer.style.backgroundImage = new StyleBackground(isLinked ? m_LinkedStatusBackground : m_UnlinkedStatusBackground);
            
            checkmark.style.display = isLinked ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        public void SetPlayerID(string playerID)
        {
            m_PlayerIDLabel.text = $"PLAYER ID: {playerID}";
        }
        
        public void ShowTestSaveInfoPopUp()
        {
            TestSaveInfoPopUp.style.display = DisplayStyle.Flex;
        }
        
        public void CloseTestSaveInfoPopUp()
        {
            TestSaveInfoPopUp.style.display = DisplayStyle.None;
        }
        
        public void ShowConfirmationDialog(string message)
        {
            m_ConfirmationDialogLabel.text = message;
            m_ConfirmationDialog.style.display = DisplayStyle.Flex;
            m_ConfirmationDialogDarken.style.display = DisplayStyle.Flex;
        }
        
        public void CloseConfirmationDialog()
        {
            m_ConfirmationDialog.style.display = DisplayStyle.None;
            m_ConfirmationDialogDarken.style.display = DisplayStyle.None;
        }
    }
}
