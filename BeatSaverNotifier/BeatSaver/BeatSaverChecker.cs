using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class BeatSaverChecker
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
        }

        private DateTime parseDateTime(string dateTime) => DateTime.ParseExact(PluginConfig.Instance.firstCheckTime,
            "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None);
        
        public async Task CheckBeatSaverAsync()
        {
            if (string.IsNullOrEmpty(PluginConfig.Instance.firstCheckTime)) PluginConfig.Instance.firstCheckTime = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);
            var maps = await getPagesUntilPastFirstCheckDateTime();
            
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
                Plugin.Log.Info("erm i just sent a request");
                if (!response.IsSuccessStatusCode) throw new Exception("Failed to fetch page with status code " + (int) response.StatusCode);
                
                var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
                
                var mapArray = JArray.Parse(responseJson.GetValue("docs")?.ToString() ?? throw new Exception());
                
                foreach (var mapJToken in mapArray)
                {
                    var map = await _beatSaver.Beatmap(mapJToken["id"]?.Value<string>() 
                                                       ?? throw new Exception("Map does not contain ID"));
                    if (map?.Uploaded > parseDateTime(PluginConfig.Instance.firstCheckTime))
                    {
                        maps.Add(map);
                        Plugin.Log.Info(map.ID + " uploaded " + map.Uploaded);
                        page++;
                    }
                    else
                    {
                        Plugin.Log.Info("erm no more requests");
                        return maps;
                    }
                }
            } while (true);
        }
    }
}