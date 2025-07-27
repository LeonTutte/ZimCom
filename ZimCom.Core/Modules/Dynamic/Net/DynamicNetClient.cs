using Spectre.Console;

using System.Net.Sockets;

using ZimCom.Core.Models;

namespace ZimCom.Core.Modules.Dynamic.Net;
public class DynamicNetClient {
    public User? User { get; set; }
    public Guid UID { get; set; }
    private TcpClient _tcpClient;

    public DynamicNetClient(TcpClient tcpClient) {
        _tcpClient = tcpClient;
        UID = Guid.NewGuid();
        AnsiConsole.MarkupLine($"Accepted [green]new user[/] session with [blue]{UID.ToString()}[/]");
    }
}
