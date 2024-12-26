using System;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.Configuration;
using BeatSaverNotifier.UI.FlowCoordinators;
using HMUI;
using SiraUtil.Logging;
using Loader = SongCore.Loader;
using Zenject;

namespace BeatSaverNotifier.UI.BSML.LoadingScreen
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.LoadingScreen.LoadingScreenView.bsml")]
    public class LoadingScreenViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private SiraLog _logger;
        
        private BeatSaverNotifierFlowCoordinator _beatSaverNotifierFlowCoordinator;
        private BeatSaverChecker _beatSaverChecker;
        
        [Inject]
        void Inject(BeatSaverNotifierFlowCoordinator beatSaverNotifierFlowCoordinator, BeatSaverChecker beatSaverChecker, SiraLog logger)
        {
            this._beatSaverNotifierFlowCoordinator = beatSaverNotifierFlowCoordinator;
            this._beatSaverChecker = beatSaverChecker;
            _logger = logger;
        }

        private void onBeatSaverCheckStarted() => _beatSaverNotifierFlowCoordinator.switchToView(BeatSaverNotifierFlowCoordinator.FlowState.Loading);

        private async void onViewControllerSwitched(ViewController viewController)
        {
            try
            {
                await Task.Delay(500); // this is required or the game gets mad

                if (Loader.AreSongsLoading) return;
                if (!_beatSaverChecker.IsChecking && _beatSaverNotifierFlowCoordinator.currentViewController is LoadingScreenViewController)
                    _beatSaverNotifierFlowCoordinator.switchToView(BeatSaverNotifierFlowCoordinator.FlowState.MapList);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
        
        public void Initialize()
        {
            _beatSaverChecker.onBeatSaverCheckStarted += onBeatSaverCheckStarted;
            _beatSaverNotifierFlowCoordinator.onViewControllerSwitched += onViewControllerSwitched;
        }

        public void Dispose()
        {
            _beatSaverChecker.onBeatSaverCheckStarted -= onBeatSaverCheckStarted;
            _beatSaverNotifierFlowCoordinator.onViewControllerSwitched -= onViewControllerSwitched;
        }
    }
}