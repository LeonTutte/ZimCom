using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Models;
public class Server {
    public required Guid Id { get; set; }
    public required string Label { get; set; }
    [JsonIgnore]
    public IPAddress IpAddress { get; set; } = GetIPAddress();
    [JsonIgnore]
    public string HostName { get; set; } = GetHostName();

    public List<Channel> Channels { get; set; } = new List<Channel>();
    public List<Group> Groups { get; set; } = new List<Group>();
    public List<User> BannedUsers { get; set; } = new List<User>();
    public List<User> KnownUsers { get; set; } = new List<User>();
    public Dictionary<string, String> UserToGroup { get; set; } = new Dictionary<string, String>();

    public override string ToString() => JsonSerializer.Serialize<Server>(this, options: new JsonSerializerOptions { WriteIndented = true });

    public bool Save() {
        try {
            File.WriteAllText(GetFilePath(), this.ToString());
        } catch (Exception) {
            return false;
        }
        return true;
    }

    public static Server? Load() {
        Server? tempServer = null;
        try {
            var content = File.ReadAllText(GetFilePath());
            if (!String.IsNullOrEmpty(content)) tempServer = JsonSerializer.Deserialize<Server>(content);
        } catch (Exception ex) {
            StaticLogModule.LogError("Error during server load", ex);
            return null;
        }
        StaticLogModule.LogDebug("Loaded server from disk");
        return tempServer ?? null;
    }
    public static string GetFilePath() => Path.Combine(StaticLocalPathModule.GetLocalApplicationFolder(), "server.json");

    public static string GetHostName() {
        try {
            return Dns.GetHostName();
        } catch (Exception ex) {
            StaticLogModule.LogError("Could not retrieve hostname", ex);
        }
        return "127.0.0.1";
    }

    public static IPAddress GetIPAddress() {
        try {
            return IPAddress.Any; //Dns.GetHostAddresses(GetHostName()).First(); gives address for all services and not TCP / UDP
        } catch (Exception ex) {
            StaticLogModule.LogError("Could not retrieve local ip address", ex);
        }
        return IPAddress.Any;
    }

    public byte[] GetPacket() {
        DynamicIoClientPacket packet = new DynamicIoClientPacket();
        packet.WriteOpCode((byte)StaticNetOpCodes.ServerCode);
        packet.WriteMessage(JsonSerializer.Serialize<Server>(this));
        return packet.GetPacketBytes();
    }

    public static Server? SetFromPacket(string data) {
        Server? tempServer = null;
        try {
            tempServer = JsonSerializer.Deserialize<Server>(data);
        } catch (Exception ex) {
            StaticLogModule.LogError("Error during server conversion", ex);
            return null;
        }
        return tempServer ?? null;
    }
}
