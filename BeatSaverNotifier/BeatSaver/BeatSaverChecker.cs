using System;
using Zenject;

namespace BeatSaverNotifier.BeatSaver
{
    public class BeatSaverChecker : IInitializable
    {
        public event Action OnBeatSaverCheck;
        
        public void Initialize()
        {
            // check beatsaver on startup
        }
    }
}