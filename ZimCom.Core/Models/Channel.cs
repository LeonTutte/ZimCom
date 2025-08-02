using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

using ZimCom.Core.Modules.Static;

namespace ZimCom.Core.Models;
public class Channel {
    public required string Label { get; set; }
    public string? Description { get; set; }
    public bool DefaultChannel { get; set; } = false;
    [JsonIgnore]
    public bool CurrentChannel { get; set; } = false;
    public bool TitleChannel { get; set; } = false;
    public bool SpacerChannel { get; set; } = false;
    public bool LocalChannel { get; set; } = false;
    public Dictionary<Strength, Int64> Strengths { get; set; } = new Dictionary<Strength, Int64>();
    public static byte Slots { get; set; } = 64;
    [JsonIgnore]
    public static bool CustomSlotAmount {
        get {
            if (Slots != 64) return true;
            return false;
        }
    }
    [JsonIgnore]
    public ObservableCollection<User> Participents { get; set; } = new ObservableCollection<User>();
    [JsonIgnore]
    public ObservableCollection<ChatMessage> Chat { get; set; } = new ObservableCollection<ChatMessage>();

    public override string ToString() => JsonSerializer.Serialize<Channel>(this);

    internal static Channel? SetFromPacket(string data) {
        Channel? temp = null;
        try {
            temp = JsonSerializer.Deserialize<Channel>(data);
        } catch (Exception ex) {
            StaticLogModule.LogError("Error during channel conversion", ex);
            return null;
        }
        return temp ?? null;
    }
}
