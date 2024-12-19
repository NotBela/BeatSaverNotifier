using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.BeatSaver.Models;
using BeatSaverNotifier.Configuration;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using BeatSaverSharp.Models.Pages;
using Newtonsoft.Json.Linq;
using SiraUtil.Logging;
using SongCore;
using UnityEngine;
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
        
        public event Action<List<BeatmapModel>> OnBeatSaverCheckFinished;
        public event Action onBeatSaverCheckStarted;
        
        public bool isChecking { get; private set; }
        public ReadOnlyCollection<BeatmapModel> cachedMaps { get; private set; } = new ReadOnlyCollection<BeatmapModel>(Array.Empty<BeatmapModel>());

        public BeatSaverChecker(SiraLog logger, OAuthApi oAuthApi)
        {
            _logger = logger;
            _oAuthApi = oAuthApi;
        }

        private long parseUnixTimestamp(DateTime dateTime) => ((DateTimeOffset) dateTime).ToUnixTimeSeconds();
        
        public async Task CheckBeatSaverAsync()
        {
            onBeatSaverCheckStarted?.Invoke();
            isChecking = true;
            if (PluginConfig.Instance.firstCheckUnixTimeStamp == -1)
                PluginConfig.Instance.firstCheckUnixTimeStamp = ((DateTimeOffset) DateTime.Now).ToUnixTimeSeconds();
            
            var maps = await getPagesUntilPastFirstCheckDateTime();
            
            OnBeatSaverCheckFinished?.Invoke(maps);
            cachedMaps = maps.AsReadOnly();
            isChecking = false;
        }

        private async Task<List<BeatmapModel>> getPagesUntilPastFirstCheckDateTime()
        {
            var maps = new List<BeatmapModel>();
            var page = 0;

            do
            {
                try
                {
                    var request = await _oAuthApi.addAuthenticationToRequest(new HttpRequestMessage
                    {
                        RequestUri =
                            new Uri(
                                $"https://api.beatsaver.com/search/text/{page}?followed=true&leaderboard=All&pageSize=20&q=&sortOrder=Latest")
                    });

                    var response = await _httpClient.SendAsync(request);
                    // Plugin.Log.Info("erm sending a request!");

                    if (!response.IsSuccessStatusCode)
                        throw new Exception("Failed to fetch page with status code " + (int)response.StatusCode);

                    var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());

                    var mapArray = JArray.Parse(responseJson.GetValue("docs")?.ToString() ?? throw new Exception());

                    foreach (var mapJToken in mapArray)
                    {
                        try
                        {
                            if (PluginConfig.Instance.keysToIgnore.Contains(mapJToken["id"]?.Value<string>() ?? throw new Exception("Map does not contain ID"))) 
                                continue;

                            var map = await BeatmapModel.Parse(mapJToken.ToString());

                            if (mapAlreadyDownloaded(map)) continue;
                            if (parseUnixTimestamp(map.UploadDate) < PluginConfig.Instance.firstCheckUnixTimeStamp)
                                return maps;

                            maps.Add(map);
                        }
                        catch (Exception e)
                        {
                            _logger.Error($"Could not fetch map data: {e}");
                        }
                    }

                    page++;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("429"))
                    {
                        _logger.Info("Rate limit reached! Please consider adding maps to your ignore list.");
                        await Task.Delay(10000);
                    }
                    else throw;
                }
            } while (true);
        }

        private bool mapAlreadyDownloaded(BeatmapModel beatmap)
        {
            foreach (var mapHash in beatmap.VersionHashes)
            {

                if (Loader.GetLevelByHash(mapHash) != null)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}