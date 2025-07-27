using System.Text.Json;

using ZimCom.Core.Modules.Static;

namespace ZimCom.Core.Models.combined;
public class Servers {
    public List<Server>? ServerList { get; set; }

    public override string ToString() => JsonSerializer.Serialize<Servers>(this);

    public bool Save() {
        try {
            File.WriteAllText(GetFilePath(), this.ToString());
        } catch (Exception) {
            return false;
        }
        return true;
    }

    public static Servers? Load() {
        Servers? tempServerList = null;
        try {
            var content = File.ReadAllText(GetFilePath());
            if (!String.IsNullOrEmpty(content)) tempServerList = JsonSerializer.Deserialize<Servers>(content);
        } catch (Exception ex) {
            StaticLogModule.LogError("Error during serverlist load", ex);
            return null;
        }
        StaticLogModule.LogDebug("Loaded serverlist from disk");
        return tempServerList ?? null;
    }
    public static string GetFilePath() => Path.Combine(StaticLocalPathModule.GetLocalApplicationFolder(), "servers.json");
}
