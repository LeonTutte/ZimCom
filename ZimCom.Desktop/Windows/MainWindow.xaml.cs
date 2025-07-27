using System.Windows;

using ZimCom.Core.Models;
using ZimCom.Desktop.ViewModels;

namespace ZimCom.Desktop.Windows;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private MainViewModel _viewModel;
    public MainWindow() {
        InitializeComponent();
        _viewModel = new MainViewModel();
        this.Title = Assets.ResourceEN.ApplicationName;
        this.DataContext = _viewModel;
    }

    private void ChannelTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        if (e.NewValue is Channel selectedChannel && _viewModel.Server is not null) {
            // Suchen Sie das Channel-Objekt aus dem ViewModel, das dem ausgewählten entspricht
            var matchedChannel = _viewModel.Server.Channels
                .FirstOrDefault(c => c.Label == selectedChannel.Label);

            if (matchedChannel != null) {
                _viewModel.SelectedChannel = matchedChannel;
            }
        }
    }

    private void ChannelTree_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        _viewModel.JoinChannel();
    }

    private void ChatSubmitButton_Click(object sender, RoutedEventArgs e) {
        if (_viewModel.Server is not null && _viewModel.User is not null && _viewModel.CurrentChannel is not null && !String.IsNullOrWhiteSpace(_viewModel.CurrentChatMessage)) {
            var tempMessage = new ChatMessage(_viewModel.User, _viewModel.CurrentChatMessage);
            _viewModel.DynamicServerManagerModule.SendChannelMessage(tempMessage, _viewModel.CurrentChannel);
            _viewModel.CurrentChatMessage = String.Empty;
        }
    }
}