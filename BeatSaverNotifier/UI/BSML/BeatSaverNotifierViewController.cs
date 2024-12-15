using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.Configuration;
using BeatSaverSharp.Models;
using HMUI;
using SiraUtil.Logging;
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
        
        [UIComponent("songNameText")] private readonly TextMeshProUGUI mapNameText = null;
        [UIComponent("songAuthorText")] private readonly TextMeshProUGUI songAuthorText = null;
        [UIComponent("songSubNameText")] private readonly TextMeshProUGUI songSubNameText = null;
        [UIComponent("coverArtImage")] private readonly Image coverArtImage = null;
        
        [UIComponent("downloadButton")] private readonly Button downloadButton = null;

        [UIAction("downloadButtonOnClick")]
        private async void DownloadButtonOnClick()
        {
            try
            {
                downloadButton.SetButtonText("Downloading...");
                downloadButton.interactable = false;
                await _mapQueueManager.addMapToQueue(_selectedBeatmap);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
        
        [UIAction("testButtonOnClick")]
        private async void testButtonOnClick()
        {
            try
            {
                if (string.IsNullOrEmpty(PluginConfig.Instance.refreshToken)) _oAuthApi.startNewOAuthFlow();
                else await _oAuthApi.getNewToken();
            }
            catch (Exception e)
            {
                _logger.Error(e);
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
                
                var downloadedImage = await _selectedBeatmap.LatestVersion.DownloadCoverImage();
                coverArtImage.sprite = BeatSaverChecker.createSpriteFromImageBuffer(downloadedImage);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
        
        private async void OnBeatSaverCheck(List<Beatmap> mapList)
        {
            try
            {
                this._beatmapsInList = mapList;
                
                var mapTableData = new List<CustomListTableData.CustomCellInfo>();
                foreach (var map in mapList)
                {
                    mapTableData.Add(await getCustomListCellData(map));
                }

                customListTableData.Data = mapTableData;
                customListTableData.TableView.ReloadData();
            }
            catch (Exception e)
            {
                _logger.Error(e);
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
        
        public void Initialize() => _beatSaverChecker.OnBeatSaverCheck += OnBeatSaverCheck;
        
        public void Dispose() => _beatSaverChecker.OnBeatSaverCheck -= OnBeatSaverCheck;
    }
}