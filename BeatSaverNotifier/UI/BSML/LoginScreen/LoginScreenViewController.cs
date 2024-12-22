using System;
using System.Diagnostics;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using HMUI;
using SiraUtil.Logging;
using TMPro;
using Zenject;

namespace BeatSaverNotifier.UI.BSML.LoginScreen
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.LoginScreen.LoginScreenView.bsml")]
    public class LoginScreenViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        [Inject] private readonly OAuthApi _oauthApi = null;
        
        [UIComponent("checkBrowserText")]
        private readonly TextMeshProUGUI _checkBrowserText = null;
        
        [UIAction("loginButtonOnClick")]
        private void LoginButtonOnClick()
        {
            _checkBrowserText.gameObject.SetActive(true);
            _oauthApi.startNewOAuthFlow();
        }

        public void Initialize() => BSMLSettings.Instance.AddSettingsMenu("BeatSaverNotifier", "BeatSaverNotifier.UI.BSML.LoginScreen.LoginScreenView.bsml", this);
        public void Dispose() => BSMLSettings.Instance.RemoveSettingsMenu(this);
    }
}