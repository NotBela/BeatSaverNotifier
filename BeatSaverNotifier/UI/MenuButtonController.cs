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
            
            MenuButtons.Instance.RegisterButton(_menuButton);
        }

        private void BeatSaverCheckerOnBeatSaverCheckStarted() => _menuButton.HoverHint = "Loading...";

        private void BeatSaverCheckerOnBeatSaverCheckFinished(List<BeatmapModel> obj)
        {
            _menuButton.HoverHint = $"{obj.Count} maps in queue.";
            MenuButtons.Instance.UnregisterButton(_menuButton);
            _menuButton.Text = "<color=#00FF00><b>BeatSaverNotifier";
            MenuButtons.Instance.RegisterButton(_menuButton);
        }

        public void Dispose()
        {
            _beatSaverChecker.OnBeatSaverCheckFinished -= BeatSaverCheckerOnBeatSaverCheckFinished;
            _beatSaverChecker.onBeatSaverCheckStarted -= BeatSaverCheckerOnBeatSaverCheckStarted;
            
            MenuButtons.Instance.UnregisterButton(_menuButton);
        }

        private void onButtonPressed() => _parent.PresentFlowCoordinator(_flowCoordinator);
    }
}