using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using IPA.Utilities;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.BeatSaver
{
    public class MapQueueManager : ITickable
    {
        private readonly SiraLog _logger;
        
        private readonly HttpClient _httpClient = new HttpClient();
        
        private readonly List<Beatmap> mapQueue = new List<Beatmap>();
        public Action finishedDownloadingQueue;
        
        private bool _queueIsDownloading = false;

        public MapQueueManager(SiraLog logger)
        {
            _logger = logger;
        }

        public async Task addMapToQueue(Beatmap beatmap)
        {
            mapQueue.Add(beatmap);

            if (!_queueIsDownloading) await startSongDownloading();
        }

        private async Task startSongDownloading()
        {
            while (true)
            {
                _queueIsDownloading = true;

                var tempQueue = mapQueue; // compiler will bitch if this isnt here so just loop in a while true until the queue is empty

                foreach (var beatmap in tempQueue)
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(beatmap.LatestVersion.DownloadURL);
                        var content = await response.Content.ReadAsByteArrayAsync();

                        var zippedZipArchive = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Update);

                        zippedZipArchive.ExtractToDirectory(Path.Combine(UnityGame.InstallPath, "Beat Saber_Data", "CustomLevels", $"{beatmap.ID} ({beatmap.Metadata.SongName} - {beatmap.Metadata.LevelAuthorName})"));
                        mapQueue.Remove(beatmap);
                    }
                    catch (Exception exception)
                    {
                        _logger.Error($"Failed to download beatmap {beatmap.ID}: {exception}");
                    }
                }

                if (mapQueue.Count > 0)
                    continue;
                _queueIsDownloading = false;
                finishedDownloadingQueue?.Invoke();

                break;
            }
            SongCore.Loader.Instance.RefreshSongs(false);
        }

        public void Tick()
        {
            
        }
    }
}