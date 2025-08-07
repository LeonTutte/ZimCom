using System.Net;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Modules.Dynamic.Misc;

/// <summary>
/// Represents a specialized client extension module for the DynamicManagerModule. It's where all the communication is happening to the server.
/// Inherits from <see cref="DynamicManagerModule"/> to provide additional functionality specific to client-side operations.
/// </summary>
public class DynamicManagerModuleClientExtras() : DynamicManagerModule(true)
{
    /// <summary>
    /// Attempts to connect the client to a server using the specified address. It's the main function where the communication starts.
    /// </summary>
    /// <param name="address">The IP address of the server as a string. If null or whitespace, the connection will not be established.</param>
    public void ConnectToServer(string? address)
    {
        if (!string.IsNullOrWhiteSpace(address)) IPAddress.TryParse(address.AsSpan(), out Address);
        if (Address is null) return;
        if (TcpClient.Connected) return;
        try
        {
            TcpClient.Connect(Address, ServerPort);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error during server connect", ex);
            StaticNetClientEvents.ConnectedToServerFail?.Invoke(this, ex);
        }

        if (TcpClient.Connected is false) return;
        StaticNetClientEvents.ConnectedToServer?.Invoke(this, EventArgs.Empty);
        ClientPacketReader = new DynamicPacketReaderModule(TcpClient.GetStream());
        HandleIncomingServerPackets();
        AttachToClientEvents();
    }

    /// <summary>
    /// This function sends packets to the server based on events.
    /// </summary>
    private void AttachToClientEvents()
    {
        StaticNetClientEvents.SendMessageToServer += (_, e) =>
        {
            if (TcpClient.Connected is false) return;
            TcpClient.Client.Send(e.GetPacket());
        };
        StaticNetClientEvents.UserChangeChannel += (_, e) =>
        {
            if (TcpClient.Connected is false) return;
            if (e.Item1 is null || e.Item2 is null) return;
            var packet = new DynamicPacketBuilderModule();
            packet.WriteOperationCode((byte)StaticNetOpCodes.ChangeChannel);
            packet.WriteMessage(e.Item1.ToString());
            packet.WriteMessage(e.Item2.ToString());
            TcpClient.Client.Send(packet.GetPacketBytes());
        };
    }

    /// <summary>
    /// Responses from the server will be handled here and should always fire an event after decoding.
    /// </summary>
    /// <exception cref="Exception"></exception>
    private void HandleIncomingServerPackets()
    {
        Task.Run(() =>
        {
            while (TcpClient.Connected && ClientPacketReader is not null)
            {
                var opCode = ClientPacketReader.ReadByte();
                switch (opCode)
                {
                    case (byte)StaticNetOpCodes.ServerCode:
                        StaticNetClientEvents.ReceivedServerData?.Invoke(this,
                            Server.SetFromPacket(ClientPacketReader!.Read32Message()) ??
                            throw new Exception("Failed to read data"));
                        break;
                    case (byte)StaticNetOpCodes.ChatMessageCode:
                        StaticNetClientEvents.ReceivedMessageFromServer?.Invoke(this,
                            ChatMessage.SetFromPacket(ClientPacketReader!.Read32Message()) ??
                            throw new Exception("Failed to read data"));
                        break;
                    case (byte)StaticNetOpCodes.ChangeChannel:
                        StaticNetClientEvents.OtherUserChangeChannel?.Invoke(this,
                            (User.SetFromPacket(ClientPacketReader!.Read32Message()),
                                Channel.SetFromPacket(ClientPacketReader!.Read32Message())));
                        break;
                }
            }

            StaticNetClientEvents.DisconnectedFromServer?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>
    /// Sends information about the specified user to the connected server.
    /// </summary>
    /// <param name="user">The user whose information is to be sent. Cannot be null.</param>
    public void SendUserInfo(User user)
    {
        if (TcpClient.Connected is false) return;
        TcpClient.Client.Send(user.GetPacket());
    }
}