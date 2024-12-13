using System;
using BeatSaverNotifier.FlowCoordinators;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaverNotifier.BeatSaver;
using HMUI;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.UI
{
    internal class MenuButtonController : IInitializable, IDisposable
    {
        private readonly BeatSaverNotifierFlowCoordinator _flowCoordinator;
        private readonly MainFlowCoordinator _parent;
        private readonly BeatSaverChecker _beatSaverChecker;
        private readonly SiraLog _logger;
        
        private readonly MenuButton _menuButton;

        public MenuButtonController(BeatSaverNotifierFlowCoordinator flowCoordinator, MainFlowCoordinator parent, BeatSaverChecker beatSaverChecker, SiraLog logger)
        {
            this._logger = logger;
            this._beatSaverChecker = beatSaverChecker;
            this._flowCoordinator = flowCoordinator;
            this._parent = parent;
            this._menuButton = new MenuButton("BeatSaverNotifier", onButtonPressed);
        }

        public void Initialize() => MenuButtons.Instance.RegisterButton(_menuButton);

        public void Dispose() => MenuButtons.Instance.UnregisterButton(_menuButton);

        private async void onButtonPressed()
        {
            try
            {
                _parent.PresentFlowCoordinator(_flowCoordinator);
                await _beatSaverChecker.CheckBeatSaverAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}