using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic;
using ZimCom.Core.Modules.Static.Net;
using ZimCom.Desktop.Windows;

namespace ZimCom.Desktop.ViewModels {
    public partial class MainViewModel : ObservableObject {
        public DynamicManagerModuleClientExtras DynamicManagerModule { get; set; }
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
            DynamicManagerModule = new DynamicManagerModuleClientExtras();
            // Testdata
            Server = DynamicManagerModule.Server;
            User = User.Load();
            if (Server is not null && User is not null) {
                Channel DefaultChannel = GetDefaultChannel();
                DefaultChannel.Participents.Add(User);
                CurrentChannel = DefaultChannel;
            }
            AttachToClientEvents();
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
        public void MuteInput() {
            if (User is not null) {
                if (User.IsMuted) User.IsMuted = false;
                if (User.IsMuted is false) User.IsMuted = true;
            }
        }
        [RelayCommand]
        public void MuteOutput() {
            if (User is not null) {
                if (User.HasOthersMuted) User.HasOthersMuted = false;
                if (User.HasOthersMuted is false) User.HasOthersMuted = true;
            }
        }
        [RelayCommand]
        public void AwayUser() {
            if (User is not null) {
                if (User.IsAway) User.IsAway = false;
                if (User.IsAway is false) User.IsAway = true;
            }
        }
        [RelayCommand]
        public static void OpenSettings() {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }
        [RelayCommand]
        public void OpenConnect() {
            ConnectWindow connectWindow = new ConnectWindow();
            connectWindow.ViewModel.ConnectToAddress += (sender, e) => {
                DynamicManagerModule!.ConnectToServer(e);
                if (User is not null) DynamicManagerModule!.SendUserInfo(User);
            };
            connectWindow.ViewModel.CloseWindow += (sender, e) => {
                connectWindow.Close();
            };
            connectWindow.ShowDialog();
        }
        public Channel GetDefaultChannel() => Server!.Channels.FindAll(x => x.DefaultChannel.Equals(true)).First();
        public void AttachToClientEvents() {
            StaticNetClientEvents.ReceivedServerData += (sender, e) => {
                this.Server = e;
            };
            StaticNetClientEvents.DisconnectedFromServer += (sender, e) => {
                MessageWindow messageWindow = new MessageWindow("Disconnect", "Disconnected from Server!");
                messageWindow.ShowDialog();
            };
        }
    }
}
