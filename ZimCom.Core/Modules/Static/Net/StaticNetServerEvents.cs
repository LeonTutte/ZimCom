using ZimCom.Core.Modules.Dynamic.Net;

namespace ZimCom.Core.Modules.Static.Net;
public static class StaticNetServerEvents {
    public static EventHandler<DynamicNetClient>? NewClientConnected { get; set; }
    public static EventHandler<DynamicNetClient>? ClientDisconnected { get; set; }
}
