using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Desktop.Modules.Dynamic;

namespace ZimCom.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    /// <summary>
    /// Represents the active user associated with the settings view model.
    /// </summary>
    [ObservableProperty]
    public partial User? User { get; set; }
    [ObservableProperty] public partial int SelectedAudioInputIndex { get; set; }
    [ObservableProperty] public partial List<MMDevice> AvailableAudioInputDevices { get; private set; }
    [ObservableProperty] public partial int SelectedAudioOutputIndex { get; set; }
    [ObservableProperty] public partial double VadThreshold { get; set; } = 0.025;
    [ObservableProperty] public partial float AudioLevel { get; set; }
    [ObservableProperty] public partial bool LocalPlayback { get; set; } = false;
    [ObservableProperty] public partial List<MMDevice> AvailableAudioOutputDevices { get; private set; }
    [ObservableProperty] private partial DynamicAudioModule AudioModule { get; set; }
    [ObservableProperty] private partial IEnumerable<float> AverageSoundMeter { get; set; }
    [ObservableProperty] public partial string AverageSoundText { get; set; } = String.Empty;

    [RelayCommand]
    private void SavedSettings()
    {
        User?.UserSettings.InputDeviceId =
            AvailableAudioInputDevices[SelectedAudioInputIndex].ID;
        User?.UserSettings.OutputDeviceId =
            AvailableAudioOutputDevices[SelectedAudioOutputIndex].ID;
        User?.UserSettings.VoiceActivityDetectionThreshold =
            VadThreshold;
        User?.UserSettings.LocalPlayback = LocalPlayback;
        User?.Save();
        SettingsSaveButtonPressed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async void StartAudioInputTest()
    {
        try
        {
            AudioModule.AudioCaptureSource.StartRecording();
            await Task.Delay(5000);
            AudioModule.AudioCaptureSource.StopRecording();
            AverageSoundText = "Your average sound number is " + AverageSoundMeter.Average();
        }
        catch (Exception e)
        {
            StaticLogModule.LogError("Audio Capture Source failed", e);
        }
    }

    /// <summary>
    /// A static event triggered when the settings save button is pressed.
    /// </summary>
    public static EventHandler? SettingsSaveButtonPressed { get; set; }

    /// <summary>
    /// Represents the view model for application settings, including management of audio input and output devices.
    /// </summary>
    public SettingsViewModel()
    {
        AverageSoundMeter = [];
        AvailableAudioOutputDevices =
            [.. new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)];
        AvailableAudioInputDevices = 
            [.. new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)];
        AudioModule = new DynamicAudioModule();
        AudioModule?.AudioLevelCalculated += (_, e) =>
        {
            AudioLevel = e;
            AverageSoundMeter = AverageSoundMeter.Append(e);
        };
    }

    internal void LoadUserSettings()
    {
        User.Load();
        if (string.IsNullOrWhiteSpace(User?.UserSettings.OutputDeviceId)) return;
        SelectedAudioOutputIndex =
            AvailableAudioOutputDevices.FindIndex(x =>
                x.ID.Equals(User?.UserSettings.OutputDeviceId, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(User?.UserSettings.InputDeviceId)) return;
        SelectedAudioInputIndex =
            AvailableAudioInputDevices.FindIndex(x =>
                x.ID.Equals(User?.UserSettings.InputDeviceId, StringComparison.Ordinal));
        VadThreshold = User?.UserSettings.VoiceActivityDetectionThreshold ?? 0.025;
        LocalPlayback = User?.UserSettings.LocalPlayback ?? false;
    }
}