using System;
using BeatSaverNotifier.FlowCoordinators;
using BeatSaberMarkupLanguage.MenuButtons;
using HMUI;
using Zenject;

namespace BeatSaverNotifier.UI
{
    internal class MenuButtonController : IInitializable, IDisposable
    {
        private readonly BeatSaverNotifierFlowCoordinator _flowCoordinator;
        private readonly MainFlowCoordinator _parent;
        
        private readonly MenuButton _menuButton;

        public MenuButtonController(BeatSaverNotifierFlowCoordinator flowCoordinator, MainFlowCoordinator parent)
        {
            _flowCoordinator = flowCoordinator;
            this._parent = parent;
            this._menuButton = new MenuButton("BeatSaverNotifier", onButtonPressed);
        }

        public void Initialize()
        {
            MenuButtons.Instance.RegisterButton(_menuButton);
        }

        public void Dispose()
        {
            MenuButtons.Instance.UnregisterButton(_menuButton);
        }

        private void onButtonPressed()
        {
            _parent.PresentFlowCoordinator(_flowCoordinator);
        }
    }
}