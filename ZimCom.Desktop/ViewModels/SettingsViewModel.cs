using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using ZimCom.Core.Models;

namespace ZimCom.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    /// <summary>
    /// Represents the active user associated with the settings view model.
    /// </summary>
    [ObservableProperty]
    public partial User? User { get; set; }
    [ObservableProperty] public partial int SelectedAudioInputIndex { get; set; }
    [ObservableProperty] public partial List<MMDevice> AvailableAudioInputDevices { get; set; }
    [ObservableProperty] public partial int SelectedAudioOutputIndex { get; set; }
    [ObservableProperty] public partial List<MMDevice> AvailableAudioOutputDevices { get; set; }

    [RelayCommand]
    private void SavedSettings()
    {
        User?.UserSettings.InputDeviceFriendlyName =
            AvailableAudioInputDevices[SelectedAudioInputIndex].DeviceFriendlyName;
        User?.UserSettings.OutputDeviceFriendlyName =
            AvailableAudioOutputDevices[SelectedAudioOutputIndex].DeviceFriendlyName;
        User?.Save();
        SettingsSaveButtonPressed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// A static event triggered when the settings save button is pressed.
    /// </summary>
    public static EventHandler? SettingsSaveButtonPressed { get; set; }

    public SettingsViewModel()
    {
        AvailableAudioOutputDevices =
            [.. new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)];
        AvailableAudioInputDevices = 
            [.. new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)];
        if (string.IsNullOrWhiteSpace(User?.UserSettings.OutputDeviceFriendlyName)) return;
        SelectedAudioOutputIndex =
            AvailableAudioOutputDevices.FindIndex(x =>
                x.DeviceFriendlyName.Equals(User.UserSettings.OutputDeviceFriendlyName, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(User?.UserSettings.InputDeviceFriendlyName)) return;
        SelectedAudioInputIndex =
            AvailableAudioInputDevices.FindIndex(x =>
                x.DeviceFriendlyName.Equals(User.UserSettings.InputDeviceFriendlyName, StringComparison.Ordinal));
    }
}