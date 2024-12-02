using System;
using System.Threading.Tasks;
using Zenject;
using BeatSaverSharp;

namespace BeatSaverNotifier.BeatSaver
{
    public class BeatSaverChecker : IInitializable
    {
        private readonly BeatSaverSharp.BeatSaver _beatSaver = new BeatSaverSharp.BeatSaver(
            new BeatSaverOptions("BeatSaverNotifier", 
                IPA.Loader.PluginManager.GetPluginFromId("BeatSaverNotifier").HVersion.ToString()));
        
        public event Action OnBeatSaverCheck;
        
        public void Initialize()
        {
            // check beatsaver on startup
        }

        private async Task CheckBeatSaver()
        {
            // yeah!
            
            OnBeatSaverCheck?.Invoke();
        }
    }
}