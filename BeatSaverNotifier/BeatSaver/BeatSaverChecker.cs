using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeatSaverNotifier.Configuration;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using BeatSaverSharp.Models.Pages;
using IPA.Loader;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.BeatSaver
{
    public class BeatSaverChecker : IInitializable
    {
        private readonly BeatSaverSharp.BeatSaver _beatSaver = new BeatSaverSharp.BeatSaver(
            new BeatSaverOptions("BeatSaverNotifier",
                PluginManager.GetPluginFromId("BeatSaverNotifier").HVersion.ToString()));

        private readonly SiraLog _logger;

        public BeatSaverChecker(SiraLog logger)
        {
            _logger = logger;
        }

        public async void Initialize()
        {
            // check beatsaver on startup
            try
            {
                await CheckBeatSaverAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public event Action<List<Beatmap>> OnBeatSaverCheck;

        private async Task CheckBeatSaverAsync()
        {
            var maps = new List<Beatmap>();

            var listOfMappers = PluginConfig.Instance.followedUsersWithFollowDate;


            foreach (var mapper in listOfMappers)
            {
                var allPages = await getAllRequiredPagesFromUser(mapper);

                foreach (var page in allPages)
                    foreach (var map in page.Beatmaps)
                        if (map.Uploaded > mapper.Item2) maps.Add(map);
            }

            OnBeatSaverCheck?.Invoke(maps);
        }

        private async Task<List<Page>> getAllRequiredPagesFromUser(Tuple<User, DateTime> mapper)
        {
            var returnList = new List<Page>();

            var currentPageIndex = 0;
            var currentBeatmapPage = await mapper.Item1.Beatmaps(currentPageIndex);

            while (currentBeatmapPage != null && currentBeatmapPage.Beatmaps.Last().Uploaded > mapper.Item2)
            {
                returnList.Add(currentBeatmapPage);
                currentPageIndex++;
                currentBeatmapPage = await mapper.Item1.Beatmaps(currentPageIndex);
            }

            return returnList;
        }
    }
}