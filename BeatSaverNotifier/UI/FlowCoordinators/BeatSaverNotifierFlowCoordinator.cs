using BeatSaberMarkupLanguage;
using SiraUtil.Logging;
using HMUI;
using System;
using BeatSaverNotifier.UI;
using BeatSaverNotifier.UI.BSML;
using Zenject;

namespace BeatSaverNotifier.FlowCoordinators
{
    internal class BeatSaverNotifierFlowCoordinator : FlowCoordinator
    {
        private SiraLog _siraLog;
        private MainFlowCoordinator _mainFlowCoordinator;
        private BeatSaverNotifierViewController _mainViewController;
        private MapQueueViewController _mapQueueViewController;
        
        [Inject]
        public void Construct(MainFlowCoordinator mainFlowCoordinator, MapQueueViewController mapQueueViewController, BeatSaverNotifierViewController viewController, SiraLog siraLog)
        {
            _mainFlowCoordinator = mainFlowCoordinator;
            _siraLog = siraLog;
            this._mainViewController = viewController;
            this._mapQueueViewController = mapQueueViewController;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle("BeatSaverNotifier");
                    showBackButton = true;
                    ProvideInitialViewControllers(_mainViewController, rightScreenViewController: _mapQueueViewController);
                }
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