using System;
using System.Collections.Concurrent;
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
using IPA.Config.Data;
using Newtonsoft.Json.Linq;
using SiraUtil.Logging;
using SongCore;
using UnityEngine;
using Zenject;

namespace BeatSaverNotifier.BeatSaver
{
    public class BeatSaverChecker : IInitializable, IDisposable
    {
        [Inject] private readonly OAuthApi _oAuthApi = null;
        [Inject] private readonly SiraLog _logger = null;
        
        private readonly HttpClient _httpClient = new();
        
        public event Action<List<BeatmapModel>> OnBeatSaverCheckFinished;
        public event Action onBeatSaverCheckStarted;
        public bool IsChecking { get; private set; }

        private List<BeatmapModel> _cachedMaps = new();
        public ReadOnlyCollection<BeatmapModel> CachedMaps => _cachedMaps.AsReadOnly();

        public void removeFromCachedMaps(BeatmapModel beatmap) => _cachedMaps.Remove(beatmap);

        public static long parseUnixTimestamp(DateTime dateTime) => ((DateTimeOffset) dateTime).ToUnixTimeSeconds();
        
        public async Task CheckBeatSaverAsync(bool silentLoadingScreen = false)
        {
            if (!PluginConfig.Instance.isSignedIn) return;
            if (IsChecking) return;
            
            _logger.Info("Checking BeatSaver...");
            
            IsChecking = true;
            if (!silentLoadingScreen) onBeatSaverCheckStarted?.Invoke();
            
            if (PluginConfig.Instance.firstCheckUnixTimeStamp == -1)
                PluginConfig.Instance.firstCheckUnixTimeStamp = parseUnixTimestamp(IPA.Utilities.Utils.CurrentTime());
            
            var maps = await getPagesUntilPastFirstCheckDateTime();

            IsChecking = false;
            _cachedMaps = maps;
            OnBeatSaverCheckFinished?.Invoke(maps);
            
            _logger.Info($"BeatSaver check finished, {maps.Count} maps fetched.");
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

        public static bool mapAlreadyDownloaded(BeatmapModel beatmap) => beatmap.VersionHashes.Any(mapHash => Loader.GetLevelByHash(mapHash) != null);

        public void Initialize()
        {
            Loader.SongsLoadedEvent += onSongsLoaded;
        }

        private async void onSongsLoaded(Loader arg1, ConcurrentDictionary<string, BeatmapLevel> arg2)
        {
            try
            {
                await CheckBeatSaverAsync();
                Loader.SongsLoadedEvent -= onSongsLoaded; // unsubscribe after first load so it doesnt refresh every single songloadedevent
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public void Dispose()
        {
            Loader.SongsLoadedEvent -= onSongsLoaded;
        }
    }
}