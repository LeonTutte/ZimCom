using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic;
using ZimCom.Desktop.Windows;

namespace ZimCom.Desktop.ViewModels {
    public partial class MainViewModel : ObservableObject {
        public DynamicServerManagerModule DynamicServerManagerModule { get; set; }
        [ObservableProperty]
        public partial Server? Server { get; set; }
        [ObservableProperty]
        public partial User? User { get; set; }
        [ObservableProperty]
        public virtual partial Channel? SelectedChannel { get; set; }
        [ObservableProperty]
        public virtual partial Channel? CurrentChannel { get; set; }
        [ObservableProperty]
        public virtual partial Channel? PreviousChannel { get; set; }
        [ObservableProperty]
        public partial string? CurrentChatMessage { get; set; }
        [ObservableProperty]
        public partial bool ChatEnabled { get; set; } = false;

        public MainViewModel() {
            DynamicServerManagerModule = new DynamicServerManagerModule();
            // Testdata
            Server = DynamicServerManagerModule.Server;
            User = User.Load();
            if (Server is not null && User is not null) {
                Channel DefaultChannel = GetDefaultChannel();
                DefaultChannel.Participents.Add(User);
                CurrentChannel = DefaultChannel;
            }
        }
        [RelayCommand]
        public void JoinChannel() {
            if (SelectedChannel is not null) {
                if (SelectedChannel.TitleChannel is false && SelectedChannel.SpacerChannel is false && User is not null) {
                    PreviousChannel = CurrentChannel;
                    PreviousChannel!.Participents.Remove(User);
                    PreviousChannel.CurrentChannel = false;
                    SelectedChannel.CurrentChannel = true;
                    SelectedChannel.Participents.Add(User);
                    CurrentChannel = SelectedChannel;
                }
            }
        }
        [RelayCommand]
        public static void MuteInput() { }
        [RelayCommand]
        public static void MuteOutput() { }
        [RelayCommand]
        public static void AwayUser() { }
        [RelayCommand]
        public static void OpenSettings() {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }
        [RelayCommand]
        public static void OpenConnect() {
            ConnectWindow connectWindow = new ConnectWindow();
            connectWindow.ViewModel.ConnectToAddress += (sender, e) => {
                Console.WriteLine($"{sender}: Lol hast verbunden zu {e}");
            };
            connectWindow.ViewModel.CloseWindow += (sender, e) => {
                connectWindow.Close();
            };
            connectWindow.ShowDialog();
        }
        public Channel GetDefaultChannel() => Server!.Channels.FindAll(x => x.DefaultChannel.Equals(true)).First();

    }
}
