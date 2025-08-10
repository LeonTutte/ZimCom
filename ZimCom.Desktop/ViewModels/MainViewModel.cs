using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.Misc;
using ZimCom.Core.Modules.Static.Net;
using ZimCom.Desktop.Windows;

namespace ZimCom.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        // Testdata
        Server = DynamicManagerModule.Server;
        User = User.Load() ?? new User("Default User");
        if (Server is not null)
        {
            var defaultChannel = GetDefaultChannel();
            defaultChannel.Participants.Add(User);
            CurrentChannel = defaultChannel;
        }

        AttachToClientEvents();
    }

    public DynamicManagerModuleClientExtras DynamicManagerModule { get; } = new();
    [ObservableProperty] public partial Server? Server { get; set; }
    [ObservableProperty] public partial User User { get; set; }
    [ObservableProperty] public partial Channel? SelectedChannel { get; set; }
    [ObservableProperty] public partial Channel? CurrentChannel { get; set; }
    [ObservableProperty] private partial Channel? PreviousChannel { get; set; }
    [ObservableProperty] public partial string? CurrentChatMessage { get; set; }
    [ObservableProperty] public partial bool ChatEnabled { get; set; } = false;
    [ObservableProperty] public partial bool ChannelExtrasEnabled { get; set; } = false;
    private User ServerUser { get; } = new("Server");

    [RelayCommand]
    public void JoinChannel()
    {
        if (SelectedChannel?.TitleChannel is not false || SelectedChannel.SpacerChannel) return;
        if (DynamicManagerModule.CheckUserAgainstChannelStrength(Strength.ChannelAccess, User, SelectedChannel))
        {
            PreviousChannel = CurrentChannel;
            PreviousChannel!.Participants.Remove(User);
            PreviousChannel.CurrentChannel = false;
            SelectedChannel.CurrentChannel = true;
            SelectedChannel.Participants.Add(User);
            CurrentChannel = SelectedChannel;
            ChannelExtrasEnabled = !CurrentChannel.LocalChannel;
            StaticNetClientEvents.UserChangeChannel?.Invoke(this, (User, CurrentChannel));
        }
        else
        {
            var messageWindow = new MessageWindow("Access denied",
                $"You are not permitted to access {SelectedChannel.Label}");
            messageWindow.ShowDialog();
        }
    }

    [RelayCommand]
    private void MuteInput() => User.IsMuted = !User.IsMuted;

    [RelayCommand]
    private void MuteOutput() => User.HasOthersMuted = !User.HasOthersMuted;

    [RelayCommand]
    private void AwayUser() => User.IsAway = !User.IsAway;

    [RelayCommand]
    private static void OpenHelp()
    {
        MessageWindow messageWindow = new MessageWindow("Help",
            $"ZimCom Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}{Environment.NewLine}" +
            $"Build by L. Zimmermann");
        messageWindow.ShowDialog();
    }

    [RelayCommand]
    private static void OpenSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    [RelayCommand]
    private void OpenConnect()
    {
        var connectWindow = new ConnectWindow();
        connectWindow.ViewModel.ConnectToAddress += (_, e) =>
        {
            try
            {
                DynamicManagerModule.ConnectToServer(e);
            }
            catch (Exception ex)
            {
                var messageWindow = new MessageWindow("Connect", ex.Message);
                messageWindow.ShowDialog();
            }

            DynamicManagerModule.SendUserInfo(User);
        };
        connectWindow.ViewModel.CloseWindow += (_, _) => { connectWindow.Close(); };
        connectWindow.ShowDialog();
    }

    private Channel GetDefaultChannel()
    {
        return Server!.Channels.FindAll(x => x.DefaultChannel.Equals(true)).First();
    }

    private void AttachToClientEvents()
    {
        StaticNetClientEvents.ReceivedServerData += (_, e) =>
        {
            Server = e;
            PreviousChannel = null;
            CurrentChannel = GetDefaultChannel();
            CurrentChannel.Participants.Add(User);
            ChatEnabled = true;
        };
        StaticNetClientEvents.DisconnectedFromServer += (_, _) =>
        {
            var messageWindow = new MessageWindow("Disconnect", "Disconnected from Server!");
            messageWindow.ShowDialog();
        };
        StaticNetClientEvents.SendMessageToServer += (_, e) => { CurrentChannel!.Chat.Add(e); };
        StaticNetClientEvents.ReceivedMessageFromServer += (_, e) => { CurrentChannel!.Chat.Add(e); };
        StaticNetClientEvents.OtherUserChangeChannel += (_, e) =>
        {
            if (e.Item1 is null || e.Item2 is null) return;
            var temp = DynamicManagerModule.FindUserInChannel(e!.Item1);
            if (temp is null) return;
            if (temp.Label != e!.Item2.Label)
                temp.Participants.Remove(temp.Participants.First(x => x.Id.Equals(e.Item1.Id)));

            var serverTemp =
                Server!.Channels.First(x => x.Label.Equals(e.Item2.Label, StringComparison.Ordinal));
            serverTemp.Participants.Add(e.Item1);
            CurrentChannel?.Chat.Add(new ChatMessage(ServerUser, $"{e.Item1.Label} joined Channel"));
        };
    }
}