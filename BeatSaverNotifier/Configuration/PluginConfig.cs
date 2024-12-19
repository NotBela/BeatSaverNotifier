using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public virtual long firstCheckUnixTimeStamp { get; set; } = -1;
        
        public virtual bool isSignedIn { get; set; } = false;
        
        [UseConverter(typeof(ListConverter<string>))]
        public virtual List<string> keysToIgnore { get; set; } = new List<string>();
    }
}