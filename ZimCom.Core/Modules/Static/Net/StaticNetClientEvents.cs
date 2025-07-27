namespace ZimCom.Core.Modules.Static.Net;
public static class StaticNetClientEvents {
    public static EventHandler? ConnectedToServer { get; set; }
    public static EventHandler<Exception>? ConnectedToServerFail { get; set; }
}
