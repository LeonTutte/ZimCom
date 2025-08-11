using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Runtime.Versioning;
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
    private QuicClientConnectionOptions? _clientConnectionOptions;

    /// <summary>
    /// Attempts to connect the client to a server using the specified address. It's the main function where the communication starts.
    /// </summary>
    /// <param name="address">The IP address of the server as a string. If null or whitespace, the connection will not be established.</param>
    public void ConnectToServer(string? address)
    {
        if (!string.IsNullOrWhiteSpace(address)) IPAddress.TryParse(address.AsSpan(), out _address);
        if (_address is null) return;
        if (TcpClient.Connected) return;
        try
        {
            TcpClient.Connect(_address, ServerPort);
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

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macOS")]
    public async Task ConnectToServerViaQuic(string? address, User user)
    {
        if (!string.IsNullOrWhiteSpace(address) && _address is null) IPAddress.TryParse(address.AsSpan(), out _address);
        if (_address is null) return;
        _clientConnectionOptions = new()
        {
            // End point of the server to connect to.
            RemoteEndPoint = new IPEndPoint(_address, QuicPort),

            // Used to abort stream if it's not properly closed by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultStreamErrorCode = 0x0A, // Protocol-dependent error code.

            // Used to close the connection if it's not done by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultCloseErrorCode = 0x0B, // Protocol-dependent error code.

            // Optionally set limits for inbound streams.
            MaxInboundUnidirectionalStreams = 10,
            MaxInboundBidirectionalStreams = 10,

            // Same options as for client side SslStream.
            ClientAuthenticationOptions = new SslClientAuthenticationOptions
            {
                // List of supported application protocols.
                ApplicationProtocols = [new SslApplicationProtocol("zimcom-server")],
            }
        };
        // Initialize, configure and connect to the server.
        var connection = await QuicConnection.ConnectAsync(_clientConnectionOptions);

        Console.WriteLine($"Connected {connection.LocalEndPoint} --> {connection.RemoteEndPoint}");

        // Open a bidirectional (can both read and write) outbound stream.
        // Opening a stream reserves it but does not notify the peer or send any data. If you don't send data, the peer
        // won't be informed about the stream, which can cause AcceptInboundStreamAsync() to hang. To avoid this, ensure
        // you send data on the stream to properly initiate communication.
        var outgoingStream =
            await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional).ConfigureAwait(false);

        // Work with the outgoing stream ...
        await outgoingStream.WriteAsync(user.GetPacket()).ConfigureAwait(true);
        // To accept any stream on a client connection, at least one of MaxInboundBidirectionalStreams or MaxInboundUnidirectionalStreams of QuicConnectionOptions must be set.
        while (outgoingStream.ReadsClosed.IsCompleted)
        {
            // Accept an inbound stream.
            var incomingStream = await connection.AcceptInboundStreamAsync().ConfigureAwait(true);
            // Work with the incoming stream ...
        }

        // Close the connection with the custom code.
        await connection.CloseAsync(0x0C).ConfigureAwait(false);

        // Dispose the connection.
        await connection.DisposeAsync().ConfigureAwait(false);
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
            packet.WriteOperationCode((byte)StaticNetCodes.ChangeChannel);
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