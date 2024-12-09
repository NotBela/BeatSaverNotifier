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

        public virtual string userId { get; set; } = String.Empty;
        
        [UseConverter(typeof(DictionaryConverter<DateTime>))]
        public virtual Dictionary<string, DateTime> savedFollowDictionary { get; set; } = new Dictionary<string, DateTime>();
    }
}