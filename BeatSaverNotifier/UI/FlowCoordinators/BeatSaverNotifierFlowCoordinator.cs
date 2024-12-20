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
        
        private ViewController currentViewController;

        public enum FlowState
        {
            Loading,
            Login,
            LoginAwaitingUser,
            MapList
        }

        private FlowState _state = FlowState.Loading;

        public FlowState State
        {
            get => _state;
            set
            {
                if (!isActivated || isInTransition || value == _state) return;
                _state = value;
                
                switch (value)
                {
                    case FlowState.Login:
                        showBackButton = true;
                        currentViewController = _loginViewController;
                        presentOrReplaceViewController(_loginViewController);
                        break;
                    case FlowState.LoginAwaitingUser:
                        showBackButton = true;
                        currentViewController = _loginAwaitingUserViewControllerViewController;
                        presentOrReplaceViewController(_loginAwaitingUserViewControllerViewController);
                        break;
                    case FlowState.MapList:
                        showBackButton = true;
                        presentOrReplaceViewController(_mainViewController);
                        currentViewController = _mainViewController;
                        SetRightScreenViewController(_mapQueueViewController, ViewController.AnimationType.In);
                        break;
                    case FlowState.Loading:
                        showBackButton = false;
                        currentViewController = _loadingScreenViewController;
                        presentOrReplaceViewController(_loadingScreenViewController);
                        SetRightScreenViewController(null, ViewController.AnimationType.Out);
                        break;
                }
            }
        }

        private void presentOrReplaceViewController(ViewController viewController)
        {
            DismissViewController(currentViewController);
            PresentViewController(viewController, immediately: true);
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (!addedToHierarchy) return;
                
                SetTitle("BeatSaverNotifier");
                showBackButton = _state != FlowState.Loading;

                ProvideInitialViewControllers(_loadingScreenViewController);

                if (!PluginConfig.Instance.isSignedIn)
                {
                    State = FlowState.Login;
                    return;
                }

                if (!_beatSaverChecker.IsChecking) State = FlowState.MapList;
            }
            catch (Exception ex)
            {
                _siraLog.Error(ex);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}