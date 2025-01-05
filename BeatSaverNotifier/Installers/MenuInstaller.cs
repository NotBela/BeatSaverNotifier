using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.UI;
using BeatSaverNotifier.UI.BSML;
using BeatSaverNotifier.UI.BSML.LoadingScreen;
using BeatSaverNotifier.UI.BSML.LoginScreen;
using BeatSaverNotifier.UI.BSML.MapListScreen;
using BeatSaverNotifier.UI.FlowCoordinators;
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
            Container.BindInterfacesAndSelfTo<MapQueueViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<LoadingScreenViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<LoginScreenViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<SongPreviewController>().AsSingle();
        }
    }
}