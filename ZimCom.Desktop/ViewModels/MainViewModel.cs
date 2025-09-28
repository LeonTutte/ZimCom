using System.Net;
using System.Net.Sockets;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Dynamic.Misc;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;
using ZimCom.Desktop.Modules.Dynamic;
using ZimCom.Desktop.Windows;
using Assembly = System.Reflection.Assembly;

namespace ZimCom.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// Represents the primary view model for the application.
    /// </summary>
    /// <remarks>
    /// The constructor initializes test data by retrieving an internal server instance from
    /// the dynamic manager module and loading a persisted user or creating a default one.
    /// It then sets up a default channel, assigns the current channel,
    /// configures audio capture information, mutes the user initially,
    /// and subscribes to client events.
    /// The view model exposes several observable properties that can be bound
    /// directly from the UI: <c>Server</c>, <c>User</c>, <c>AudioInformationText</c>,
    /// and an internal <c>DynamicAudioModule</c>.  These properties are marked with
    /// <see cref="ObservablePropertyAttribute"/> so changes propagate automatically.
    /// </remarks>
    /// <seealso cref="ZimCom.Core.Models.Server"/>
    /// <seealso cref="ZimCom.Core.Models.User"/>
    /// <seealso cref="ZimCom.Desktop.Modules.Dynamic.DynamicAudioModule"/>
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

        AudioModule = new DynamicAudioModule();
        AudioInformationText =
            $"{AudioModule.AudioFormat.AverageBytesPerSecond} Bps as {AudioModule.AudioFormat.Encoding} from {AudioModule.AudioCaptureDevice.FriendlyName}";
        User.IsMuted = true;
        AttachToClientEvents();
    }

    /// <summary>
    /// Provides an instance of the DynamicManagerModuleClientExtras, which acts as a specialized extension of the DynamicManagerModule.
    /// Used in communication with the server and managing dynamic functionality within the application.
    /// </summary>
    public DynamicManagerModuleClientExtras DynamicManagerModule { get; } = new();

    [ObservableProperty] public partial Server? Server { get; private set; }
    [ObservableProperty] public partial User User { get; private set; }
    [ObservableProperty] public partial Channel? SelectedChannel { get; set; }
    [ObservableProperty] public partial Channel? CurrentChannel { get; private set; }
    [ObservableProperty] private partial Channel? PreviousChannel { get; set; }
    [ObservableProperty] public partial string? CurrentChatMessage { get; set; }
    [ObservableProperty] public partial string? AudioInformationText { get; private set; }
    [ObservableProperty] public partial bool ChatEnabled { get; set; } = false;
    [ObservableProperty] public partial bool ConnectEnabled { get; set; } = true;
    [ObservableProperty] public partial bool DisconnectEnabled { get; set; } = false;
    [ObservableProperty] public partial bool ChannelExtrasEnabled { get; set; } = false;
    [ObservableProperty] private partial DynamicAudioModule AudioModule { get; set; }
    [ObservableProperty] public partial float AudioLevel { get; set; }
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
            ChatEnabled = true;
            StaticNetClientEvents.UserChangeChannel?.Invoke(this, (User, CurrentChannel.Label));
        }
        else
        {
            var messageWindow = new MessageWindow("Access denied",
                $"You are not permitted to access {SelectedChannel.Label}");
            messageWindow.ShowDialog();
        }
    }

    [RelayCommand]
    private void MuteInput()
    {
        User.IsMuted = !User.IsMuted;
        if (User.IsMuted)
        {
            AudioModule.AudioCaptureSource.StopRecording();
            StaticLogModule.LogInformation(
                $"Disabled audio input on {AudioModule.AudioCaptureDevice.FriendlyName} with {AudioModule.AudioCaptureSource!.WaveFormat.SampleRate} on {AudioModule.AudioCaptureSource.WaveFormat.Channels} channels.");
        }
        else
        {
            AudioModule.VadThreshold = User?.UserSettings.VoiceActivityDetectionThreshold ?? 0.025;
            AudioModule.AudioCaptureSource.StartRecording();
            StaticLogModule.LogInformation($"Enabled audio input on {AudioModule.AudioCaptureDevice.FriendlyName}");
        }
        AudioModule.LocalPlayback = User?.UserSettings.LocalPlayback ?? false;
    }

    [RelayCommand]
    private void MuteOutput() => User.HasOthersMuted = !User.HasOthersMuted;

    [RelayCommand]
    private void AwayUser()
    {
        User.IsAway = !User.IsAway;
        if (User.IsAway)
        {
            User.IsMuted = true;
            User.HasOthersMuted = true;
        }
        else
        {
            User.IsMuted = false;
            User.HasOthersMuted = false;
        }
    }

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
        var settingsWindow = new SettingsWindow
        {
            ViewModel =
            {
                User = User.Load() ?? new User("Unknown")
            }
        };
        settingsWindow.ViewModel.LoadUserSettings();
        settingsWindow.ShowDialog();
    }

    [RelayCommand]
    private void SendMessage()
    {
        if (Server is null || CurrentChannel is null || string.IsNullOrWhiteSpace(CurrentChatMessage)) return;
        var tempMessage = new ChatMessage(User, CurrentChatMessage, CurrentChannel.Label);
        if (DynamicManagerModule.CheckUserAgainstChannelStrength(Strength.ChannelChat, User, CurrentChannel) is false)
        {
            var messageWindow = new MessageWindow("Denied",
                $"You are not permitted to send messages to {CurrentChannel.Label}");
            messageWindow.ShowDialog();
        }
        else
        {
            CurrentChannel.Chat.Add(tempMessage);
            StaticNetClientEvents.SendMessageToServer?.Invoke(this, tempMessage);
        }

        CurrentChatMessage = string.Empty;
    }

    [RelayCommand]
    private void OpenConnect()
    {
        var connectWindow = new ConnectWindow();
        connectWindow.ViewModel.ConnectToAddress += async void (_, e) =>
        {
            try
            {
                var address = string.Empty;
                switch (Uri.CheckHostName(e))
                {
                    case UriHostNameType.Unknown:
                        throw new ArgumentException("The hostname provided is not valid (no type available).");
                    case UriHostNameType.IPv4:
                    case UriHostNameType.IPv6:
                        address = e;
                        break;
                    case UriHostNameType.Dns:
                        address = (await Dns.GetHostAddressesAsync(e, AddressFamily.InterNetwork)).ToString();
                        break;
                }
                try
                {
                    DynamicManagerModule.ConnectToServer(address);
                }
                catch (Exception ex)
                {
                    var messageWindow = new MessageWindow("Connect", ex.Message);
                    messageWindow.ShowDialog();
                }

                await DynamicManagerModule.SendPacketToServer(User.GetPacket());
            }
            catch (Exception ex)
            {
                StaticLogModule.LogError("Error during connect", ex);
            }
        };
        connectWindow.ViewModel.CloseWindow += (_, _) => { connectWindow.Close(); };
        connectWindow.ShowDialog();
    }

    private void AttachToClientEvents()
    {
        AudioModule.AudioLevelCalculated += (_, e) =>
        {
            AudioLevel = e;
        };
        StaticNetClientEvents.ConnectedToServer += (_, _) =>
        {
            ConnectEnabled = false;
            DisconnectEnabled = true;
        };
        StaticNetClientEvents.ReceivedServerData += (_, e) =>
        {
            Server = e;
            PreviousChannel = null;
            SelectedChannel = GetDefaultChannel();
            JoinChannel();
        };
        StaticNetClientEvents.DisconnectedFromServer += (_, _) =>
        {
            var messageWindow = new MessageWindow("Disconnect", "Disconnected from Server!");
            messageWindow.ShowDialog();
            ConnectEnabled = true;
            DisconnectEnabled = false;
        };
        StaticNetClientEvents.ReceivedMessageFromServer += (_, e) =>
        {
            if (e is null) return;
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                Server?.Channels.First(x => x.Label.Equals(e.ChannelLabel, StringComparison.Ordinal)).Chat.Add(e)));
        };
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
            CurrentChannel?.Chat.Add(new(ServerUser, $"{e.Item1.Label} joined Channel",
                CurrentChannel.Label));
        };
        SettingsViewModel.SettingsSaveButtonPressed += (_, _) => { User.Load(); };
        AudioModule.PacketAvailable += (_, e) =>
        {
            //if (AudioLevel < 0.015) return;
            if (DynamicManagerModule.CheckUserAgainstChannelStrength(Strength.ChannelSpeech, User, CurrentChannel!) is false)
            {
                var messageWindow = new MessageWindow("Denied",
                    $"You are not permitted to talk in {CurrentChannel!.Label}");
                messageWindow.ShowDialog();
            }
            else
            {
                if (DynamicManagerModule.Registered)
                    DynamicManagerModule.SendPacketToServer(e).ConfigureAwait(false);
            }
        };
    }
    private Channel GetDefaultChannel()
    {
        return Server!.Channels.FindAll(x => x.DefaultChannel.Equals(true)).First();
    }
}