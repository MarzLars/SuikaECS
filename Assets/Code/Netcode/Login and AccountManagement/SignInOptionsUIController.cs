using System;
using UnityEngine;
using Suika.Scripts.Utilities;
using UnityEngine.Serialization;
using Logger = Suika.Scripts.Utilities.Logger;

namespace Suika.Scripts.Login_and_AccountManagement
{
    public class SignInOptionsUIController : MonoBehaviour
    {
        [SerializeField] SignInOptionsView m_SignInOptionsView;
        [SerializeField] MainMenuLoginUIController m_MainMenuLoginController;
        
        [SerializeField] UnityPlayerAccountSignIn m_UnityPlayerAccountSignIn;


        void Start()
        {
            m_SignInOptionsView ??= GetComponent<SignInOptionsView>();
            m_MainMenuLoginController ??= GetComponent<MainMenuLoginUIController>();
            m_UnityPlayerAccountSignIn ??= GetComponent<UnityPlayerAccountSignIn>();

            if (m_SignInOptionsView == null)
                throw new InvalidOperationException($"{nameof(SignInOptionsUIController)} requires a {nameof(SignInOptionsView)} reference.");
            if (m_MainMenuLoginController == null)
                throw new InvalidOperationException($"{nameof(SignInOptionsUIController)} requires a {nameof(MainMenuLoginUIController)} reference.");
            if (m_UnityPlayerAccountSignIn == null)
                throw new InvalidOperationException($"{nameof(SignInOptionsUIController)} requires a {nameof(UnityPlayerAccountSignIn)} reference.");

            if (!m_SignInOptionsView.Initialize())
            {
                return;
            }
            m_SignInOptionsView.ButtonClose.clicked += CloseSignInOptionsUI;
            m_SignInOptionsView.ButtonUnityID.clicked += SignInWithUnityID;
            
        }

        void CloseSignInOptionsUI()
        {
            m_SignInOptionsView.HideSignInOptions();
            m_MainMenuLoginController.OpenMainMenu();
        }

        void SignInWithUnityID()
        {
            m_UnityPlayerAccountSignIn.StartSignInOrLink();
            m_SignInOptionsView.HideSignInOptions();
        }


        public void ShowSocialSignUpOptions()
        {
            m_SignInOptionsView.ShowSignInOptions();
        }

        void OnDisable()
        {
            if (m_SignInOptionsView == null)
                return;

            m_SignInOptionsView.ButtonClose.clicked -= CloseSignInOptionsUI;
            m_SignInOptionsView.ButtonUnityID.clicked -= SignInWithUnityID;
  
        }
    }
}
