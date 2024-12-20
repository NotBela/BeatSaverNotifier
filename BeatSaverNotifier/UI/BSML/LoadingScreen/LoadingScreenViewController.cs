using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.FlowCoordinators;
using Zenject;

namespace BeatSaverNotifier.UI.BSML.LoadingScreen
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.LoadingScreen.LoadingScreenView.bsml")]
    public class LoadingScreenViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private BeatSaverNotifierFlowCoordinator _beatSaverNotifierFlowCoordinator;
        private BeatSaverChecker _beatSaverChecker;
        
        [Inject]
        void Inject(BeatSaverNotifierFlowCoordinator beatSaverNotifierFlowCoordinator, BeatSaverChecker beatSaverChecker)
        {
            this._beatSaverNotifierFlowCoordinator = beatSaverNotifierFlowCoordinator;
            this._beatSaverChecker = beatSaverChecker;
        }

        private void onBeatSaverCheckStarted() => _beatSaverNotifierFlowCoordinator.State =
            BeatSaverNotifierFlowCoordinator.FlowState.Loading;
        
        public void Initialize()
        {
            _beatSaverChecker.onBeatSaverCheckStarted += onBeatSaverCheckStarted;
        }

        public void Dispose()
        {
            _beatSaverChecker.onBeatSaverCheckStarted -= onBeatSaverCheckStarted;
        }
    }
}