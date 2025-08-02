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
                    case (byte)StaticNetOpCodes.ChatMessageCode:
                        SetChatMessage(_packetReader.ReadMessage());
                        break;
                    case (byte)StaticNetOpCodes.ChangeChannel:
                        ChangeChannel(_packetReader.ReadMessage(), _packetReader.ReadMessage());
                        break;
                    default:
                        break;
                }
            }
        });
    }
    private void ChangeChannel(string data, string data2) {
        var user = User.SetFromPacket(data);
        var channel = Channel.SetFromPacket(data2);
        if (user is not null && channel is not null) {
            StaticNetServerEvents.UserChannelChange?.Invoke(this, (user, channel));
        }
    }
    private void SetUser(string data) {
        User = User.SetFromPacket(data);
        if (User is not null) AnsiConsole.MarkupLine($"[green]new user[/] with [blue]{UID.ToString()}[/] identified as [green]{User.Label}[/]");
        StaticNetServerEvents.NewClientConnected?.Invoke(this, this);
        if (User is not null) StaticNetServerEvents.ReceivedUserInformation?.Invoke(this, User);
    }

    public void SetChatMessage(string data) {
        var temp = ChatMessage.SetFromPacket(data);
        if (User is not null && temp is not null) AnsiConsole.MarkupLine($"[blue]{User.Label}[/] send Message with size {data.Length}");
        if (temp is not null) StaticNetServerEvents.RecievedChatMessage?.Invoke(this, temp);
    }

    ~DynamicNetClient() {
        TcpClient.Close();
        TcpClient.Dispose();
        AnsiConsole.MarkupLine($"[blue]{UID.ToString()}[/] disconnected");
    }
}
