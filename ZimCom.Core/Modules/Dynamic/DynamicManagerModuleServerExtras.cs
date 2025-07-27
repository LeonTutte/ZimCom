using System.Data;
using System.Net.Sockets;

using ZimCom.Core.Modules.Dynamic.Net;
using ZimCom.Core.Modules.Static;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Modules.Dynamic;
public class DynamicManagerModuleServerExtras : DynamicManagerModule {
    private List<DynamicNetClient> _clients { get; set; }
    private TcpListener _tcpListener { get; set; }
    public DynamicManagerModuleServerExtras() : base(false) {
        _clients = new List<DynamicNetClient>();
        _address = Server.IpAddress;
        if (_address is null) {
            StaticLogModule.LogError("Error during server initialize, the net address is empty", null);
            throw new NoNullAllowedException();
        }
        _tcpListener = new TcpListener(_address!, _serverPort);
    }
    public void StartServerListener() {
        _tcpListener.Start();
        while (true) {
            DynamicNetClient tempClient = new DynamicNetClient(_tcpListener.AcceptTcpClient());
            _clients.Add(tempClient);
            StaticNetServerEvents.NewClientConnected?.Invoke(this, tempClient);
        }
    }
}
