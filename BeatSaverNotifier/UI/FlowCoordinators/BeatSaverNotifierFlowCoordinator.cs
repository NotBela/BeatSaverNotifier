using BeatSaberMarkupLanguage;
using SiraUtil.Logging;
using HMUI;
using System;
using System.Linq;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.UI.BSML;
using BeatSaverNotifier.UI.BSML.LoadingScreen;
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
        
        [Inject]
        public void Construct(MainFlowCoordinator mainFlowCoordinator, 
            MapQueueViewController mapQueueViewController, 
            BeatSaverNotifierViewController viewController, 
            LoadingScreenViewController loadingScreenViewController,
            BeatSaverChecker beatSaverChecker,
            SiraLog siraLog)
        {
            _mainFlowCoordinator = mainFlowCoordinator;
            _siraLog = siraLog;
            this._mainViewController = viewController;
            this._mapQueueViewController = mapQueueViewController;
            this._loadingScreenViewController = loadingScreenViewController;
            this._beatSaverChecker = beatSaverChecker;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle("BeatSaverNotifier");
                    showBackButton = true;

                    if (_beatSaverChecker.isChecking)
                    {
                        showBackButton = false;
                        ProvideInitialViewControllers(_loadingScreenViewController);
                        return;
                    }
                    ProvideInitialViewControllers(_mainViewController, rightScreenViewController: _mapQueueViewController);
                }
            }
            catch (Exception ex)
            {
                _siraLog.Error(ex);
            }
        }

        public void presentLoadingScreen()
        {
            showBackButton = false;
            this.ReplaceTopViewController(_loadingScreenViewController);
            this.SetRightScreenViewController(null, ViewController.AnimationType.Out);
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