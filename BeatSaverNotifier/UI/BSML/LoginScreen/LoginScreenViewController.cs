using System;
using System.Diagnostics;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.FlowCoordinators;
using Zenject;

namespace BeatSaverNotifier.UI.BSML.LoginScreen
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.LoginScreen.LoginScreenView.bsml")]
    public class LoginScreenViewController : BSMLAutomaticViewController
    {
        private OAuthApi _oauthApi;
        private BeatSaverNotifierFlowCoordinator _flowCoordinator;
        
        [Inject]
        void Inject(OAuthApi oauthApi, BeatSaverNotifierFlowCoordinator flowCoordinator)
        {
            this._oauthApi = oauthApi;
            this._flowCoordinator = flowCoordinator;
        }
        
        [UIAction("loginButtonOnClick")]
        private void LoginButtonOnClick()
        {
            _flowCoordinator.State = BeatSaverNotifierFlowCoordinator.FlowState.LoginAwaitingUser;
            _oauthApi.startNewOAuthFlow();
        }
    }
}