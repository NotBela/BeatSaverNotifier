using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.FlowCoordinators;
using BeatSaverNotifier.UI;
using Zenject;

namespace BeatSaverNotifier.Installers
{
    internal class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            var checker = Container.Resolve<BeatSaverChecker>();
            
            if (checker == null) Plugin.Log.Info("this is null");
            else Plugin.Log.Info("this is not null");
            
            Container.BindInstance(checker).AsSingle(); // HOW IS THIS FAILING ????????????
            
            Container.BindInterfacesAndSelfTo<MenuButtonController>().AsSingle();
            Container.BindInterfacesAndSelfTo<BeatSaverNotifierFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
            Container.BindInterfacesAndSelfTo<BeatSaverNotifierViewController>().FromNewComponentAsViewController().AsSingle();
        }
    }
}