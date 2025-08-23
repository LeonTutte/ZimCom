using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZimCom.Core.Models;

namespace ZimCom.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    /// <summary>
    /// Represents the active user associated with the settings view model.
    /// </summary>
    [ObservableProperty] public partial User? User { get; set; }

    [RelayCommand]
    private void SavedSettings()
    {
        User?.Save();
        SettingsSaveButtonPressed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// A static event triggered when the settings save button is pressed.
    /// </summary>
    public static EventHandler? SettingsSaveButtonPressed { get; set; }
}