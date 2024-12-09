using System.Linq;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using BeatSaverNotifier.Configuration;
using BeatSaverNotifier.Installers;
using IPA.Loader;
using IPALogger = IPA.Logging.Logger;

namespace BeatSaverNotifier
{
    [Plugin(RuntimeOptions.DynamicInit),
     NoEnableDisable] // NoEnableDisable supresses the warnings of not having a OnEnable/OnStart
    // and OnDisable/OnExit methods
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        
        public PluginMetadata metaData { get; set; }

        [Init]
        public void Init(Zenjector zenjector, IPALogger logger, Config config, PluginMetadata metadata)
        {
            Instance = this;
            Log = logger;
            this.metaData = metadata;

            zenjector.UseLogger(logger);
            zenjector.UseMetadataBinder<Plugin>();
            
            PluginConfig.Instance = config.Generated<PluginConfig>();

            zenjector.Install<AppInstaller>(Location.App);
            zenjector.Install<MenuInstaller>(Location.Menu);
        }
    }
}