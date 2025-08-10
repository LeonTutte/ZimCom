using System.Text.Json;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Models;

/// <summary>
/// Represents a chat message inside a channel.
/// </summary>
/// <remarks>
/// Its properties cannot be modified after initialization.
/// </remarks>
/// <param name="user">The user from wich the message originates from.</param>
/// <param name="message">The message content.</param>
public class ChatMessage(User user, string message)
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public User User { get; init; } = user;

    /// <summary>
    /// Gets the message content sent by the user.
    /// </summary>
    public string Message { get; init; } = message;

    /// <summary>
    /// Gets the date and time when this chat message was created.
    /// This property is initialized to the current UTC date and time when a new instance of ChatMessage is created.
    /// </summary>
    public DateTime DateTime { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public override string ToString() => JsonSerializer.Serialize<ChatMessage>(this);

    /// <summary>
    /// Converts the chat message to a byte array packet for network transmission.
    /// </summary>
    /// <returns>A byte array representing the serialized chat message packet.</returns>
    public byte[] GetPacket()
    {
        var packet = new DynamicPacketBuilderModule();
        packet.WriteOperationCode((byte)StaticNetOpCodes.ChatMessageCode);
        packet.WriteMessage(JsonSerializer.Serialize<ChatMessage>(this));
        return packet.GetPacketBytes();
    }

    /// <summary>
    /// Attempts to create a new ChatMessage instance from serialized data.
    /// </summary>
    /// <param name="data">The serialized data string to deserialize into a ChatMessage object.</param>
    /// <returns>A newly created ChatMessage object if deserialization is successful, otherwise null.</returns>
    public static ChatMessage? SetFromPacket(string data)
    {
        ChatMessage? temp;
        try
        {
            temp = JsonSerializer.Deserialize<ChatMessage>(data);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError($"Error during {nameof(temp)} conversion", ex);
            return null;
        }

        return temp ?? null;
    }
}