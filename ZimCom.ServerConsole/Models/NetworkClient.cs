using System.Net;

namespace ZimCom.ServerConsole.Models;

public class NetworkClient(IPEndPoint endPoint)
{
    public IPEndPoint EndPoint = endPoint;
    public string? UserLabel;
    public string? ChannelLabel;
}