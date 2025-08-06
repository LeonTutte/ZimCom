using System.Text.Json;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Models;

public class ChatMessage
{
    public ChatMessage(User user, string message)
    {
        User = user;
        Message = message;
    }

    public User User { get; set; }
    public string Message { get; set; }
    public DateTime DateTime { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return JsonSerializer.Serialize<ChatMessage>(this);
    }

    public byte[] GetPacket()
    {
        var packet = new DynamicIoClientPacket();
        packet.WriteOpCode((byte)StaticNetOpCodes.ChatMessageCode);
        packet.WriteMessage(JsonSerializer.Serialize<ChatMessage>(this));
        return packet.GetPacketBytes();
    }

    public static ChatMessage? SetFromPacket(string data)
    {
        ChatMessage? temp = null;
        try
        {
            temp = JsonSerializer.Deserialize<ChatMessage>(data);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error during chat message conversion", ex);
            return null;
        }

        return temp ?? null;
    }
}