using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BeatSaverNotifier.BeatSaver.Auth;
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
        private readonly OAuthApi _oAuthApi;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly SiraLog _logger;
        
        public event Action<List<Beatmap>> OnBeatSaverCheck;

        public BeatSaverChecker(SiraLog logger, OAuthApi oAuthApi)
        {
            _logger = logger;
            _oAuthApi = oAuthApi;
            
            _httpClient.BaseAddress = new Uri("https://api.beatsaver.com");
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
            var maps = new List<Beatmap>();

            var request = await _oAuthApi.addAuthenticationToRequest(new HttpRequestMessage
            {
                RequestUri = new Uri("https://api.beatsaver.com/search/text/0?followed=true&leaderboard=All&pageSize=20&q=&sortOrder=Latest")
            });
            
            var response = await _httpClient.SendAsync(request);
            
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