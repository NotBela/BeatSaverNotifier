using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.BeatSaver.Models;
using BeatSaverNotifier.Configuration;
using BeatSaverNotifier.UI.FlowCoordinators;
using HMUI;
using SiraUtil.Logging;
using SongCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaverNotifier.UI.BSML.MapListScreen
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.MapListScreen.BeatSaverNotifierView.bsml")]
    internal class BeatSaverNotifierViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private BeatSaverChecker _beatSaverChecker;
        private SiraLog _logger;
        private OAuthApi _oAuthApi;
        private MapQueueManager _mapQueueManager;
        private BeatSaverNotifierFlowCoordinator flowCoordinator;
        
        internal List<BeatmapModel> _beatmapsInList = [];
        private BeatmapModel _selectedBeatmap;
        
        public bool areMapsInQueue => _beatmapsInList.Any();
        
        [UIParams] private BSMLParserParams parserParams = null;
        
        [UIComponent("mapList")]
        private readonly CustomListTableData customListTableData = null;
        
        [Inject]
        public void Inject(SiraLog siraLog, BeatSaverChecker beatSaverChecker, OAuthApi oAuthApi, MapQueueManager mapQueueManager, BeatSaverNotifierFlowCoordinator flowCoordinator)
        {
            this._logger = siraLog;
            this._beatSaverChecker = beatSaverChecker;
            this._oAuthApi = oAuthApi;
            this._mapQueueManager = mapQueueManager;
            this.flowCoordinator = flowCoordinator;
        }
        
        [UIComponent("rightPanelContainer")] private readonly HorizontalLayoutGroup _rightPanelContainer = null;
        
        [UIComponent("songNameText")] private readonly TextMeshProUGUI mapNameText = null;
        [UIComponent("songAuthorText")] private readonly TextMeshProUGUI songAuthorText = null;
        [UIComponent("songSubNameText")] private readonly TextMeshProUGUI songSubNameText = null;
        [UIComponent("coverArtImage")] private readonly Image coverArtImage = null;
        
        [UIComponent("descriptionText")] private readonly TextMeshProUGUI descriptionText = null;
        
        [UIComponent("downloadButton")] private readonly Button downloadButton = null;
        [UIComponent("ignoreButton")] private readonly Button ignoreButton = null;

        [UIAction("ignoreButtonOnClick")]
        private void IgnoreButtonOnClick()
        {
            if (!PluginConfig.Instance.keysToIgnore.Contains(_selectedBeatmap.Id)) 
                PluginConfig.Instance.keysToIgnore.Add(_selectedBeatmap.Id);
            
            var idx = _beatmapsInList.IndexOf(_selectedBeatmap);
            
            _rightPanelContainer.gameObject.SetActive(false);

            if (idx == -1) return;
            _selectedBeatmap = null;
            _beatmapsInList.RemoveAt(idx);
            customListTableData.Data.RemoveAt(idx);
            customListTableData.TableView.ReloadData();
        }
        
        [UIAction("downloadButtonOnClick")]
        private async void DownloadButtonOnClick()
        {
            try
            {
                downloadButton.SetButtonText("Downloading...");
                downloadButton.interactable = false;
                ignoreButton.interactable = false;

                customListTableData.Data.RemoveAt(_beatmapsInList.IndexOf(_selectedBeatmap));
                _beatmapsInList.Remove(_selectedBeatmap);
                customListTableData.TableView.ReloadData();
                customListTableData.TableView.ClearSelection();
                
                await _mapQueueManager.addMapToQueue(_selectedBeatmap);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                showErrorModal();
            }
        }

        [UIAction("errorModalOkButtonOnClick")]
        private void errorModalOkButtonOnClick() => parserParams.EmitEvent("errorModalHide");
        
        private void showErrorModal() => parserParams.EmitEvent("errorModalShow");
        
        [UIAction("refreshButtonOnClick")]
        private async void refreshButtonOnClick()
        {
            try
            {
                _rightPanelContainer.gameObject.SetActive(false);
                customListTableData.TableView.ClearSelection();
                await _beatSaverChecker.CheckBeatSaverAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                showErrorModal();
            }
        }
        
        [UIAction("#post-parse")]
        void postParse()
        {
            coverArtImage.material = Resources.FindObjectsOfTypeAll<Material>()
                .FirstOrDefault(m => m.name == "UINoGlowRoundEdge");
            descriptionText.enableWordWrapping = true;
            
            _beatmapsInList = _beatSaverChecker.cachedMaps.ToList();
            ReloadTableData();
        }

        [UIAction("onCellSelect")]
        private void onCellSelected(TableView tableView, int index)
        {
            try
            {
                _selectedBeatmap = _beatmapsInList[index];

                _rightPanelContainer.gameObject.SetActive(true);
                
                songSubNameText.text = _selectedBeatmap.SongSubName;
                mapNameText.text = $"{_selectedBeatmap.SongName}";
                songAuthorText.text = _selectedBeatmap.Author;
                
                descriptionText.text = _selectedBeatmap.Description;

                bool mapIsQueuedOrDownloaded = _mapQueueManager.readOnlyQueue.Contains(_selectedBeatmap) || Loader.GetLevelByHash(_selectedBeatmap.VersionHashes[0]) != null;
                downloadButton.interactable = !mapIsQueuedOrDownloaded;
                ignoreButton.interactable = !mapIsQueuedOrDownloaded;
                downloadButton.SetButtonText(mapIsQueuedOrDownloaded ? "Downloading..." : "Download");
                
                coverArtImage.sprite = _selectedBeatmap.CoverSprite;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                showErrorModal();
            }
        }
        
        private void OnBeatSaverCheckFinished(List<BeatmapModel> mapList)
        { 
            flowCoordinator.switchToView(BeatSaverNotifierFlowCoordinator.FlowState.MapList); 
            _beatmapsInList = mapList;
            ReloadTableData();
        }
        
        public void ReloadTableData()
        {
            if (customListTableData == null) return;
            
            customListTableData.Data = _beatmapsInList.Select(i => i.getCustomListCellInfo()).ToList();
            customListTableData.TableView.ReloadData();
        }

        public async void Initialize()
        {
            try
            {
                _beatSaverChecker.OnBeatSaverCheckFinished += OnBeatSaverCheckFinished;
            
                await _beatSaverChecker.CheckBeatSaverAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public void Dispose()
        {
            _beatSaverChecker.OnBeatSaverCheckFinished -= OnBeatSaverCheckFinished;
        }
    }
}