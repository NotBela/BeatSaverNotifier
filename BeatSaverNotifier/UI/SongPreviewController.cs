using System;
using BeatSaverNotifier.UI.FlowCoordinators;
using HMUI;
using UnityEngine;
using Zenject;

namespace BeatSaverNotifier.UI;

public class SongPreviewController : IInitializable, IDisposable
{
    [Inject] private readonly SongPreviewPlayer _songPreviewPlayer = null!;
    [Inject] private readonly BeatSaverNotifierFlowCoordinator _beatSaverNotifierFlowCoordinator = null!;
    [Inject] private readonly SettingsManager _settingsManager = null!;

    public void playPreview(AudioClip audioClip) => _songPreviewPlayer.CrossfadeTo(audioClip, _settingsManager.settings.audio.ambientVolumeScale, 0f, audioClip.length, () => {});
    
    private void onBackButtonPressed() => _songPreviewPlayer.CrossfadeToDefault();
    private void onViewControllerSwitched(ViewController _) => _songPreviewPlayer.CrossfadeToDefault();

    public void Initialize()
    {
        _beatSaverNotifierFlowCoordinator.onBackButtonPressed += onBackButtonPressed;
        _beatSaverNotifierFlowCoordinator.onViewControllerSwitched += onViewControllerSwitched;
    }

    public void Dispose()
    {
        _beatSaverNotifierFlowCoordinator.onBackButtonPressed -= onBackButtonPressed;
        _beatSaverNotifierFlowCoordinator.onViewControllerSwitched -= onViewControllerSwitched;
    }
}