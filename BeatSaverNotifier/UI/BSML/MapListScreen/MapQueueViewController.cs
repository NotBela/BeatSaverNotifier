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
using HMUI;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaverNotifier.UI.BSML
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.MapListScreen.MapQueueView.bsml")]
    public class MapQueueViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private DownloadQueueManager _downloadQueueManager;
        private SiraLog _logger;
        
        [UIComponent("queueList")] private readonly CustomListTableData _queueList = null;

        [UIAction("onCellSelect")]
        private void onCellSelect(TableView tableView, int row) => tableView.ClearSelection();

        [UIAction("#post-parse")]
        void postParse()
        {
            _queueList.Data = _downloadQueueManager.readOnlyQueue.Select(i => i.getCustomListCellInfo(true)).ToList();
            _queueList.TableView.ReloadData();
        }
        
        [Inject]
        void Inject(DownloadQueueManager downloadQueueManager, SiraLog logger)
        {
            _downloadQueueManager = downloadQueueManager;
            _logger = logger;
        }

        private void mapAddedToQueue(BeatmapModel beatmap)
        {
            _queueList.Data.Add(beatmap.getCustomListCellInfo(true));
            _queueList.TableView.ReloadData();
        }

        private void onDownloadStarted(BeatmapModel beatmap)
        {
            if (_downloadQueueManager.readOnlyQueue.Count == 0) return;
            if (_downloadQueueManager.readOnlyQueue.IndexOf(beatmap) == -1) return;
            
            _queueList.Data[_downloadQueueManager.readOnlyQueue.IndexOf(beatmap)].Subtext = "Downloading...";
            _queueList.TableView.ReloadData();
        }

        private void onDownloadFinished(BeatmapModel beatmap, int indexToRemove)
        {
            _queueList.Data.RemoveAt(indexToRemove);
            _queueList.TableView.ReloadData();
        }
        
        public void Initialize()
        {
            _downloadQueueManager.downloadStarted += onDownloadStarted;
            _downloadQueueManager.downloadFinished += onDownloadFinished;
            _downloadQueueManager.mapAddedToQueue += mapAddedToQueue;
        }

        public void Dispose()
        {
            _downloadQueueManager.downloadStarted -= onDownloadStarted;
            _downloadQueueManager.downloadFinished -= onDownloadFinished;
            _downloadQueueManager.mapAddedToQueue -= mapAddedToQueue;
        }
    }
}