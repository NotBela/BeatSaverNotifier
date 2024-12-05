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

        [NonNullable]
        public virtual DateTime firstCheckTime { get; set; } = DateTime.Now;
        
        [UseConverter(typeof(ListConverter<User>))]
        public virtual List<User> followedUsers { get; set; } = new List<User>();
    }
}