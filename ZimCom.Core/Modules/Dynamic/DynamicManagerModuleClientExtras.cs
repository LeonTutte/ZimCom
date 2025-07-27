using System.Net;

using ZimCom.Core.Modules.Static;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Modules.Dynamic;
public class DynamicManagerModuleClientExtras : DynamicManagerModule {
    public DynamicManagerModuleClientExtras() : base(true) {

    }
    public void ConnectToServer(string address) {
        if (!String.IsNullOrWhiteSpace(address)) {
            IPAddress.TryParse(address.AsSpan(), out _address);
        }
        if (_address is not null) {
            if (_tcpClient.Connected is false) {
                try {
                    _tcpClient.Connect(_address, _serverPort);
                    if (_tcpClient.Connected is true) StaticNetClientEvents.ConnectedToServer?.Invoke(this, new EventArgs());
                } catch (Exception ex) {
                    StaticLogModule.LogError("Error during server connect", ex);
                    StaticNetClientEvents.ConnectedToServerFail?.Invoke(this, ex);
                }
            }
        }
    }
}
