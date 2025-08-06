using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZimCom.Core.Modules.Static;
using ZimCom.Core.Modules.Static.Misc;

namespace ZimCom.Core.Models;

public class Channel
{
    public required string Label { get; set; }
    public string? Description { get; set; }
    public bool DefaultChannel { get; set; }

    [JsonIgnore] public bool CurrentChannel { get; set; }

    public bool TitleChannel { get; set; }
    public bool SpacerChannel { get; set; }
    public bool LocalChannel { get; set; }
    public Dictionary<Strength, long> Strengths { get; set; } = new();
    public static byte Slots { get; set; } = 64;

    [JsonIgnore]
    public static bool CustomSlotAmount
    {
        get
        {
            if (Slots != 64) return true;
            return false;
        }
    }

    [JsonIgnore] public ObservableCollection<User> Participents { get; set; } = new();

    [JsonIgnore] public ObservableCollection<ChatMessage> Chat { get; set; } = new();

    public override string ToString()
    {
        return JsonSerializer.Serialize<Channel>(this);
    }

    internal static Channel? SetFromPacket(string data)
    {
        Channel? temp = null;
        try
        {
            temp = JsonSerializer.Deserialize<Channel>(data);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error during channel conversion", ex);
            return null;
        }

        return temp ?? null;
    }
}