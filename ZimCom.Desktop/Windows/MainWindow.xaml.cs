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

    private void ChatSubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Server is null || _viewModel.CurrentChannel is null ||
            string.IsNullOrWhiteSpace(_viewModel.CurrentChatMessage)) return;
        var tempMessage =
            new ChatMessage(_viewModel.User, _viewModel.CurrentChatMessage, _viewModel.CurrentChannel.Label);
        _viewModel.DynamicManagerModule.SendChannelMessage(tempMessage, _viewModel.CurrentChannel);
        StaticNetClientEvents.SendMessageToServer?.Invoke(this, tempMessage);
        _viewModel.CurrentChatMessage = string.Empty;
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        if (_viewModel.DynamicManagerModule.Registered is false) return;
        _viewModel.DynamicManagerModule.DisconnectFromServer();
    }
}