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
        private BeatSaverNotifierFlowCoordinator _flowCoordinator;
        private BeatSaverChecker _beatSaverChecker;

        [Inject]
        void Construct(BeatSaverNotifierFlowCoordinator flowCoordinator, BeatSaverChecker beatSaverChecker)
        {
            _flowCoordinator = flowCoordinator;
            _beatSaverChecker = beatSaverChecker;
        }

        private void onBeatSaverCheckStarted() => _flowCoordinator.presentLoadingScreen();

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