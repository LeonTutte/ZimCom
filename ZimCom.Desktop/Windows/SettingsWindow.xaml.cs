using ZimCom.Desktop.ViewModels;

namespace ZimCom.Desktop.Windows;

/// <summary>
///     Interaktionslogik für SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow
{
    private readonly SettingsViewModel _viewModel = new();

    public SettingsViewModel ViewModel => _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }
}