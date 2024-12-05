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
            Container.BindInterfacesAndSelfTo<MenuButtonController>().AsSingle();
            Container.BindInterfacesAndSelfTo<BeatSaverNotifierFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
            Container.BindInterfacesAndSelfTo<BeatSaverNotifierViewController>().FromNewComponentAsViewController().AsSingle();
        }
    }
}