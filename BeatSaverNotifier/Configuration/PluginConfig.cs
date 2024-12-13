using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeatSaverSharp.Models;
using IPA.Config.Data;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace BeatSaverNotifier.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        public virtual string refreshToken { get; set; } = String.Empty;

        public virtual DateTime? firstCheckTime { get; set; } = null;
    }
}