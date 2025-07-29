using Spectre.Console;

using System.Net.Sockets;

using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Modules.Dynamic.Net;
public class DynamicNetClient {
    public User? User { get; set; }
    public Guid UID { get; set; }
    public TcpClient TcpClient;
    private DynamicIoClientPacketReader _packetReader;

    public DynamicNetClient(TcpClient tcpClient) {
        TcpClient = tcpClient;
        UID = Guid.NewGuid();
        _packetReader = new DynamicIoClientPacketReader(TcpClient.GetStream());
        AnsiConsole.MarkupLine($"Accepted [green]new user[/] session with [blue]{UID.ToString()}[/]");
        HandleIncomingServerPackets();
    }

    private void HandleIncomingServerPackets() {
        Task.Run(() => {
            while (TcpClient.Connected is true && _packetReader is not null) {
                byte opCode = _packetReader.ReadByte();
                switch (opCode) {
                    case (byte)StaticNetOpCodes.UserCode:
                        SetUser(_packetReader.ReadMessage());
                        break;
                    default:
                        break;
                }
            }
        });
    }

    private void SetUser(string data) {
        User = User.SetFromPacket(data);
        if (User is not null) AnsiConsole.MarkupLine($"[green]new user[/] with [blue]{UID.ToString()}[/] identified as [green]{User.Label}[/]");
        StaticNetServerEvents.NewClientConnected?.Invoke(this, this);
        if (User is not null) StaticNetServerEvents.ReceivedUserInformation?.Invoke(this, User);
    }

    ~DynamicNetClient() {
        TcpClient.Dispose();
        AnsiConsole.MarkupLine($"[blue]{UID.ToString()}[/] disconnected");
    }
}
