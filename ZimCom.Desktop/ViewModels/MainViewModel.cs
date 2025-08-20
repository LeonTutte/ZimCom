using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.Misc;
using ZimCom.Core.Modules.Static.Net;
using ZimCom.Desktop.Windows;
using Assembly = System.Reflection.Assembly;

namespace ZimCom.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        // Testdata
        Server = DynamicManagerModule.InternalServer;
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
    [ObservableProperty] public partial bool ConnectEnabled { get; set; } = true;
    [ObservableProperty] public partial bool DisconnectEnabled { get; set; } = false;
    [ObservableProperty] public partial bool ChannelExtrasEnabled { get; set; } = false;
    private User ServerUser { get; } = new("Server");

    /// <summary>
    /// Attempts to switch the current user to the selected channel. Ensures the user has sufficient access rights to join
    /// the selected channel and updates the channel's participant list accordingly. If access is denied, a message dialog
    /// is displayed to notify the user.
    /// </summary>
    /// <remarks>
    /// This method first checks if the selected channel is not a title or spacer channel. Then, it verifies the user's
    /// access rights against the selected channel using the dynamic manager module. If access is granted, the method
    /// updates the current and previous channel states and manages the participants' lists. If access is denied,
    /// a message window is displayed indicating the denial.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if no channel has been selected (i.e., SelectedChannel is null) when the method is invoked.
    /// </exception>
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
    private void DisconnectFromServer() => DynamicManagerModule.DisconnectFromServer();

    [RelayCommand]
    private static void OpenHelp()
    {
        MessageWindow messageWindow = new("Help",
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
                //Task.Run(() => DynamicManagerModule.ConnectToServer(e));
                //Task.Run(() => DynamicManagerModule.ConnectToServerViaQuic(e, User));
                DynamicManagerModule.ConnectToServer(e);
            }
            catch (Exception ex)
            {
                var messageWindow = new MessageWindow("Connect", ex.Message);
                messageWindow.ShowDialog();
            }

            DynamicManagerModule.SendPacketToServer(User.GetPacket()).ConfigureAwait(true);
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
        StaticNetClientEvents.ConnectedToServer += (_, _) =>
        {
            ConnectEnabled = false;
            DisconnectEnabled = true;
        };
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
            ConnectEnabled = true;
            DisconnectEnabled = false;
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