using System.Net;

namespace ZimCom.ServerConsole.Models;

/// <summary>
/// Represents a network client with an associated address, port, user label, and channel label.
/// </summary>
public class NetworkClient(IPEndPoint endPoint)
{
    /// <summary>
    /// The address and port of the client
    /// </summary>
    public IPEndPoint EndPoint = endPoint;
    /// <summary>
    /// String value of the user, used for logging
    /// </summary>
    public string? UserLabel;
    ///<summary>
    /// String value of the channel name, the user is currently in
    /// </summary>
    public string? ChannelLabel;
}