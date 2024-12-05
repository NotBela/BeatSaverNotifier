using BeatSaverNotifier.BeatSaver;
using Zenject;

namespace BeatSaverNotifier.Installers
{
    internal class AppInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<BeatSaverChecker>().AsSingle();
        }
    }
}