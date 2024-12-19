using BeatSaberMarkupLanguage;
using SiraUtil.Logging;
using HMUI;
using System;
using System.Linq;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.Configuration;
using BeatSaverNotifier.UI.BSML;
using BeatSaverNotifier.UI.BSML.LoadingScreen;
using BeatSaverNotifier.UI.BSML.LoginScreen;
using BeatSaverNotifier.UI.BSML.MapListScreen;
using Zenject;

namespace BeatSaverNotifier.FlowCoordinators
{
    internal class BeatSaverNotifierFlowCoordinator : FlowCoordinator
    {
        private SiraLog _siraLog;
        private MainFlowCoordinator _mainFlowCoordinator;
        private BeatSaverNotifierViewController _mainViewController;
        private MapQueueViewController _mapQueueViewController;
        private LoadingScreenViewController _loadingScreenViewController;
        private BeatSaverChecker _beatSaverChecker;
        private LoginScreenViewController _loginViewController;
        private LoginScreenAwaitingUserViewController _loginAwaitingUserViewControllerViewController;
        
        [Inject]
        public void Construct(MainFlowCoordinator mainFlowCoordinator, 
            MapQueueViewController mapQueueViewController, 
            BeatSaverNotifierViewController viewController, 
            LoadingScreenViewController loadingScreenViewController,
            LoginScreenViewController loginScreenViewController,
            BeatSaverChecker beatSaverChecker,
            LoginScreenAwaitingUserViewController loginAwaitingUserViewController,
            SiraLog siraLog)
        {
            _mainFlowCoordinator = mainFlowCoordinator;
            _siraLog = siraLog;
            this._mainViewController = viewController;
            this._mapQueueViewController = mapQueueViewController;
            this._loadingScreenViewController = loadingScreenViewController;
            this._beatSaverChecker = beatSaverChecker;
            this._loginViewController = loginScreenViewController;
            this._loginAwaitingUserViewControllerViewController = loginAwaitingUserViewController;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (!firstActivation) return;
                
                SetTitle("BeatSaverNotifier");
                showBackButton = true;

                if (!PluginConfig.Instance.isSignedIn)
                {
                    ProvideInitialViewControllers(_loginViewController);
                    return;
                }
                                        
                if (_beatSaverChecker.isChecking)
                {
                    showBackButton = false;
                    ProvideInitialViewControllers(_loadingScreenViewController);
                    return;
                }
                ProvideInitialViewControllers(_mainViewController, rightScreenViewController: _mapQueueViewController);
            }
            catch (Exception ex)
            {
                _siraLog.Error(ex);
            }
        }

        public void presentAwaitingUserLoginScreen()
        {
            this.showBackButton = true;
            this.ReplaceTopViewController(_loginAwaitingUserViewControllerViewController);
            this.SetRightScreenViewController(null, ViewController.AnimationType.Out);
        }

        public void presentLoginScreen()
        {
            showBackButton = true;
            this.ReplaceTopViewController(_loginViewController);
            this.SetRightScreenViewController(null, ViewController.AnimationType.Out);
        }

        public void presentLoadingScreen()
        {
            Plugin.Log.Info("got to here1");
            showBackButton = false;
            Plugin.Log.Info("got to here2");
            try
            {
                ReplaceTopViewController(_loadingScreenViewController);
            }
            catch (Exception ex)
            {
                _siraLog.Error(ex);
            }
            
            Plugin.Log.Info("got to here3");
            this.SetRightScreenViewController(null, ViewController.AnimationType.Out);
            Plugin.Log.Info("got to here4");
        }

        public void presentMapListAndQueueViewController()
        {
            showBackButton = true;
            this.ReplaceTopViewController(_mainViewController);
            this.SetRightScreenViewController(_mapQueueViewController, ViewController.AnimationType.In);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}