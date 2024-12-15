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
        
        public event Action<List<Beatmap>> OnBeatSaverCheck;

        public BeatSaverChecker(SiraLog logger, OAuthApi oAuthApi)
        {
            _logger = logger;
            _oAuthApi = oAuthApi;
        }

        private long parseUnixTimestamp(DateTime dateTime) => ((DateTimeOffset) dateTime).ToUnixTimeSeconds();

        public static Sprite createSpriteFromImageBuffer(byte[] buffer)
        {
            var tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, buffer);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }
        
        public async Task CheckBeatSaverAsync()
        {
            if (PluginConfig.Instance.firstCheckUnixTimeStamp == -1)
                PluginConfig.Instance.firstCheckUnixTimeStamp = ((DateTimeOffset) DateTime.Now).ToUnixTimeSeconds();
            
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
                Plugin.Log.Info("erm sending a request!");

                if (!response.IsSuccessStatusCode) throw new Exception("Failed to fetch page with status code " + (int) response.StatusCode);
                
                var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
                
                var mapArray = JArray.Parse(responseJson.GetValue("docs")?.ToString() ?? throw new Exception());
                
                foreach (var mapJToken in mapArray)
                {
                    /*
                    var map = await _beatSaver.Beatmap(mapJToken["id"]?.Value<string>() 
                                                       ?? throw new Exception("Map does not contain ID"));
                    if (parseUnixTimestamp(map?.Uploaded ?? throw new Exception("Map does not have a DateTime")) > PluginConfig.Instance.firstCheckUnixTimeStamp)
                    {
                        if (mapAlreadyDownloaded(map)) continue;
                        
                        maps.Add(map);
                    }
                    else return maps;
                    */
                    
                    var map = await _beatSaver.Beatmap(mapJToken["id"]?.Value<string>() 
                                                       ?? throw new Exception("Map does not contain ID"));
                    Plugin.Log.Info($"{map?.ID} uploaded {map?.Uploaded}");
                    if (mapAlreadyDownloaded(map)) continue;
                    if (parseUnixTimestamp(map?.Uploaded ?? throw new Exception("Map does not have an uploaded DateTime")) < PluginConfig.Instance.firstCheckUnixTimeStamp) 
                        return maps;
                    
                    maps.Add(map);
                }
                page++;
            } while (true);
        }

        private bool mapAlreadyDownloaded(Beatmap beatmap)
        {
            foreach (var mapHash in beatmap.Versions.Select(i => i.Hash))
            {
                if (Loader.GetLevelByHash(mapHash) != null) return true;
            }
            
            return false;
        }
    }
}