using BeatSaberMarkupLanguage;
using SiraUtil.Logging;
using HMUI;
using System;
using BeatSaverNotifier.UI;
using Zenject;

namespace BeatSaverNotifier.FlowCoordinators
{
    internal class BeatSaverNotifierFlowCoordinator : FlowCoordinator
    {
        private SiraLog _siraLog;
        private MainFlowCoordinator _mainFlowCoordinator;
        private BeatSaverNotifierViewController _viewController;
        
        [Inject]
        public void Construct(MainFlowCoordinator mainFlowCoordinator, BeatSaverNotifierViewController viewController, SiraLog siraLog)
        {
            _mainFlowCoordinator = mainFlowCoordinator;
            _siraLog = siraLog;
            this._viewController = viewController;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle("BeatSaverNotifier");
                    showBackButton = true;
                    ProvideInitialViewControllers(_viewController);
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