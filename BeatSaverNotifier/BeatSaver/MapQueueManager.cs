using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using IPA.Utilities;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.BeatSaver
{
    public class MapQueueManager
    {
        private readonly SiraLog _logger;
        
        private readonly HttpClient _httpClient = new HttpClient();

        public event Action<Beatmap, byte[]> mapAddedToQueue;
        public event Action<Beatmap> downloadStarted;
        public event Action<Beatmap, int, bool> downloadFinished;
        
        private readonly List<Beatmap> mapQueue = new List<Beatmap>();
        
        private bool _queueIsDownloading = false;
        
        public ReadOnlyCollection<Beatmap> readOnlyQueue => mapQueue.AsReadOnly();
        
        private Beatmap _currentlyDownloadingBeatmap;
        public Beatmap CurrentlyDownloadingBeatmap => _currentlyDownloadingBeatmap;

        public MapQueueManager(SiraLog logger)
        {
            _logger = logger;
        }

        public async Task addMapToQueue(Beatmap beatmap, byte[] cachedSongCover)
        {
            mapQueue.Add(beatmap);
            mapAddedToQueue?.Invoke(beatmap, cachedSongCover);

            if (!_queueIsDownloading) await startSongDownloading();
        }

        private async Task startSongDownloading()
        {
            while (true)
            {
                _queueIsDownloading = true;

                var tempQueue = new List<Beatmap>(mapQueue); // compiler will bitch if this isnt here so just loop in a while true until the queue is empty

                foreach (var beatmap in tempQueue)
                {
                    var idx = mapQueue.IndexOf(beatmap);
                    try
                    {
                        _currentlyDownloadingBeatmap = beatmap;
                        downloadStarted?.Invoke(beatmap);
                        var response = await _httpClient.GetAsync(beatmap.LatestVersion.DownloadURL);
                        var content = await response.Content.ReadAsByteArrayAsync();

                        var zippedZipArchive = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Update);

                        zippedZipArchive.ExtractToDirectory(Path.Combine(UnityGame.InstallPath, 
                            "Beat Saber_Data", 
                            "CustomLevels", 
                            Path.GetInvalidFileNameChars().Aggregate($"{beatmap.ID} ({beatmap.Metadata.SongName} - {beatmap.Metadata.LevelAuthorName})", (current, illegalChar) => current.Replace(illegalChar.ToString(), ""))));
                        
                        mapQueue.Remove(beatmap);
                        _currentlyDownloadingBeatmap = null;
                        downloadFinished?.Invoke(beatmap, idx, false);
                    }
                    catch (Exception exception)
                    {
                        downloadFinished?.Invoke(beatmap, idx, false);
                        _logger.Error($"Failed to download beatmap {beatmap.ID}: {exception}");
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