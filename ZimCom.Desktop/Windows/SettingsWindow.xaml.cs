using ZimCom.Desktop.ViewModels;

namespace ZimCom.Desktop.Windows;

/// <summary>
///     Interaktionslogik für SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow
{
    public SettingsViewModel ViewModel { get; } = new();

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }
}