using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Models;

public partial class User : ObservableObject, IJsonModel<User>
{
    [SetsRequiredMembers]
    public User(string label)
    {
        (Id, Label) = (Guid.NewGuid(), label);
    }

    public required Guid Id { get; set; } = Guid.NewGuid();
    public required string Label { get; set; }

    [JsonIgnore] public IPAddress? Address { get; set; }

    [JsonIgnore] [ObservableProperty] public partial bool IsMuted { get; set; } = false;

    [JsonIgnore] [ObservableProperty] public partial bool IsAway { get; set; } = false;

    [JsonIgnore] [ObservableProperty] public partial bool HasOthersMuted { get; set; } = false;

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

    public static User? Load()
    {
        User? tempUser = null;
        try
        {
            var content = File.ReadAllText(GetFilePath());
            if (!string.IsNullOrEmpty(content)) tempUser = JsonSerializer.Deserialize<User>(content);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error during user load", ex);
            return null;
        }

        StaticLogModule.LogDebug("Loaded user from disk");
        return tempUser ?? null;
    }

    public static string GetFilePath()
    {
        return Path.Combine(StaticLocalPathModule.GetLocalApplicationFolder(), "user.json");
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize<User>(this);
    }

    public byte[] GetPacket()
    {
        var packet = new DynamicIoClientPacket();
        packet.WriteOpCode((byte)StaticNetOpCodes.UserCode);
        packet.WriteMessage(JsonSerializer.Serialize<User>(this));
        return packet.GetPacketBytes();
    }

    public static User? SetFromPacket(string data)
    {
        User? tempUser = null;
        try
        {
            tempUser = JsonSerializer.Deserialize<User>(data);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error during user conversion", ex);
            return null;
        }

        return tempUser ?? null;
    }
}