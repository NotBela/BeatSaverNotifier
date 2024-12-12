using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using Zenject;

namespace BeatSaverNotifier.Installers
{
    internal class AppInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<BeatSaverChecker>().AsSingle();
            Container.BindInterfacesAndSelfTo<CallbackListener>().AsSingle();
            Container.BindInterfacesTo<OAuthApi>().AsSingle();
        }
    }
}