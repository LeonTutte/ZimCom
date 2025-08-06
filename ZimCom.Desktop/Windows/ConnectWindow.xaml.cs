using ZimCom.Desktop.ViewModels;

namespace ZimCom.Desktop.Windows;

/// <summary>
///     Interaktionslogik für ConnectWindow.xaml
/// </summary>
public partial class ConnectWindow
{
    public ConnectWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
        AddressBox.Focus();
    }

    public ConnectWindowViewModel ViewModel { get; } = new();
}