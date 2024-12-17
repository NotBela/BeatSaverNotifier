using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Tags;
using BeatSaberMarkupLanguage.TypeHandlers;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverSharp.Models;
using HMUI;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.UI.BSML
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.MapQueueView.bsml")]
    public class MapQueueViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private MapQueueManager _mapQueueManager;
        private SiraLog _logger;
        
        [UIComponent("queueList")] private CustomListTableData _queueList = null;
        
        [UIAction("onCellSelect")] private void onCellSelect(TableView tableView, int row){} // do nothing

        [UIAction("#post-parse")]
        async void postParse()
        {
            try
            {
                var data = new List<CustomListTableData.CustomCellInfo>();
                foreach (var map in _mapQueueManager.readOnlyQueue)
                {
                    data = data.Prepend(await getCustomCellInfo(map)).ToList();
                }

                _queueList.Data = data;
                _queueList.TableView.ReloadData();
                // make list not interactable here
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
        
        private async Task<CustomListTableData.CustomCellInfo> getCustomCellInfo(Beatmap i) => new CustomListTableData.CustomCellInfo(
            i.Name, 
            _mapQueueManager.CurrentlyDownloadingBeatmap == i ? "Downloading..." : "In queue", 
            BeatSaverChecker.createSpriteFromImageBuffer(await i.LatestVersion.DownloadCoverImage())
        );
        
        [Inject]
        void Inject(MapQueueManager mapQueueManager, SiraLog logger)
        {
            _mapQueueManager = mapQueueManager;
            _logger = logger;
        }

        private void mapAddedToQueue(Beatmap beatmap, byte[] songCoverArt)
        {
            _queueList.Data.Add(new CustomListTableData.CustomCellInfo(beatmap.Name, "In queue", BeatSaverChecker.createSpriteFromImageBuffer(songCoverArt)));
            _queueList.TableView.ReloadData();
        }

        private void onDownloadStarted(Beatmap beatmap)
        {
            if (_mapQueueManager.readOnlyQueue.IndexOf(beatmap) == -1) return;
            
            _queueList.Data[_mapQueueManager.readOnlyQueue.IndexOf(beatmap)].Subtext = "Downloading...";
            _queueList.TableView.ReloadData();
        }

        private void onDownloadFinished(Beatmap beatmap, int indexToRemove, bool wasSuccessful)
        {
            _queueList.Data.RemoveAt(indexToRemove);
            _queueList.TableView.ReloadData();
        }
        
        public void Initialize()
        {
            _mapQueueManager.downloadStarted += onDownloadStarted;
            _mapQueueManager.downloadFinished += onDownloadFinished;
            _mapQueueManager.mapAddedToQueue += mapAddedToQueue;
        }

        public void Dispose()
        {
            _mapQueueManager.downloadStarted -= onDownloadStarted;
            _mapQueueManager.downloadFinished -= onDownloadFinished;
            _mapQueueManager.mapAddedToQueue -= mapAddedToQueue;
        }
    }
}