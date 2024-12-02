using System;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace BeatSaverNotifier.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        public virtual int firstCheckTime { get; set; } = -1;
    }
}