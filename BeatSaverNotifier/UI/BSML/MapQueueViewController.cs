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
using BeatSaverNotifier.BeatSaver.Models;
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
        void postParse()
        {
            try
            {

                _queueList.Data = _mapQueueManager.readOnlyQueue.Select(getCustomCellInfo).ToList();
                _queueList.TableView.ReloadData();
                // make list not interactable here
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
        
        private CustomListTableData.CustomCellInfo getCustomCellInfo(BeatmapModel i) => new CustomListTableData.CustomCellInfo(
            i.UploadName, 
            _mapQueueManager.CurrentlyDownloadingBeatmap == i ? "Downloading..." : "In queue", 
            BeatSaverChecker.createSpriteFromImageBuffer(i.Cover)
        );
        
        [Inject]
        void Inject(MapQueueManager mapQueueManager, SiraLog logger)
        {
            _mapQueueManager = mapQueueManager;
            _logger = logger;
        }

        private void mapAddedToQueue(BeatmapModel beatmap)
        {
            _queueList.Data.Add(new CustomListTableData.CustomCellInfo(beatmap.UploadName, "In queue", BeatSaverChecker.createSpriteFromImageBuffer(beatmap.Cover)));
            _queueList.TableView.ReloadData();
        }

        private void onDownloadStarted(BeatmapModel beatmap)
        {
            if (_mapQueueManager.readOnlyQueue.IndexOf(beatmap) == -1) return;
            
            _queueList.Data[_mapQueueManager.readOnlyQueue.IndexOf(beatmap)].Subtext = "Downloading...";
            _queueList.TableView.ReloadData();
        }

        private void onDownloadFinished(BeatmapModel beatmap, int indexToRemove, bool wasSuccessful)
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