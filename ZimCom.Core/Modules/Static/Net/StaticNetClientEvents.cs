using ZimCom.Core.Models;

namespace ZimCom.Core.Modules.Static.Net;
public static class StaticNetClientEvents {
    public static EventHandler? ConnectedToServer { get; set; }
    public static EventHandler<Exception>? ConnectedToServerFail { get; set; }
    public static EventHandler<Server>? ReceivedServerData { get; set; }
    public static EventHandler? DisconnectedFromServer { get; set; }
    public static EventHandler<ChatMessage>? SendMessageToServer { get; set; }
}
