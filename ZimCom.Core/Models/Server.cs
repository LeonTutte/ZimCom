using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Models;

public class Server
{
    private IPAddress? _v6Address, _v4Address;
    public required Guid Id { get; set; }
    public required string Label { get; set; }

    public IPAddress GetV4Address() => _v4Address ?? GetHostAddress(ServerUrl ?? HostName, AddressFamily.InterNetwork);
    public IPAddress GetV6Address() => _v6Address ?? GetHostAddress(ServerUrl ?? HostName, AddressFamily.InterNetworkV6);
    public static IPAddress GetLocalAnyAddress() => IPAddress.Any;

    [JsonIgnore] public static string HostName { get; set; } = GetHostName();
    public static string? ServerUrl { get; set; }
    public List<Channel> Channels { get; set; } = new();
    public List<Group> Groups { get; set; } = new();
    public List<User> BannedUsers { get; set; } = new();
    public List<User> KnownUsers { get; set; } = new();
    public Dictionary<string, string> UserToGroup { get; set; } = new();

    public override string ToString()
    {
        return JsonSerializer.Serialize<Server>(this, new JsonSerializerOptions { WriteIndented = true });
    }

    public bool Save()
    {
        try
        {
            File.WriteAllText(GetFilePath(), ToString());
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public static Server? Load()
    {
        Server? tempServer = null;
        try
        {
            var content = File.ReadAllText(GetFilePath());
            if (!string.IsNullOrEmpty(content)) tempServer = JsonSerializer.Deserialize<Server>(content);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error during server load", ex);
            return null;
        }

        StaticLogModule.LogDebug("Loaded server from disk");
        return tempServer ?? null;
    }

    public static string GetFilePath()
    {
        return Path.Combine(StaticLocalPathModule.GetLocalApplicationFolder(), "server.json");
    }

    public static string GetHostName()
    {
        try
        {
            return Dns.GetHostName();
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Could not retrieve hostname", ex);
        }

        return "127.0.0.1";
    }
    
    public static IPAddress GetHostAddress(string hostname, AddressFamily addressFamily) => Dns.GetHostAddresses(hostname, addressFamily).First();

    public static IPAddress GetIpAddress()
    {
        try
        {
            return
                IPAddress.Any; //Dns.GetHostAddresses(GetHostName()).First(); gives address for all services and not TCP / UDP
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Could not retrieve local ip address", ex);
        }

        return IPAddress.Any;
    }

    public byte[] GetPacket()
    {
        var packet = new DynamicPacketBuilderModule();
        packet.WriteOperationCode((byte)StaticNetOpCodes.ServerCode);
        packet.WriteMessage(JsonSerializer.Serialize(this));
        return packet.GetPacketBytes();
    }

    public static Server? SetFromPacket(string data)
    {
        Server? temp = null;
        try
        {
            temp = JsonSerializer.Deserialize<Server>(data);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError($"Error during {nameof(temp)} conversion", ex);
            return null;
        }

        return temp ?? null;
    }
}