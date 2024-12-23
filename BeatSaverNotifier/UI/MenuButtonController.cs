using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Models;
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
        private readonly BeatSaverChecker _beatSaverChecker;
        
        private readonly MenuButton _menuButton;

        public MenuButtonController(BeatSaverNotifierFlowCoordinator flowCoordinator, MainFlowCoordinator parent, BeatSaverChecker beatSaverChecker)
        {
            _beatSaverChecker = beatSaverChecker;
            this._flowCoordinator = flowCoordinator;
            this._parent = parent;
            
            this._menuButton = new MenuButton("BeatSaverNotifier", 
                !PluginConfig.Instance.isSignedIn ? 
                    "Not signed into BeatSaver! Please sign in at the mod options menu!" : 
                    "Loading...", 
                onButtonPressed, 
                PluginConfig.Instance.isSignedIn);
        }

        public void Initialize()
        {
            _beatSaverChecker.OnBeatSaverCheckFinished += BeatSaverCheckerOnBeatSaverCheckFinished;
            _beatSaverChecker.onBeatSaverCheckStarted += BeatSaverCheckerOnBeatSaverCheckStarted;
            _flowCoordinator.onBackButtonPressed += FlowCoordinatorOnBackButtonPressed;
            
            MenuButtons.Instance.RegisterButton(_menuButton);
        }

        private void FlowCoordinatorOnBackButtonPressed()
        {
            updateMenuButton();
        }

        private void updateMenuButton()
        {
            var buttonText = _beatSaverChecker.CachedMaps.Count == 0 ? "BeatSaverNotifier" : "<color=#00FF00><b>BeatSaverNotifier";
            
            _menuButton.HoverHint = $"{_beatSaverChecker.CachedMaps.Count} maps in queue.";
            
            MenuButtons.Instance.UnregisterButton(_menuButton);
            _menuButton.Text = buttonText;
            MenuButtons.Instance.RegisterButton(_menuButton);
        }

        private void BeatSaverCheckerOnBeatSaverCheckStarted()
        {
            _menuButton.HoverHint = "Loading...";
            
            MenuButtons.Instance.UnregisterButton(_menuButton);
            _menuButton.Text = "BeatSaverNotifier";
            MenuButtons.Instance.RegisterButton(_menuButton);
        }

        private void BeatSaverCheckerOnBeatSaverCheckFinished(List<BeatmapModel> beatmaps) => updateMenuButton();

        public void Dispose()
        {
            _beatSaverChecker.OnBeatSaverCheckFinished -= BeatSaverCheckerOnBeatSaverCheckFinished;
            _beatSaverChecker.onBeatSaverCheckStarted -= BeatSaverCheckerOnBeatSaverCheckStarted;
            _flowCoordinator.onBackButtonPressed -= FlowCoordinatorOnBackButtonPressed;
            
            MenuButtons.Instance.UnregisterButton(_menuButton);
        }

        private void onButtonPressed() => _parent.PresentFlowCoordinator(_flowCoordinator);
    }
}