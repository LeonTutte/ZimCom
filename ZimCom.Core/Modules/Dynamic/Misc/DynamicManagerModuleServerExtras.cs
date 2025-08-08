using System.Data;
using System.Net.Sockets;
using Spectre.Console;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Dynamic.Net;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Modules.Dynamic.Misc;

/// <summary>
/// Represents a specialized server extension for the DynamicManagerModule. It's where all the communication is happening from or on the server.
/// Inherits from <see cref="DynamicManagerModule"/> to provide additional functionality specific to server-side operations.
/// </summary>
public class DynamicManagerModuleServerExtras : DynamicManagerModule
{
    public DynamicManagerModuleServerExtras()
    {
        Address = Server.GetLocalAnyAddress();
        if (Address is null)
        {
            StaticLogModule.LogError("Error during server initialize, the net address is empty", null);
            Environment.Exit(-1);
        }

        TcpListener = new TcpListener(Address!, ServerPort);
        AttachToServerEvents();
    }

    private List<DynamicNetClient> Clients { get; } = new();
    private TcpListener TcpListener { get; }
    // ReSharper disable FunctionNeverReturns
    public void StartServerListener()
    {
        TcpListener.Start();
        AnsiConsole.MarkupLine("[green]Server listening ...[/]");
        while (true)
        {
            var tempClient = new DynamicNetClient(TcpListener.AcceptTcpClient());
            Clients.Add(tempClient);
        }
    }
    // ReSharper restore FunctionNeverReturns

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
            Server.Channels.FindAll(x => x.DefaultChannel.Equals(true)).First().Participents.Add(e);
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
            foreach (var user in temp.Participents.Where(x => x.Id != e.User.Id))
                Clients.First(x => x.User is not null && x.User.Equals(user)).TcpClient.Client
                    .Send(e.GetPacket());
        };
        StaticNetServerEvents.UserChannelChange += (_, e) =>
        {
            if (Clients.Count <= 0) return;
            var temp = FindUserInChannel(e.Item1);
            if (temp == null) return;
            if (temp.Label != e.Item2.Label)
                temp.Participents.Remove(temp.Participents.First(x => x.Id.Equals(e.Item1.Id)));
            var serverTemp = Server.Channels.First(x => x.Label.Equals(e.Item2.Label));
            serverTemp.Participents.Add(e.Item1);
            StaticNetClientEvents.UserChangeChannel?.Invoke(this, (e.Item1, serverTemp));
            var packet = new DynamicPacketBuilderModule();
            packet.WriteOperationCode((byte)StaticNetOpCodes.ChangeChannel);
            packet.WriteMessage(e.Item1.ToString());
            packet.WriteMessage(e.Item2.ToString());
            foreach (var user in temp.Participents.Where(x => x.Id != e.Item1.Id))
                Clients.First(x => x.User is not null && x.User.Equals(user)).TcpClient.Client
                    .Send(packet.GetPacketBytes());
        };
    }
}