using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaverNotifier.Configuration;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using BeatSaverSharp.Models.Pages;
using Newtonsoft.Json.Linq;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.BeatSaver
{
    public class BeatSaverChecker : IInitializable
    {
        private readonly BeatSaverSharp.BeatSaver _beatSaver = new BeatSaverSharp.BeatSaver(
            new BeatSaverOptions("BeatSaverNotifier", 
                Plugin.Instance.metaData.HVersion.ToString()));
        
        private readonly HttpClient _httpClient = new HttpClient();

        private readonly SiraLog _logger;
        
        public event Action<List<Beatmap>> OnBeatSaverCheck;

        public BeatSaverChecker(SiraLog logger)
        {
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri("https://api.beatsaver.com");
        }

        public async void Initialize()
        {
            // check beatsaver on startup
            try
            {
                await updateBeatSaverFollowedList();
                await CheckBeatSaverAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }


        private async Task updateBeatSaverFollowedList()
        {
            if (PluginConfig.Instance.userId == String.Empty)
            {
                _logger.Info("UserId is not set!");
                return;
            }
            
            // this looks ugly
            var jArrayResponse = JArray.Parse(await (await _httpClient.GetAsync($"/users/followedBy/{PluginConfig.Instance.userId}/0")).Content.ReadAsStringAsync());

            foreach (var token in jArrayResponse)
            {
                if (!PluginConfig.Instance.savedFollowDictionary.ContainsKey($"{token.Value<string>("id")}"))
                {
                    PluginConfig.Instance.savedFollowDictionary.Add($"{token.Value<string>("id")}", DateTime.Now);
                }
            }
        }
        
        private async Task CheckBeatSaverAsync()
        {
            var maps = new List<Beatmap>();

            var listOfMappers = PluginConfig.Instance.savedFollowDictionary;

            if (listOfMappers != null)
            {
                foreach (var mapper in listOfMappers)
                {
                    var allPages = await getAllRequiredPagesFromUser(mapper);

                    foreach (var page in allPages)
                    foreach (var map in page.Beatmaps)
                        if (map.Uploaded > mapper.Value) maps.Add(map);
                }
            }
            
            OnBeatSaverCheck?.Invoke(maps);
        }

        private async Task<List<Page>> getAllRequiredPagesFromUser(KeyValuePair<string, DateTime> user)
        {
            var returnList = new List<Page>();

            var mapper = await _beatSaver.User(user.Key);

            if (mapper == null) return returnList;

            var currentPageIndex = 0;
            var currentBeatmapPage = await mapper.Beatmaps(currentPageIndex);

            while (currentBeatmapPage != null && currentBeatmapPage.Beatmaps.Last().Uploaded > user.Value)
            {
                returnList.Add(currentBeatmapPage);
                currentPageIndex++;
                currentBeatmapPage = await mapper.Beatmaps(currentPageIndex);
            }

            return returnList;
        }
    }
}