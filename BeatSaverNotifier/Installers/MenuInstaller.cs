using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.FlowCoordinators;
using BeatSaverNotifier.UI;
using BeatSaverNotifier.UI.BSML;
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
            Container.BindInterfacesAndSelfTo<CallbackListener>().AsSingle();
        }
    }
}