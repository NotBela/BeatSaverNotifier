using System;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.Configuration;
using BeatSaverNotifier.UI.FlowCoordinators;
using HMUI;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.UI
{
    internal class MenuButtonController : IInitializable, IDisposable
    {
        private readonly BeatSaverNotifierFlowCoordinator _flowCoordinator;
        private readonly MainFlowCoordinator _parent;
        private readonly SiraLog _logger;
        
        private readonly MenuButton _menuButton;

        public MenuButtonController(BeatSaverNotifierFlowCoordinator flowCoordinator, MainFlowCoordinator parent, SiraLog logger)
        {
            this._logger = logger;
            this._flowCoordinator = flowCoordinator;
            this._parent = parent;
            this._menuButton = new MenuButton("BeatSaverNotifier", 
                !PluginConfig.Instance.isSignedIn ? 
                    "Not signed into BeatSaver! Please sign in at the mod options menu!" : 
                    "Loading...", 
                onButtonPressed, 
                PluginConfig.Instance.isSignedIn);
        }

        public void Initialize() => MenuButtons.Instance.RegisterButton(_menuButton);

        public void Dispose() => MenuButtons.Instance.UnregisterButton(_menuButton);

        private void onButtonPressed() => _parent.PresentFlowCoordinator(_flowCoordinator);
    }
}