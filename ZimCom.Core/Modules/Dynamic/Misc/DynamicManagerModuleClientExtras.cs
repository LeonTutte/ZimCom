using System.Net;
using System.Net.Sockets;
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
    private IPAddress? _address;
    private UdpClient _client;

    /// <summary>
    /// Attempts to connect the client to a server using the specified address. It's the main function where the communication starts.
    /// </summary>
    /// <param name="address">The IP address of the server as a string. If null or whitespace, the connection will not be established.</param>
    public void ConnectToServer(string? address)
    {
        if (!string.IsNullOrWhiteSpace(address)) IPAddress.TryParse(address.AsSpan(), out _address);
        if (_address is null) return;
        var packet = new DynamicPacketBuilderModule();
        packet.WriteOperationCode((byte)StaticNetCodes.RegisterCode);
        try
        {
            _client = new UdpClient(_address.AddressFamily);
            _client.Send(packet.GetPacketBytes(), packet.GetPacketBytes().Length, new(_address, ServerPort));
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error during server registration", ex);
            StaticNetClientEvents.ConnectedToServerFail?.Invoke(this, ex);
        }
        //HandleIncomingServerPackets();
    }

    /// <summary>
    /// Responses from the server will be handled here and should always fire an event after decoding.
    /// </summary>
    /// <exception cref="Exception"></exception>
    private void HandleIncomingServerPackets()
    {
        Task.Run(() =>
        {
            while (ClientPacketReader is not null)
            {
                var opCode = ClientPacketReader.ReadByte();
                switch (opCode)
                {
                    case (byte)StaticNetCodes.ServerCode:
                        StaticNetClientEvents.ReceivedServerData?.Invoke(this,
                            Server.SetFromPacket(ClientPacketReader!.Read32Message()) ??
                            throw new Exception("Failed to read data"));
                        break;
                    case (byte)StaticNetCodes.ChatMessageCode:
                        StaticNetClientEvents.ReceivedMessageFromServer?.Invoke(this,
                            ChatMessage.SetFromPacket(ClientPacketReader!.Read32Message()) ??
                            throw new Exception("Failed to read data"));
                        break;
                    case (byte)StaticNetCodes.ChangeChannel:
                        StaticNetClientEvents.OtherUserChangeChannel?.Invoke(this,
                            (User.SetFromPacket(ClientPacketReader!.Read32Message()),
                                Channel.SetFromPacket(ClientPacketReader!.Read32Message())));
                        break;
                }
            }
        });
    }
}