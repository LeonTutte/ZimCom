using CommunityToolkit.Mvvm.ComponentModel;

namespace ZimCom.Core.Models;

/// <summary>
/// Unknown use yet ...
/// </summary>
/// <remarks>
/// Planned usage for saving the input/output device and audio settings
/// </remarks>
public partial class UserSettings : ObservableObject
{
    [ObservableProperty] public partial string? OutputDeviceId { get; set; }
    [ObservableProperty] public partial string? InputDeviceId { get; set; }
    [ObservableProperty] public partial bool LocalPlayback { get; set; } = false;
    [ObservableProperty] public partial double VoiceActivityDetectionThreshold { get; set; } = 0.025;
}