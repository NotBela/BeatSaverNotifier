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
    internal class BeatSaverNotifierFlowCoordinator : FlowCoordinator, IInitializable
    {
        private SiraLog _siraLog;
        private MainFlowCoordinator _mainFlowCoordinator;
        private BeatSaverNotifierViewController _mainViewController;
        private MapQueueViewController _mapQueueViewController;
        private LoadingScreenViewController _loadingScreenViewController;
        private LoginScreenViewController _loginViewController;
        private LoginScreenAwaitingUserViewController _loginAwaitingUserViewControllerViewController;

        public event Action<ViewController> onViewControllerSwitched;
        
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
            this._loginViewController = loginScreenViewController;
            this._loginAwaitingUserViewControllerViewController = loginAwaitingUserViewController;
            this._siraLog = siraLog;
        }
        
        public ViewController currentViewController { get; private set; }

        public enum FlowState
        {
            Loading,
            Login,
            LoginAwaitingUser,
            MapList
        }

        public void switchToView(FlowState flowState)
        {
            ViewController viewController = flowState switch
            {
                FlowState.Loading => _loadingScreenViewController,
                FlowState.Login => _loginViewController,
                FlowState.LoginAwaitingUser => _loginAwaitingUserViewControllerViewController,
                _ => _mainViewController,
            };
            
            SetRightScreenViewController(viewController is BeatSaverNotifierViewController ? _mapQueueViewController : null, ViewController.AnimationType.In);
            
            if (!isActivated || isInTransition || currentViewController == viewController) return;

            showBackButton = true; // viewController is not LoadingScreenViewController;
           	
            ReplaceTopViewController(viewController, () => currentViewController = viewController);
            
            onViewControllerSwitched?.Invoke(currentViewController);
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (addedToHierarchy)
            {
                showBackButton = true; // currentViewController is not LoadingScreenViewController;
                ProvideInitialViewControllers(currentViewController);
                onViewControllerSwitched?.Invoke(currentViewController);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }

        public void Initialize()
        {
            SetTitle("BeatSaverNotifier");
            currentViewController = _loadingScreenViewController;
        }
    }
}