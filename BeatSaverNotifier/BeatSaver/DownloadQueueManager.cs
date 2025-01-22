using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaverNotifier.BeatSaver.Models;
using IPA.Utilities;
using ModestTree;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.BeatSaver
{
    public class DownloadQueueManager
    {
        [Inject] private readonly SiraLog _logger = null;
        
        private readonly HttpClient _httpClient = new();

        public event Action<BeatmapModel> mapAddedToQueue;
        public event Action<BeatmapModel> downloadStarted;
        public event Action<BeatmapModel, int> downloadFinished;
        
        private readonly List<BeatmapModel> mapQueue = new();
        
        private bool _queueIsDownloading = false;
        public ReadOnlyCollection<BeatmapModel> readOnlyQueue => mapQueue.AsReadOnly();

        public async Task addMapToQueue(BeatmapModel beatmap)
        {
            mapQueue.Add(beatmap);
            mapAddedToQueue?.Invoke(beatmap);

            if (!_queueIsDownloading) await startSongDownloading();
        }

        private async Task startSongDownloading()
        {
            while (true)
            {
                _queueIsDownloading = true;

                var tempQueue = new List<BeatmapModel>(mapQueue);

                foreach (var beatmap in tempQueue)
                {
                    var idx = mapQueue.IndexOf(beatmap);
                    try
                    {
                        downloadStarted?.Invoke(beatmap);
                        var response = await _httpClient.GetAsync(new Uri(beatmap.DownloadUrl, UriKind.Absolute));
                        var content = await response.Content.ReadAsByteArrayAsync();

                        var zippedZipArchive = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Update);

                        zippedZipArchive.ExtractToDirectory(Path.Combine(UnityGame.InstallPath, 
                            "Beat Saber_Data", 
                            "CustomLevels", 
                            Path.GetInvalidFileNameChars().Aggregate($"{beatmap.Key} ({beatmap.SongName} - {beatmap.Mappers.Join(", ")})", (current, illegalChar) => current.Replace(illegalChar.ToString(), ""))));
                        
                        mapQueue.Remove(beatmap);
                        downloadFinished?.Invoke(beatmap, idx);
                    }
                    catch (Exception exception)
                    {
                        if (tempQueue.Contains(beatmap)) tempQueue.Remove(beatmap);
                        if (mapQueue.Contains(beatmap)) tempQueue.Remove(beatmap);
                        _logger.Error($"Failed to download beatmap {beatmap.Key}: {exception}");
                    }
                }

                if (mapQueue.Count > 0)
                    continue;
                _queueIsDownloading = false;

                break;
            }
            SongCore.Loader.Instance.RefreshSongs(false);
        }
    }
}