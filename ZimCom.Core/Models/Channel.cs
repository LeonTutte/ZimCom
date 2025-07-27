using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

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
}
