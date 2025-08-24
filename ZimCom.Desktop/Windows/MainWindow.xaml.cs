using System.Windows;
using System.Windows.Input;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Static.Net;
using ZimCom.Desktop.ViewModels;

namespace ZimCom.Desktop.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly MainViewModel _viewModel = new();

    /// <summary>
    /// Represents the main window of the application.
    /// </summary>
    /// <remarks>
    /// This class defines the main user interface window of the application. It is initialized with a title and a
    /// data context linked to the <see cref="MainViewModel"/>. The <c>DataContext</c> is used to bind UI elements
    /// to the properties and commands present in the associated ViewModel.
    /// </remarks>
    public MainWindow()
    {
        InitializeComponent();
        Title = "ZimCom";
        DataContext = _viewModel;
    }

    private void ChannelTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not Channel selectedChannel || _viewModel.Server is null) return;
        // Suchen Sie das Channel-Objekt aus dem ViewModel, das dem ausgewählten entspricht
        var matchedChannel = _viewModel.Server.Channels
            .FirstOrDefault(c => c.Label == selectedChannel.Label);

        if (matchedChannel != null) _viewModel.SelectedChannel = matchedChannel;
    }

    private void ChannelTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.JoinChannel();
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        if (_viewModel.DynamicManagerModule.Registered is false) return;
        _viewModel.DynamicManagerModule.DisconnectFromServer();
    }
}