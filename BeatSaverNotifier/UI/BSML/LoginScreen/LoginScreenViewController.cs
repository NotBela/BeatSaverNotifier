using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.Configuration;
using HMUI;
using SiraUtil.Logging;
using TMPro;
using UnityEngine.UI;
using Zenject;

namespace BeatSaverNotifier.UI.BSML.LoginScreen
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.LoginScreen.LoginScreenView.bsml")]
    public class LoginScreenViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        [Inject] private readonly OAuthApi _oauthApi = null;
        
        [UIParams] private readonly BSMLParserParams _parserParams = null;
        
        [UIComponent("checkBrowserText")]
        private readonly TextMeshProUGUI _checkBrowserText = null;
        
        [UIComponent("loginVertical")] private readonly VerticalLayoutGroup _loginVertical = null;
        [UIComponent("loggedInVertical")] private readonly VerticalLayoutGroup _loggedInVertical = null;
        [UIComponent("loginButton")] private readonly Button _loginButton = null;
        [UIComponent("versionText")] private readonly TextMeshProUGUI _versionText = null;

        [UIAction("#post-parse")]
        void postParse()
        {
            _versionText.text = $"BeatSaverNotifier v{Plugin.Instance.metaData.HVersion}";
            
            if (PluginConfig.Instance.isSignedIn) _loginVertical.gameObject.SetActive(false);
            else _loggedInVertical.gameObject.SetActive(false);
        }
        
        [UIAction("loginButtonOnClick")]
        private void LoginButtonOnClick()
        {
            _loginButton.interactable = false;
            _checkBrowserText.gameObject.SetActive(true);
            _oauthApi.startNewOAuthFlow();
        }

        [UIAction("signOutConfirmModalConfirmButtonOnClick")]
        private void SignOutConfirmModalConfirmButtonOnClick()
        {
            _parserParams.EmitEvent("signOutConfirmModalHide");
            PluginConfig.Instance.isSignedIn = false;
            _loginButton.interactable = true;
            
            _checkBrowserText.gameObject.SetActive(false);
            
            _loggedInVertical.gameObject.SetActive(false);
            _loginVertical.gameObject.SetActive(true);
        }
        
        [UIAction("signOutConfirmModalDenyButtonOnClick")]
        private void SignOutConfirmModalDenyButtonOnClick() => _parserParams.EmitEvent("signOutConfirmModalHide");

        [UIAction("signOutOfBeatSaverOnClick")]
        private void SignOutOfBeatSaverOnClick() => _parserParams.EmitEvent("signOutConfirmModalShow");
        
        public void Initialize()
        {
            BSMLSettings.Instance.AddSettingsMenu("BeatSaverNotifier", "BeatSaverNotifier.UI.BSML.LoginScreen.LoginScreenView.bsml", this);
        }

        public void Dispose()
        {
            BSMLSettings.Instance.RemoveSettingsMenu(this);
        }
    }
}