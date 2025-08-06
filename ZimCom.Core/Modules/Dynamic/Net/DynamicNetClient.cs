using System.Net.Sockets;
using Spectre.Console;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Modules.Dynamic.Net;

/// <summary>
/// Represents a client for handling network communication.
/// </summary>
public class DynamicNetClient
{
    /// <summary>
    /// Represents the packet reader module responsible for reading data from the network stream of a TCP client.
    /// </summary>
    private readonly DynamicPacketReaderModule _packetReaderModule;

    /// <summary>
    /// Represents a TCP client connection used for network communication.
    /// </summary>
    public readonly TcpClient TcpClient;

    /// <summary>
    /// Represents a network client.
    /// </summary>
    public DynamicNetClient(TcpClient tcpClient)
    {
        TcpClient = tcpClient;
        _packetReaderModule = new DynamicPacketReaderModule(TcpClient.GetStream());
        AnsiConsole.MarkupLine($"Accepted [green]new user[/] session with [blue]{Uid}[/]");
        HandleIncomingServerPackets();
    }

    /// <summary>
    /// Gets or sets the User object associated with this client.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets the unique identifier for the client.
    /// </summary>
    public Guid Uid { get; } = Guid.NewGuid();

    /// <summary>
    /// Handles incoming server packets asynchronously.
    /// </summary>
    private void HandleIncomingServerPackets()
    {
        Task.Run(() =>
        {
            while (TcpClient.Connected)
            {
                var opCode = _packetReaderModule.ReadByte();
                switch (opCode)
                {
                    case (byte)StaticNetOpCodes.UserCode:
                        SetUser(_packetReaderModule.Read32Message());
                        break;
                    case (byte)StaticNetOpCodes.ChatMessageCode:
                        SetChatMessage(_packetReaderModule.Read32Message());
                        break;
                    case (byte)StaticNetOpCodes.ChangeChannel:
                        ChangeChannel(_packetReaderModule.Read32Message(), _packetReaderModule.Read32Message());
                        break;
                }
            }
            if (User is not null) AnsiConsole.MarkupLine($"[blue]{User.Label}[/] disconnected from server");
        });
    }

    /// <summary>
    /// Handles the change of channel for a user.
    /// </summary>
    /// <param name="data">The serialized data representing the user.</param>
    /// <param name="data2">The serialized data representing the channel.</param>
    private void ChangeChannel(string data, string data2)
    {
        var user = User.SetFromPacket(data);
        var channel = Channel.SetFromPacket(data2);
        if (user is not null && channel is not null)
            StaticNetServerEvents.UserChannelChange?.Invoke(this, (user, channel));
    }

    /// <summary>
    /// Sets the user based on the provided packet data.
    /// </summary>
    /// <param name="data">The packet data containing user information.</param>
    private void SetUser(string data)
    {
        User = User.SetFromPacket(data);
        if (User is null) return;
        AnsiConsole.MarkupLine($"[green]new user[/] with [blue]{Uid.ToString()}[/] identified as [green]{User.Label}[/]");
        StaticLogModule.LogInformation($"{User.Label} connected to server with {User.Id}");
        StaticNetServerEvents.NewClientConnected?.Invoke(this, this);
        StaticNetServerEvents.ReceivedUserInformation?.Invoke(this, User);
    }

    /// <summary>
    /// Sets the chat message for the user.
    /// </summary>
    /// <param name="data">The data string containing the chat message.</param>
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