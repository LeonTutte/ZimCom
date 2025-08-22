using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZimCom.Core.Models;

namespace ZimCom.Desktop.ViewModels;

public partial class SettingsViewModel() : ObservableObject
{
    [ObservableProperty] public partial User User { get; set; }

    [RelayCommand]
    public void SavedSettings()
    {
        User.Save();
        SettingsSaveButtonPressed?.Invoke(this, EventArgs.Empty);
    }

    public static EventHandler? SettingsSaveButtonPressed { get; set; }
}