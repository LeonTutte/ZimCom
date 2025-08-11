using System.Collections.ObjectModel;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;
using Spectre.Console;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Dynamic.Misc;
using ZimCom.Core.Modules.Dynamic.Net;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.ServerConsole.Modules.Dynamic;

/// <summary>
/// Represents a specialized server extension for the DynamicManagerModule. It's where all the communication is happening from or on the server.
/// Inherits from <see cref="DynamicManagerModule"/> to provide additional functionality specific to server-side operations.
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("macOS")]
public class DynamicManagerModuleServerExtras : DynamicManagerModule
{
    public DynamicManagerModuleServerExtras()
    {
        TcpListener = new TcpListener(Server.GetV6Address()!, ServerPort);
        AttachToServerEvents();
    }

    private QuicServerConnectionOptions _serverConnectionOptions = new()
    {
        DefaultStreamErrorCode = StaticNetLCodes.GetDefaultStreamErrorCode,
        DefaultCloseErrorCode = StaticNetLCodes.GetDefaultCloseErrorCode,
        // TODO: Should be set to sum of all channel slots
        MaxInboundUnidirectionalStreams = 100,
        MaxInboundBidirectionalStreams = 100,
        // Same options as for server side SslStream.
        ServerAuthenticationOptions = new SslServerAuthenticationOptions
        {
            // Specify the application protocols that the server supports. This list must be a subset of the protocols specified in QuicListenerOptions.ApplicationProtocols.
            ApplicationProtocols = [new SslApplicationProtocol("zimcom-server")],
        }
    };

    /// <summary>
    /// Gets a collection of active network clients managed by this instance.
    /// </summary>
    private List<DynamicNetClient> Clients { get; } = [];

    /// <summary>
    /// Gets a collection of active QUIC connections managed by this instance.
    /// </summary>
    private ObservableCollection<QuicConnection> QuicConnections { get; } = [];

    private QuicListener? QuicListener { get; set; }
    private TcpListener TcpListener { get; }

    // ReSharper disable FunctionNeverReturns
    /// <summary>
    /// Starts the TCP listener on the server.
    /// </summary>
    /// <remarks>
    /// This method initializes and starts a TCP listener to accept incoming connections from clients.
    /// It continuously listens for new client connections, accepting them as they arrive,
    /// and adds each connected client to the list of managed clients. The server will display
    /// a status message indicating that it is listening via TCP.
    /// </remarks>
    public void StartTcpListener()
    {
        TcpListener.Start();
        AnsiConsole.MarkupLine("[green]Server listening via TCP ...[/]");
        while (true)
        {
            var tempClient = new DynamicNetClient(TcpListener.AcceptTcpClientAsync().GetAwaiter().GetResult());
            Clients.Add(tempClient);
        }
    }

    /// <summary>
    /// Starts the QUIC listener on the server.
    /// </summary>
    /// <remarks>
    /// This method initializes and starts a QUIC listener to accept incoming connections from clients.
    /// It sets up the listener with specified options, including the endpoint, supported application protocols, and connection options callback.
    /// The server will display a status message indicating that it is listening via QUIC.
    /// </remarks>
    public async void StartQuicListener()
    {
        try
        {
            QuicListener = await QuicListener.ListenAsync(new QuicListenerOptions
            {
                // Define the endpoint on which the server will listen for incoming connections. The port number 0 can be replaced with any valid port number as needed.
                ListenEndPoint = new IPEndPoint(Server.GetV6Address(), QuicPort),
                // List of all supported application protocols by this listener.
                ApplicationProtocols = [new SslApplicationProtocol("zimcom-server")],
                // Callback to provide options for the incoming connections, it gets called once per each connection.
                ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(_serverConnectionOptions)
            }).ConfigureAwait(false);
            AnsiConsole.MarkupLine("[green]Server listening via QUIC...[/]");
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }

        bool quicListenerActive = true;
        // Accept and process the connections.
        while (quicListenerActive)
        {
            // Accept will propagate any exceptions that occurred during the connection establishment,
            // including exceptions thrown from ConnectionOptionsCallback, caused by invalid QuicServerConnectionOptions or TLS handshake failures.
            try
            {
                var tempClient = await QuicListener!.AcceptConnectionAsync().ConfigureAwait(false);
                AnsiConsole.MarkupLine($"[green]Client connected[/] -> {tempClient.RemoteEndPoint}");
                QuicConnections.Add(tempClient);
            }
            catch (Exception e)
            {
                AnsiConsole.WriteException(e);
                quicListenerActive = false;
            }
        }

        // When finished, dispose the listener.
        await QuicListener!.DisposeAsync().ConfigureAwait(true);
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
        bool fail = false;
        if (Server.Channels.FindAll(x => x.DefaultChannel.Equals(true)).Count > 1)
        {
            AnsiConsole.MarkupLine("[red]You have more than one default channel configured for this server.[/]");
            fail = true;
        }

        if (Server.Channels.FindAll(x => x.DefaultChannel).Count < 1)
        {
            AnsiConsole.MarkupLine("[red]You have no default channel configured for this server.[/]");
            fail = true;
        }

        if (Server.Channels.FindAll(x => x.LocalChannel.Equals(true)).Count > 0)
        {
            AnsiConsole.MarkupLine("[red]You have at least one local channel configured for this server.[/]");
            fail = true;
        }

        if (Server.Channels.Distinct().Count() != Server.Channels.Count)
        {
            AnsiConsole.MarkupLine("[red]Your channel labels are not unique.[/]");
            fail = true;
        }

        if (Server.Groups.Distinct().Count() != Server.Groups.Count)
        {
            AnsiConsole.MarkupLine("[red]Your group labels are not unique.[/]");
            fail = true;
        }

        if (Server.UserToGroup.Keys.Distinct().Count() != Server.UserToGroup.Keys.Count)
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

    private void AttachToServerEvents()
    {
        StaticNetServerEvents.ReceivedUserInformation += (_, e) =>
        {
            Server.Channels.FindAll(x => x.DefaultChannel.Equals(true)).First().Participants.Add(e);
            if (Server.KnownUsers.Any(x => x.Id.Equals(e.Id)) is false)
            {
                AnsiConsole.MarkupLine($"[yellow]{e.Label}[/] is an previously unknown user");
                Server.KnownUsers.Add(e);
                AnsiConsole.MarkupLine($"[green]Adding[/] [yellow]{e.Label}[/] to default group");
                if (Server.Groups.Any(x => x.IsDefault.Equals(true)) is false)
                    throw new Exception("No default group defined!");
                Server.UserToGroup.Add(e.Id.ToString(),
                    Server.Groups.FindAll(x => x.IsDefault.Equals(true)).First().Label);
                Server.Save();
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]{e.Label}[/] is an known user");
                if (Server.BannedUsers.Any(x => x.Id.Equals(e.Id)))
                {
                    AnsiConsole.MarkupLine($"[green]{e.Label}[/] is a [red]banned[/] user -> sending client reject");
                    StaticNetServerEvents.RejectClientUser?.Invoke(this, e);
                    var tempUser = Clients.FirstOrDefault(x => x.User!.Equals(e));
                    if (tempUser is null) return;
                    tempUser.TcpClient.Close();
                    Clients.Remove(tempUser);
                }
                else
                {
                    var tempClient = Clients.FirstOrDefault(x => x.User!.Equals(e));
                    if (tempClient is null) return;
                    try
                    {
                        tempClient.TcpClient.Client.Send(Server.GetPacket());
                    }
                    catch (Exception ex)
                    {
                        StaticLogModule.LogError("Error sending server data to client", ex);
                        throw;
                    }

                    AnsiConsole.MarkupLine($"Session [blue]{tempClient.Uid}[/] was given the server information");
                }
            }
        };
        StaticNetServerEvents.ReceivedChatMessage += (_, e) =>
        {
            if (Clients.Count <= 0) return;
            var temp = FindUserInChannel(e.User);
            if (temp == null) return;
            SendChannelMessage(e, temp);
            foreach (var user in temp.Participants.Where(x => x.Id != e.User.Id))
                Clients.First(x => x.User is not null && x.User.Equals(user)).TcpClient.Client
                    .Send(e.GetPacket());
        };
        StaticNetServerEvents.UserChannelChange += (_, e) =>
        {
            if (Clients.Count <= 0) return;
            var temp = FindUserInChannel(e.Item1);
            if (temp == null) return;
            if (temp.Label != e.Item2.Label)
                temp.Participants.Remove(temp.Participants.First(x => x.Id.Equals(e.Item1.Id)));
            var serverTemp = Server.Channels.First(x => x.Label.Equals(e.Item2.Label));
            serverTemp.Participants.Add(e.Item1);
            StaticNetClientEvents.UserChangeChannel?.Invoke(this, (e.Item1, serverTemp));
            var packet = new DynamicPacketBuilderModule();
            packet.WriteOperationCode((byte)StaticNetCodes.ChangeChannel);
            packet.WriteMessage(e.Item1.ToString());
            packet.WriteMessage(e.Item2.ToString());
            foreach (var user in temp.Participants.Where(x => x.Id != e.Item1.Id))
                Clients.First(x => x.User is not null && x.User.Equals(user)).TcpClient.Client
                    .Send(packet.GetPacketBytes());
        };
    }
}