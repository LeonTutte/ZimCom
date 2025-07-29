using Spectre.Console;

using System.Net;

using ZimCom.Core.Modules.Dynamic;
using ZimCom.ServerConsole.Modules.Static;

namespace ZimCom.ServerConsole;

internal class Program {
    private static DynamicManagerModuleServerExtras? _dynamicManagerModule;
    static void Main(string[] args) {
        StaticLogWrapper.WriteAnsiMarkupDebug("Starting [green]ZimCom Server[/]!");
        StaticLogWrapper.WriteAnsiMarkupDebug("Loaded [blue]configuration[/] files ");
        _dynamicManagerModule = new DynamicManagerModuleServerExtras();
        if (_dynamicManagerModule.Server.Label == "Default Server") {
            StaticLogWrapper.WriteAnsiMarkupDebug("Created a [yellow]new Server[/] with [blue]default[/] values");
            _dynamicManagerModule.Server.Save();
        }
        AnsiConsole.MarkupLine($"Counting {_dynamicManagerModule.Server.Channels.Count} Channels");
        AnsiConsole.MarkupLine($"Counting {_dynamicManagerModule.Server.Groups.Count} Groups");
        AnsiConsole.MarkupLine($"Counting {_dynamicManagerModule.Server.UserToGroup!.Count} user to group combinations");
        StaticLogWrapper.WriteAnsiMarkupDebug($"Starting server on [blue]{_dynamicManagerModule.Server.HostName}[/] and [blue]{_dynamicManagerModule.Server.IpAddress.MapToIPv4().ToString()}[/] | [blue]{_dynamicManagerModule.Server.IpAddress.ToString()}[/]");

        _dynamicManagerModule.StartServerListener();

        //int voicePort = 44111;
        //int serverPort = 44112;
        //StaticLogWrapper.WriteAnsiMarkupDebug($"Using Ports [blue]{voicePort}[/] and [blue]{serverPort}[/]");

        //UdpClient voiceClient = new UdpClient(voicePort);
        //UdpClient serverClient = new UdpClient(serverPort);
        //IPEndPoint voiceEndpoint = new IPEndPoint(_dynamicServerManagerModule.Server.IpAddress, voicePort);
        //IPEndPoint serverEndpoint = new IPEndPoint(_dynamicServerManagerModule.Server.IpAddress, serverPort);
        //StaticLogWrapper.WriteAnsiMarkupDebug("Created [blue]UDP and TCP listeners[/] ");

        //try {
        //    AnsiConsole.MarkupLine("I am [green]alive[/]");
        //    AnsiConsole.MarkupLine($"Waiting for incoming [blue]data[/] on [blue]{voicePort}[/] and [blue]{serverPort}[/]");
        //    while (true) {
        //        byte[] voiceBytes = voiceClient.Receive(ref voiceEndpoint);
        //        byte[] voiceCode = voiceBytes[0..4];
        //        if (voiceCode == StaticPacketConverter.GetHelloPacket()) {
        //            AnsiConsole.MarkupLine($"User connected");
        //        } else if (voiceCode == StaticPacketConverter.GetChannelRequestPacket()) {
        //            AnsiConsole.MarkupLine("Recieved request for Channellist");
        //            CerasSerializer cerasSerializer = new CerasSerializer();
        //            byte[] sendBytes = StaticPacketConverter.GetChannelReceivePacket(cerasSerializer.Serialize(_dynamicServerManagerModule.Server.Channels));
        //            voiceClient.Send(sendBytes, sendBytes.Length, voiceEndpoint);
        //        } else {
        //            //WaveIn_DataAvailable();
        //        }
        //    }
        //} catch (SocketException ex) {
        //    AnsiConsole.MarkupLine($"There was an [red]exception[/]:  {ex.Message}");
        //    StaticLogModule.LogError("There was an exception during the server process", ex);
        //} finally {
        //    voiceClient.Close();
        //    serverClient.Close();
        //    StaticLogWrapper.WriteAnsiMarkupDebug("Closed [blue]UDP and TCP listeners[/]");
        //}
    }
    static void WaveIn_DataAvailable(byte[] e, IPAddress ipAddress) {
        byte[] values = new byte[e.Length];
        Buffer.BlockCopy(e, 0, values, 0, e.Length);
        // Get Channel where IP is included
        // Send copy to all participents
    }
}