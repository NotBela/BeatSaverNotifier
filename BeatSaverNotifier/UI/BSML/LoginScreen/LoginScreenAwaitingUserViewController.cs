using System;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.Configuration;
using BeatSaverNotifier.FlowCoordinators;
using SiraUtil.Logging;
using TMPro;
using Zenject;

namespace BeatSaverNotifier.UI.BSML.LoginScreen
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.LoginScreen.LoginScreenAwaitingUserView.bsml")]
    public class LoginScreenAwaitingUserViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private OAuthApi _oauthApi;
        private BeatSaverChecker _beatSaverChecker;
        private SiraLog _logger;
        private BeatSaverNotifierFlowCoordinator _flowCoordinator;
        
        [UIComponent("statusText")] private readonly TextMeshProUGUI _statusText = null;

        [Inject]
        void Inject(OAuthApi oauthApi, BeatSaverChecker beatSaverChecker, BeatSaverNotifierFlowCoordinator flowCoordinator, SiraLog siraLog)
        {
            this._oauthApi = oauthApi;
            this._beatSaverChecker = beatSaverChecker;
            _logger = siraLog;
            _flowCoordinator = flowCoordinator;
        }
        
        private void onAccessCodeAquired() => _statusText.text = "Status: Exchanging access code...";

        private async void onAccessTokenAquired()
        {
            try
            {
                await Task.Delay(1000);
                
                PluginConfig.Instance.isSignedIn = true;
                await _beatSaverChecker.CheckBeatSaverAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
        
        public void Initialize()
        {
            _oauthApi.onAccessCodeAquired += onAccessCodeAquired;
            _oauthApi.onAccessTokenAquired += onAccessTokenAquired;
        }

        public void Dispose()
        {
            _oauthApi.onAccessCodeAquired -= onAccessCodeAquired;
            _oauthApi.onAccessTokenAquired -= onAccessTokenAquired;
        }
    }
}