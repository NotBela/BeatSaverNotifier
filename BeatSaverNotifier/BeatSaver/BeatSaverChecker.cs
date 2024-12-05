using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BeatSaverNotifier.Configuration;
using Zenject;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using BeatSaverSharp.Models.Pages;
using SiraUtil.Logging;
using UnityEngine;

namespace BeatSaverNotifier.BeatSaver
{
    public class BeatSaverChecker : IInitializable
    {
        private readonly BeatSaverSharp.BeatSaver _beatSaver = new BeatSaverSharp.BeatSaver(
            new BeatSaverOptions("BeatSaverNotifier", 
                IPA.Loader.PluginManager.GetPluginFromId("BeatSaverNotifier").HVersion.ToString()));
        
        public event Action<List<Beatmap>> OnBeatSaverCheck;
        
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

        private async Task CheckBeatSaverAsync()
        {
            List<Beatmap> maps = new List<Beatmap>();
            
            var listOfMappers = PluginConfig.Instance.followedUsersWithFollowDate;

            if (listOfMappers != null)
            {
                foreach (var mapper in listOfMappers)
                {
                    var allPages = await getAllRequiredPagesFromUser(mapper);

                    foreach (var page in allPages)
                    {
                        foreach (var map in page.Beatmaps)
                        {
                            if (map.Uploaded > mapper.Item2) maps.Add(map);
                        }
                    }
                }
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