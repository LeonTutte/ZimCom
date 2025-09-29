using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Desktop.ViewModels;

public partial class ChatWindowViewModel : ObservableObject
{
    /// <summary>
    /// Send a <c>ChatMessage</c> to the current active channel
    /// </summary>
    /// <remarks>
    /// The message is only client-side
    /// </remarks>
    /// <seealso cref="ZimCom.Core.Models.ChatMessage"/>
    public static EventHandler<ChatMessage> SendMessageToCurrentChat {get; set;} = null!;

    /// <summary>
    /// Send a <c>string</c> to the current active channel, it will be transformed to a <c>ChatMessage</c> on the fly
    /// </summary>
    /// <remarks>
    /// The message is only client-side
    /// </remarks>
    /// <seealso cref="ZimCom.Core.Models.ChatMessage"/>
    public static EventHandler<string> SendTextToCurrentChat {get; set;} = null!;
    /// <summary>
    /// Send a <c>string</c> to the message-board, it will be transformed to a <c>ChatMessage</c> on the fly
    /// </summary>
    /// <remarks>
    /// The message is only client-side and intended for program events, that should inform the user
    /// </remarks>
    /// <seealso cref="ZimCom.Core.Models.ChatMessage"/>
    public static EventHandler<string> SendEventAsMessage {get; set;} = null!;
    /// <summary>
    /// Set the <c>Channel</c> on wich the client is active
    /// </summary>
    /// <remarks>
    /// A message about the channel change will automatically be populated on the fly
    /// </remarks>
    /// <seealso cref="ZimCom.Core.Models.Channel"/>
    public static EventHandler<Channel> SetCurrentChannel { get; set; } = null!;
    private User User { get; init; }
    [ObservableProperty] public partial Channel? CurrentChannel { get; private set; }
    [ObservableProperty] public partial ObservableCollection<ChatMessage> Messages { get; private set; } = [];
    [ObservableProperty] public partial string CurrentMessage {get; set; } = String.Empty;
    [ObservableProperty] public partial bool ChatEnabled { get; set; } = false;
    private User SystemUser { get; init; } = new User("System");

    [RelayCommand]
    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(CurrentMessage) || ChatEnabled is false || CurrentChannel is null) return;
        var tempMessage = new ChatMessage(User, CurrentMessage, CurrentChannel.Label);
        CurrentChannel.Chat.Add(tempMessage);
        Messages.Add(tempMessage);
        StaticNetClientEvents.SendMessageToServer?.Invoke(this, tempMessage);
        CurrentMessage = String.Empty;
    }

    /// <inheritdoc />
    public ChatWindowViewModel(User user)
    {
        User = user;
        Messages.Add(new ChatMessage(SystemUser, "Loaded profile " + User.Label, "System"));
        AttachToClientEvents();
    }

    private void AttachToClientEvents()
    {
        SendMessageToCurrentChat += (_, e) => Messages.Add(e);
        SendEventAsMessage += (_, e) => Messages.Add(new ChatMessage(SystemUser, e, "System"));
        SendTextToCurrentChat += (_, e) => Messages.Add(new ChatMessage(User, e, CurrentChannel?.Label ?? "System"));
        SetCurrentChannel += (_, e) =>
        {
            CurrentChannel = e;
            Messages.Add(new ChatMessage(SystemUser, "You joined " + e.Label, "System"));
        };
        StaticNetClientEvents.DisconnectedFromServer += (_, _) =>
        {
            Messages.Add(new ChatMessage(SystemUser, "Disconnected from server", "System"));
        };
        StaticNetClientEvents.ConnectedToServer += (_, _) =>
        {
            Messages.Add(new ChatMessage(SystemUser, "Connected to server", "System"));
        };
        StaticNetClientEvents.ReceivedMessageFromServer += (_, e) =>
        {
            if (e is null) return;
            Messages.Add(e);
        };
        StaticNetClientEvents.OtherUserChangeChannel += (_, e) =>
        {
            if (e.Item1 is null || e.Item2 is null) return;
            Messages.Add(new ChatMessage(SystemUser, e.Item1.Label + " joined your Channel", e.Item2.Label));
        };
    }
}