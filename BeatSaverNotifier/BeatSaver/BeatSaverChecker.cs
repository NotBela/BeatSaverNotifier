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
                if (PluginConfig.Instance.refreshToken == null) return;
                await CheckBeatSaverAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
        
        private async Task CheckBeatSaverAsync()
        {
            var maps = await getPagesUntilPastFirstCheckDateTime();
            
            if (PluginConfig.Instance.firstCheckTime == null) PluginConfig.Instance.firstCheckTime = DateTime.Now;
            
            OnBeatSaverCheck?.Invoke(maps);
        }

        private async Task<List<Beatmap>> getPagesUntilPastFirstCheckDateTime()
        {
            var maps = new List<Beatmap>();
            var page = 0;

            do
            {
                var request = await _oAuthApi.addAuthenticationToRequest(new HttpRequestMessage
                {
                    RequestUri = new Uri($"https://api.beatsaver.com/search/text/{page}?followed=true&leaderboard=All&pageSize=20&q=&sortOrder=Latest")
                });
                
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode) throw new Exception("Failed to fetch page with status code " + (int) response.StatusCode);
                
                var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
                var mapArray = JArray.Parse(responseJson["docs"]?.Value<string>() ?? throw new Exception("response content does not contain docs value"));

                foreach (var mapJToken in mapArray)
                {
                    var map = await _beatSaver.Beatmap(mapJToken["id"]?.Value<string>() 
                                                      ?? throw new Exception("Map does not contain ID"));
                    if (map?.Uploaded > PluginConfig.Instance.firstCheckTime)
                    {
                        maps.Add(map);
                        page++;
                    }
                    else return maps;

                }
            } while (true);
        }
    }
}