using System.Net.Sockets;
using Spectre.Console;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Dynamic.Misc;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;
using ZimCom.ServerConsole.Models;

namespace ZimCom.ServerConsole.Modules.Dynamic;

/// <summary>
/// Represents a specialized server extension for the DynamicManagerModule. It's where all the communication is happening from or on the server.
/// Inherits from <see cref="DynamicManagerModule"/> to provide additional functionality specific to server-side operations.
/// </summary>
public class DynamicManagerModuleServerExtras : DynamicManagerModule
{
    // User IP, User Name, Current Channel Name
    private List<NetworkClient>? _networkClients = null;

    // ReSharper disable FunctionNeverReturns
    /// <summary>
    /// Starts listening for network communications on the server port using a UDP client.
    /// </summary>
    /// <remarks>
    /// This method continuously listens for incoming UDP packets, maintains a list of connected _networkClients, and forwards messages to all connected _networkClients except the sender.
    /// </remarks>
    /// <returns>
    /// A task that represents the asynchronous operation of the network listener.
    /// Note that this method does not return under normal operation as it operates in an infinite loop.
    /// </returns>
    public async Task StartNetworkListener()
    {
        UdpClient? listener = null;
        try
        {
            listener = new UdpClient(ServerPort);
            _networkClients = new List<NetworkClient>
            {
                Capacity = 64
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            StaticLogModule.LogError("Error during server initialization", ex);
            Environment.Exit(1);
        }

        while (true)
        {
            var result = await listener.ReceiveAsync().ConfigureAwait(true);
            if (!_networkClients.Any(x => x.EndPoint.Equals(result.RemoteEndPoint)))
                _networkClients.Add(new NetworkClient(result.RemoteEndPoint));

            if (!CheckClientPacket(result, ref listener)) continue;
            // Forward to all other _networkClients
            await ForwardPackageToClients(_networkClients, result, listener).ConfigureAwait(false);
        }
    }

    private static async Task ForwardPackageToClients(List<NetworkClient> clients, UdpReceiveResult result,
        UdpClient listener)
    {
        for (var index = clients.Count - 1; index >= 0; index--)
        {
            var client = clients[index];
            if (!client.EndPoint.Equals(result.RemoteEndPoint))
            {
                await listener.SendAsync(result.Buffer, result.Buffer.Length, client.EndPoint).ConfigureAwait(false);
            }
        }
    }

    private bool CheckClientPacket(UdpReceiveResult receiveResult, ref UdpClient client)
    {
        var opCode = receiveResult.Buffer[0];
        switch (opCode)
        {
            case (byte)StaticNetCodes.RegisterCode:
                AnsiConsole.MarkupLine($"{receiveResult.RemoteEndPoint.Address.MapToIPv6()} registered on server");
                var serverPacket = InternalServer.GetPacket();
                client.SendAsync(serverPacket, serverPacket.Length, receiveResult.RemoteEndPoint).ConfigureAwait(false);
                break;
            case (byte)StaticNetCodes.UnregisterCode:
                AnsiConsole.MarkupLine($"{receiveResult.RemoteEndPoint.Address.MapToIPv6()} unregistered on server");
                _networkClients?.RemoveAll(x => x.EndPoint.Equals(receiveResult.RemoteEndPoint));
                break;
            case (byte)StaticNetCodes.ChangeChannel:
                UpdateClientChannelInfo(receiveResult);
                break;
            case (byte)StaticNetCodes.VoiceCode:
                AnsiConsole.MarkupLine(
                    $"{receiveResult.RemoteEndPoint.Address.MapToIPv6()} send voice packet with {receiveResult.Buffer.Length} bytes");
                ForwardPackageToChannelMemberOfSender(receiveResult, client, _networkClients!);
                return false;
            case (byte)StaticNetCodes.UserCode:
                var user = User.SetFromPacket(DynamicPacketReaderModule.ReadDirect32Message(receiveResult.Buffer)) ??
                           new User("Unknown");
                AnsiConsole.MarkupLine(
                    $"{receiveResult.RemoteEndPoint.Address.MapToIPv6()} identified as a {user.Label} with identifier {user.Id}");
                _networkClients!.First(x => x.EndPoint.Equals(receiveResult.RemoteEndPoint)).UserLabel = user.Label;
                break;
            case (byte)StaticNetCodes.ChatMessageCode:
                // Server doesn't care for the message, just forward it to everyone
                break;
            default:
                AnsiConsole.MarkupLine(
                    $"Error during packet check for {receiveResult.RemoteEndPoint.Address.MapToIPv6()}");
                return false;
        }

        return true;
    }

    private void UpdateClientChannelInfo(UdpReceiveResult receiveResult)
    {
        DynamicPacketReaderModule packetReader = new(receiveResult.Buffer);
        var user = User.SetFromPacket(packetReader.Read32Message()) ?? new User("Unknown");
        var channel = packetReader.Read32Message();
        AnsiConsole.MarkupLine($"{receiveResult.RemoteEndPoint.Address.MapToIPv6()} joined a {channel}");
        _networkClients!.First(x => x.EndPoint.Equals(receiveResult.RemoteEndPoint)).ChannelLabel = channel;
        try
        {
            _networkClients!
                .First(x => x.EndPoint.Equals(receiveResult.RemoteEndPoint) && string.IsNullOrEmpty(x.UserLabel))
                .UserLabel = user.Label;
        }
        catch
        {
            // ignored
        }
    }

    private static void ForwardPackageToChannelMemberOfSender(UdpReceiveResult receiveResult, UdpClient client,
        List<NetworkClient> clients)
    {
        var sender = clients.First(x => x.EndPoint.Equals(receiveResult.RemoteEndPoint));
        if (string.IsNullOrEmpty(sender.ChannelLabel)) return;
        clients.RemoveAll(x => x.ChannelLabel != sender.ChannelLabel);
        ForwardPackageToClients(clients, receiveResult, client).ConfigureAwait(false);
    }
    // ReSharper restore FunctionNeverReturns

    /// <summary>
    /// Performs basic server configuration checks.
    /// </summary>
    /// <remarks>
    /// This method validates the server's configuration. If any check fails, it outputs appropriate error messages and terminates the server.
    /// </remarks>
    public void DoBasicServerConfigChecks()
    {
        var fail = false;
        if (InternalServer.Channels.FindAll(x => x.DefaultChannel.Equals(true)).Count > 1)
        {
            AnsiConsole.MarkupLine("[red]You have more than one default channel configured for this server.[/]");
            fail = true;
        }

        if (InternalServer.Channels.FindAll(x => x.DefaultChannel).Count < 1)
        {
            AnsiConsole.MarkupLine("[red]You have no default channel configured for this server.[/]");
            fail = true;
        }

        if (InternalServer.Channels.FindAll(x => x.LocalChannel.Equals(true)).Count > 0)
        {
            AnsiConsole.MarkupLine("[red]You have at least one local channel configured for this server.[/]");
            fail = true;
        }

        if (InternalServer.Channels.Distinct().Count() != InternalServer.Channels.Count)
        {
            AnsiConsole.MarkupLine("[red]Your channel labels are not unique.[/]");
            fail = true;
        }

        if (InternalServer.Groups.Distinct().Count() != InternalServer.Groups.Count)
        {
            AnsiConsole.MarkupLine("[red]Your group labels are not unique.[/]");
            fail = true;
        }

        if (InternalServer.UserToGroup.Keys.Distinct().Count() != InternalServer.UserToGroup.Keys.Count)
        {
            AnsiConsole.MarkupLine("[red]At least one user has multiple groups.[/]");
            fail = true;
        }

        if (fail)
        {
            AnsiConsole.MarkupLine("[red]The server configuration has failed at least one test.[/]");
            AnsiConsole.MarkupLine("[red]Terminating server...[/]");
            AnsiConsole.MarkupLine("[yellow]Press any key to exit.[/]");
            Console.ReadKey();
            Environment.Exit(-1);
        }

        AnsiConsole.MarkupLine("[green]The server configuration is valid.[/]");
    }
}