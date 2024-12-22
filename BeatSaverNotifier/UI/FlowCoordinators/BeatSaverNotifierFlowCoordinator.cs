using System;
using BeatSaverNotifier.UI.BSML;
using BeatSaverNotifier.UI.BSML.LoadingScreen;
using BeatSaverNotifier.UI.BSML.MapListScreen;
using HMUI;
using Zenject;

namespace BeatSaverNotifier.UI.FlowCoordinators
{
    internal class BeatSaverNotifierFlowCoordinator : FlowCoordinator, IInitializable
    {
        [Inject] private readonly MainFlowCoordinator _mainFlowCoordinator = null;
        [Inject] private readonly BeatSaverNotifierViewController _mainViewController = null;
        [Inject] private readonly MapQueueViewController _mapQueueViewController = null;
        [Inject] private readonly LoadingScreenViewController _loadingScreenViewController = null;

        public event Action<ViewController> onViewControllerSwitched;
        
        public ViewController currentViewController { get; private set; }

        public enum FlowState
        {
            Loading,
            MapList
        }

        public void switchToView(FlowState flowState)
        {
            ViewController viewController = flowState switch
            {
                FlowState.Loading => _loadingScreenViewController,
                _ => _mainViewController,
            };
            
            if (!isActivated || isInTransition || currentViewController == viewController) return;
            
            SetRightScreenViewController(viewController is BeatSaverNotifierViewController ? 
                _mapQueueViewController : null, ViewController.AnimationType.In);

            showBackButton = true; // viewController is not LoadingScreenViewController;
           	
            ReplaceTopViewController(viewController, () => currentViewController = viewController);
            
            onViewControllerSwitched?.Invoke(currentViewController);
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (addedToHierarchy)
            {
                showBackButton = true; // currentViewController is not LoadingScreenViewController;
                ProvideInitialViewControllers(currentViewController);
                onViewControllerSwitched?.Invoke(currentViewController);
            }
        }

        protected override void BackButtonWasPressed(ViewController _) => _mainFlowCoordinator.DismissFlowCoordinator(this);

        public void Initialize()
        {
            SetTitle("BeatSaverNotifier");
            currentViewController = _loadingScreenViewController;
        }
    }
}