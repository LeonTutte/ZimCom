using System.Net;
using System.Net.Sockets;
using Spectre.Console;
using ZimCom.Core.Modules.Dynamic.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.ServerConsole.Modules.Dynamic;

/// <summary>
/// Represents a specialized server extension for the DynamicManagerModule. It's where all the communication is happening from or on the server.
/// Inherits from <see cref="DynamicManagerModule"/> to provide additional functionality specific to server-side operations.
/// </summary>
public class DynamicManagerModuleServerExtras : DynamicManagerModule
{
    // ReSharper disable FunctionNeverReturns
    /// <summary>
    /// Starts listening for network communications on the server port using a UDP client.
    /// </summary>
    /// <remarks>
    /// This method continuously listens for incoming UDP packets, maintains a list of connected clients, and forwards messages to all connected clients except the sender.
    /// </remarks>
    /// <returns>
    /// A task that represents the asynchronous operation of the network listener.
    /// Note that this method does not return under normal operation as it operates in an infinite loop.
    /// </returns>
    public async Task StartNetworkListener()
    {
        var server = new UdpClient(ServerPort);
        var clients = new List<IPEndPoint>();

        while (true)
        {
            var result = await server.ReceiveAsync().ConfigureAwait(true);
            if (!clients.Contains(result.RemoteEndPoint))
                clients.Add(result.RemoteEndPoint);

            CheckClientPacket(result);

            // Forward to all other clients
            foreach (var client in clients.Where(client => !client.Equals(result.RemoteEndPoint)))
            {
                await server.SendAsync(result.Buffer, result.Buffer.Length, client).ConfigureAwait(false);
            }
        }
    }

    private void CheckClientPacket(UdpReceiveResult receiveResult)
    {
        var opCode = receiveResult.Buffer[0];
        switch (opCode)
        {
            case (byte)StaticNetCodes.RegisterCode:
                AnsiConsole.MarkupLine($"{receiveResult.RemoteEndPoint.Address.MapToIPv6()} registered on server");
                break;
            default:
                break;
        }
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