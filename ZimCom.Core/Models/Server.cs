using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using ZimCom.Core.Modules.Static;

namespace ZimCom.Core.Models;
public class Server {
    public required Guid Id { get; set; }
    public required string Label { get; set; }
    [JsonIgnore]
    public IPAddress IpAddress { get; set; } = GetIPAddress();
    [JsonIgnore]
    public string HostName { get; set; } = GetHostName();

    public List<Channel> Channels { get; set; } = new List<Channel>();

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
}
