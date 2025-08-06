using System.Net.Sockets;
using Spectre.Console;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Modules.Dynamic.Net;

public class DynamicNetClient
{
    private readonly DynamicIoClientPacketReader _packetReader;
    public readonly TcpClient TcpClient;

    public DynamicNetClient(TcpClient tcpClient)
    {
        TcpClient = tcpClient;
        _packetReader = new DynamicIoClientPacketReader(TcpClient.GetStream());
        AnsiConsole.MarkupLine($"Accepted [green]new user[/] session with [blue]{Uid}[/]");
        HandleIncomingServerPackets();
    }

    public User? User { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    private void HandleIncomingServerPackets()
    {
        Task.Run(() =>
        {
            while (TcpClient.Connected)
            {
                var opCode = _packetReader.ReadByte();
                switch (opCode)
                {
                    case (byte)StaticNetOpCodes.UserCode:
                        SetUser(_packetReader.ReadMessage());
                        break;
                    case (byte)StaticNetOpCodes.ChatMessageCode:
                        SetChatMessage(_packetReader.ReadMessage());
                        break;
                    case (byte)StaticNetOpCodes.ChangeChannel:
                        ChangeChannel(_packetReader.ReadMessage(), _packetReader.ReadMessage());
                        break;
                }
            }
            if (User is not null) AnsiConsole.MarkupLine($"[blue]{User.Label}[/] disconnected from server");
        });
    }

    private void ChangeChannel(string data, string data2)
    {
        var user = User.SetFromPacket(data);
        var channel = Channel.SetFromPacket(data2);
        if (user is not null && channel is not null)
            StaticNetServerEvents.UserChannelChange?.Invoke(this, (user, channel));
    }

    private void SetUser(string data)
    {
        User = User.SetFromPacket(data);
        if (User is not null)
            AnsiConsole.MarkupLine(
                $"[green]new user[/] with [blue]{Uid.ToString()}[/] identified as [green]{User.Label}[/]");
        StaticNetServerEvents.NewClientConnected?.Invoke(this, this);
        if (User is not null) StaticNetServerEvents.ReceivedUserInformation?.Invoke(this, User);
    }

    private void SetChatMessage(string data)
    {
        var temp = ChatMessage.SetFromPacket(data);
        if (User is not null && temp is not null)
            AnsiConsole.MarkupLine($"[blue]{User.Label}[/] send Message with size {data.Length}");
        if (temp is not null) StaticNetServerEvents.ReceivedChatMessage?.Invoke(this, temp);
    }

    ~DynamicNetClient()
    {
        TcpClient.Close();
        TcpClient.Dispose();
        AnsiConsole.MarkupLine($"[blue]{Uid}[/] disconnected");
    }
}