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
    private UdpClient? _client;
    private IPEndPoint? _serverEndPoint;

    /// <summary>
    /// Indicates whether the client module is successfully registered with a server.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the client is actively registered and ready to exchange data with the server.
    /// The value is updated during the execution of the <see cref="ConnectToServer"/> method, and is used to determine
    /// whether the client can send or receive packets from the server.
    /// </remarks>
    public bool Registered;

    /// <summary>
    /// Attempts to connect the client to a server using the specified address. It's the main function where the communication starts.
    /// </summary>
    /// <param name="address">The IP address of the server as a string. If null or whitespace, the connection will not be established.</param>
    public void ConnectToServer(string? address)
    {
        if (!string.IsNullOrWhiteSpace(address)) IPAddress.TryParse(address.AsSpan(), out _address);
        if (_address is null || string.IsNullOrWhiteSpace(_address.ToString())) return;
        var packet = new DynamicPacketBuilderModule();
        packet.WriteOperationCode((byte)StaticNetCodes.RegisterCode);
        try
        {
            _client = new UdpClient(_address.AddressFamily);
            _client.Send(packet.GetPacketBytes(), packet.GetPacketBytes().Length, new(_address, ServerPort));
            Registered = true;
            StaticNetClientEvents.ConnectedToServer?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error during server registration", ex);
            ResetServerNetwork();
        }
        finally
        {
            if (_client is not null) _serverEndPoint = new(_address, ServerPort);
        }

        HandleIncomingServerPackets().ConfigureAwait(false);
        HandleClientEvents();
    }

    private void HandleClientEvents()
    {
        StaticNetClientEvents.UserChangeChannel += (_, e) =>
        {
            var packet = new DynamicPacketBuilderModule();
            packet.WriteOperationCode((byte)StaticNetCodes.ChangeChannel);
            packet.WriteMessage(e.Item1!.ToString());
            packet.WriteMessage(e.Item2!.ToString());
            SendPacketToServer(packet.GetPacketBytes()).ConfigureAwait(true);
        };
    }

    /// <summary>
    /// Disconnects the client from the server by sending an unregister packet.
    /// If the disconnection is successful, triggers the DisconnectedFromServer event.
    /// </summary>
    public void DisconnectFromServer()
    {
        Registered = false;
        var packet = new DynamicPacketBuilderModule();
        packet.WriteOperationCode((byte)StaticNetCodes.UnregisterCode);
        if (SendPacketToServer(packet.GetPacketBytes()).Result)
            StaticNetClientEvents.DisconnectedFromServer?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sends a packet to the server asynchronously using the provided payload.
    /// Handles any errors that might occur during the send operation and logs them accordingly.
    /// </summary>
    /// <param name="payload">The byte array containing the data to be sent to the server. Cannot be null or empty.</param>
    /// <returns>A task representing the asynchronous operation. The result is <c>true</c> if the packet was sent successfully, otherwise <c>false</c>.</returns>
    public async Task<bool> SendPacketToServer(byte[] payload)
    {
        if (_client is null || _serverEndPoint is null || _address is null) return false;
        try
        {
            await _client.SendAsync(payload, payload.Length, _serverEndPoint).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error sending packet to server", ex);
            return false;
        }

        return true;
    }

    private void ResetServerNetwork()
    {
        _client = null;
        _serverEndPoint = null;
        _address = null;
    }

    /// <summary>
    /// Responses from the server will be handled here and should always fire an event after decoding.
    /// </summary>
    /// <exception cref="Exception"></exception>
    private async Task HandleIncomingServerPackets()
    {
        if (_client is null || !Registered) return;
        while (true)
        {
            UdpReceiveResult result;
            try
            {
                result = await _client.ReceiveAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                StaticLogModule.LogError("Error during packet receive", ex);
                continue;
            }

            var opCode = result.Buffer[0];
            switch (opCode)
            {
                case (byte)StaticNetCodes.ServerCode:
                    var server = Server.SetFromPacket(DynamicPacketReaderModule.ReadDirect32Message(result.Buffer));
                    if (server is null)
                    {
                        DisconnectFromServer();
                        continue;
                    }

                    StaticNetClientEvents.ReceivedServerData?.Invoke(this, server);
                    break;
                case (byte)StaticNetCodes.ChangeChannel:
                    StaticNetClientEvents.ReceivedAudio?.Invoke(this,
                        DynamicPacketReaderModule.ReadDirect32Custom(result.Buffer));
                    break;
                case (byte)StaticNetCodes.VoiceCode:
                    break;
                case (byte)StaticNetCodes.ChatMessageCode:
                    StaticNetClientEvents.ReceivedMessageFromServer?.Invoke(this,
                        ChatMessage.SetFromPacket(DynamicPacketReaderModule.ReadDirect32Message(result.Buffer)) ??
                        throw new Exception("Failed to read data"));
                    break;
                default:
                    StaticLogModule.LogDebug("Received unknown packet");
                    break;
            }
        }

        // var opCode = ClientPacketReader.ReadByte();
        // switch (opCode)
        // {
        //     case (byte)StaticNetCodes.ServerCode:
        //         StaticNetClientEvents.ReceivedServerData?.Invoke(this,
        //             Server.SetFromPacket(ClientPacketReader!.Read32Message()) ??
        //             throw new Exception("Failed to read data"));
        //         break;
        //     case (byte)StaticNetCodes.ChatMessageCode:
        //         StaticNetClientEvents.ReceivedMessageFromServer?.Invoke(this,
        //             ChatMessage.SetFromPacket(ClientPacketReader!.Read32Message()) ??
        //             throw new Exception("Failed to read data"));
        //         break;
        //     case (byte)StaticNetCodes.ChangeChannel:
        //         StaticNetClientEvents.OtherUserChangeChannel?.Invoke(this,
        //             (User.SetFromPacket(ClientPacketReader!.Read32Message()),
        //                 Channel.SetFromPacket(ClientPacketReader!.Read32Message())));
        //         break;
        // }
    }
}