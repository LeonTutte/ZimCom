using Spectre.Console;

using System.Data;
using System.Net.Sockets;

using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Dynamic.Net;
using ZimCom.Core.Modules.Static;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Modules.Dynamic;
public class DynamicManagerModuleServerExtras : DynamicManagerModule {
    private List<DynamicNetClient> _clients { get; set; }
    private TcpListener _tcpListener { get; set; }
    public DynamicManagerModuleServerExtras() : base(false) {
        _clients = new List<DynamicNetClient>();
        _address = Server.IpAddress;
        if (_address is null) {
            StaticLogModule.LogError("Error during server initialize, the net address is empty", null);
            throw new NoNullAllowedException();
        }
        _tcpListener = new TcpListener(_address!, _serverPort);
        AttachToServerEvents();
    }
    public void StartServerListener() {
        _tcpListener.Start();
        while (true) {
            DynamicNetClient tempClient = new DynamicNetClient(_tcpListener.AcceptTcpClient());
            _clients.Add(tempClient);
        }
    }
    private void AttachToServerEvents() {
        StaticNetServerEvents.ReceivedUserInformation += (sender, e) => {
            Server!.Channels.FindAll(x => x.DefaultChannel.Equals(true)).First().Participents.Add(e);
            if (Server is not null && Server.KnownUsers!.Any(x => x.Id.Equals(e.Id)) is false) {
                AnsiConsole.MarkupLine($"[yellow]{e.Label}[/] is an previously unknown user");
                Server.KnownUsers.Add(e);
                AnsiConsole.MarkupLine($"[green]Adding[/] [yellow]{e.Label}[/] to default group");
                if (Server.Groups.Any(x => x.IsDefault.Equals(true)) is false) {
                    throw new Exception("No default group defined!");
                }
                Server.UserToGroup!.Add(e.Id.ToString(), Server!.Groups.FindAll(x => x.IsDefault.Equals(true)).First().Label);
                Server.Save();
            } else {
                AnsiConsole.MarkupLine($"[yellow]{e.Label}[/] is an known user");
                if (Server!.BannedUsers.Any(x => x.Id.Equals(e.Id)) is true) {
                    AnsiConsole.MarkupLine($"[yellow]{e.Label}[/] is a [red]banned[/] user -> sending client reject");
                    StaticNetServerEvents.RejectClientUser?.Invoke(this, e);
                    var tempUser = _clients.FirstOrDefault<DynamicNetClient>(x => x.User.Equals(e)) as DynamicNetClient;
                    if (tempUser is not null) {
                        tempUser.TcpClient.Close();
                        _clients.Remove(tempUser);
                    }
                } else {
                    var tempClient = _clients.FirstOrDefault<DynamicNetClient>(x => x.User.Equals(e));
                    if (tempClient is not null) {
                        try {
                            tempClient.TcpClient.Client.Send(Server.GetPacket());
                        } catch (Exception ex) {
                            StaticLogModule.LogError("Error sending server data to client", ex);
                            throw;
                        }
                        AnsiConsole.MarkupLine($"[blue]{tempClient.UID}[/] was given the server information");
                    }
                }
            }
        };
        StaticNetServerEvents.RecievedChatMessage += (sender, e) => {
            if (Server is not null && _clients.Count > 0) {
                Channel? temp = FindUserInChannel(e.User);
                if (temp != null) {
                    SendChannelMessage(e, temp);
                    foreach (var user in temp.Participents.Where(x => x.Id != e.User.Id)) {
                        _clients.First(x => (x.User is null) ? false : x.User.Equals(user)).TcpClient.Client.Send(e.GetPacket());
                    }
                }
            }
        };
        StaticNetServerEvents.UserChannelChange += (sender, e) => {
            if (Server is not null && _clients.Count > 0) {
                Channel? temp = FindUserInChannel(e.Item1);
                if (temp != null) {
                    if (temp.Label != e.Item2.Label) {
                        temp.Participents.Remove(temp.Participents.Where(x => x.Id.Equals(e.Item1.Id)).First());
                    }
                    var serverTemp = Server.Channels.Where(x => x.Label.Equals(e.Item2.Label)).First();
                    serverTemp.Participents.Add(e.Item1);
                    //StaticNetClientEvents.UserChangeChannel?.Invoke(this, (e.Item1, serverTemp));
                    DynamicIoClientPacket packet = new DynamicIoClientPacket();
                    packet.WriteOpCode((byte)StaticNetOpCodes.ChangeChannel);
                    packet.WriteMessage(e.Item1.ToString());
                    packet.WriteMessage(e.Item2.ToString());
                    foreach (var user in temp.Participents.Where(x => x.Id != e.Item1.Id)) {
                        _clients.First(x => (x.User is null) ? false : x.User.Equals(user)).TcpClient.Client.Send(packet.GetPacketBytes());
                    }
                }
            }
        };
    }
}
