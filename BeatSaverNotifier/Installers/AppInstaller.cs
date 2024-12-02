using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.Configuration;
using BeatSaverNotifier.FlowCoordinators;
using BeatSaverNotifier.UI;
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