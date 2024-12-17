using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.Configuration;
using BeatSaverSharp.Models;
using HMUI;
using SiraUtil.Logging;
using SongCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaverNotifier.UI.BSML
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.BeatSaverNotifierView.bsml")]
    [HotReload(RelativePathToLayout = @"../UI/BSML/BeatSaverNotifierView.bsml")]
    internal class BeatSaverNotifierViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private BeatSaverChecker _beatSaverChecker;
        private SiraLog _logger;
        private OAuthApi _oAuthApi;
        private MapQueueManager _mapQueueManager;
        
        private List<Beatmap> _beatmapsInList;
        private Beatmap _selectedBeatmap;
        private byte[] cachedImageCoverArt;
        
        [UIParams] private BSMLParserParams parserParams = null;
        
        [UIComponent("mapList")]
        private readonly CustomListTableData customListTableData = null;
        
        [Inject]
        public void Inject(SiraLog siraLog, BeatSaverChecker beatSaverChecker, OAuthApi oAuthApi, MapQueueManager mapQueueManager)
        {
            this._logger = siraLog;
            this._beatSaverChecker = beatSaverChecker;
            this._oAuthApi = oAuthApi;
            this._mapQueueManager = mapQueueManager;
        }
        
        [UIComponent("rightPanelContainer")] private readonly HorizontalLayoutGroup _rightPanelContainer = null;
        [UIComponent("mapListContainer")] private readonly VerticalLayoutGroup _mapListContainer = null;
        [UIComponent("loadingContainer")] private readonly VerticalLayoutGroup _loadingContainer = null;
        
        [UIComponent("songNameText")] private readonly TextMeshProUGUI mapNameText = null;
        [UIComponent("songAuthorText")] private readonly TextMeshProUGUI songAuthorText = null;
        [UIComponent("songSubNameText")] private readonly TextMeshProUGUI songSubNameText = null;
        [UIComponent("coverArtImage")] private readonly Image coverArtImage = null;
        
        [UIComponent("downloadButton")] private readonly Button downloadButton = null;
        [UIComponent("ignoreButton")] private readonly Button ignoreButton = null;

        [UIAction("ignoreButtonOnClick")]
        private void IgnoreButtonOnClick()
        {
            if (!PluginConfig.Instance.keysToIgnore.Contains(_selectedBeatmap.ID)) 
                PluginConfig.Instance.keysToIgnore.Add(_selectedBeatmap.ID);
            
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
                customListTableData.TableView.ReloadData();
                customListTableData.TableView.ClearSelection();
                
                await _mapQueueManager.addMapToQueue(_selectedBeatmap, cachedImageCoverArt);
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
                await _beatSaverChecker.CheckBeatSaverAsync();
                showErrorModal();
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
        }

        [UIAction("onCellSelect")]
        private async void onCellSelected(TableView tableView, int index)
        {
            try
            {
                _selectedBeatmap = _beatmapsInList[index];

                _rightPanelContainer.gameObject.SetActive(true);
                
                songSubNameText.text = _selectedBeatmap.Metadata.SongSubName;
                mapNameText.text = $"{_selectedBeatmap.Metadata.SongName}";
                songAuthorText.text = _selectedBeatmap.Metadata.SongAuthorName;

                bool mapIsQueuedOrDownloaded = _mapQueueManager.readOnlyQueue.Contains(_selectedBeatmap) || Loader.GetLevelByHash(_selectedBeatmap.LatestVersion.Hash) != null;
                downloadButton.interactable = !mapIsQueuedOrDownloaded;
                ignoreButton.interactable = !mapIsQueuedOrDownloaded;
                downloadButton.SetButtonText(mapIsQueuedOrDownloaded ? "Downloading..." : "Download");
                
                var downloadedImage = await _selectedBeatmap.LatestVersion.DownloadCoverImage();
                cachedImageCoverArt = downloadedImage;
                coverArtImage.sprite = BeatSaverChecker.createSpriteFromImageBuffer(downloadedImage);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                showErrorModal();
            }
        }

        private void OnBeatSaverCheckStarted()
        {
            if (_rightPanelContainer != null) _rightPanelContainer.gameObject.SetActive(false);
            if (_loadingContainer != null) _loadingContainer.gameObject.SetActive(true);
            if (_mapListContainer != null) _mapListContainer.gameObject.SetActive(false);
        }
        
        private async void OnBeatSaverCheckFinished(List<Beatmap> mapList)
        {
            try
            {
                this._beatmapsInList = mapList;
                
                var mapTableData = new List<CustomListTableData.CustomCellInfo>();
                foreach (var map in mapList)
                {
                    var customData = await getCustomListCellData(map);
                    mapTableData.Add(customData);
                }
                
                if (customListTableData.Data != null) customListTableData.Data = mapTableData;
                customListTableData.TableView.ReloadData();
                
                _loadingContainer.gameObject.SetActive(false);
                _mapListContainer.gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                showErrorModal();
            }
        }

        private async Task<CustomListTableData.CustomCellInfo> getCustomListCellData(Beatmap beatmap)
        {
            var imgBytes = await beatmap.LatestVersion.DownloadCoverImage();
            
            return new CustomListTableData.CustomCellInfo(
                beatmap.Name, 
                beatmap.Metadata.LevelAuthorName, 
                BeatSaverChecker.createSpriteFromImageBuffer(imgBytes));
        }

        public async void Initialize()
        {
            try
            {
                _beatSaverChecker.OnBeatSaverCheckFinished += OnBeatSaverCheckFinished;
                _beatSaverChecker.onBeatSaverCheckStarted += OnBeatSaverCheckStarted;
                
                await _beatSaverChecker.CheckBeatSaverAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                showErrorModal();
            }
        }

        public void Dispose()
        {
            _beatSaverChecker.OnBeatSaverCheckFinished -= OnBeatSaverCheckFinished;
            _beatSaverChecker.onBeatSaverCheckStarted -= OnBeatSaverCheckStarted;
        }
    }
}