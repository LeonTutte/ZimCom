using ZimCom.Core.Models;

namespace ZimCom.Core.Modules.Static.Net;

/// <summary>
/// Provides static events for managing communication between a client and a server in a networked environment.
/// </summary>
public static class StaticNetClientEvents
{
    public static EventHandler? ConnectedToServer { get; set; }
    public static EventHandler<Server>? ReceivedServerData { get; set; }
    public static EventHandler? DisconnectedFromServer { get; set; }
    public static EventHandler<ChatMessage>? SendMessageToServer { get; set; }
    public static EventHandler<ChatMessage>? ReceivedMessageFromServer { get; set; }
    public static EventHandler<(User, string)>? UserChangeChannel { get; set; }
    public static EventHandler<(User?, Channel?)>? OtherUserChangeChannel { get; set; }
    public static EventHandler<byte[]>? ReceivedAudio { get; set; }
}