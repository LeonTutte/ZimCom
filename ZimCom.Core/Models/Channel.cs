using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZimCom.Core.Modules.Static.Misc;

namespace ZimCom.Core.Models;

/// <summary>
/// Represents a communication channel with various properties and capabilities.
/// </summary>
public class Channel
{
    /// <summary>
    /// The name of the channel.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// An optional description for the channel
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Indicates whether the channel is the default aka the first one to put a client into.
    /// </summary>
    public bool DefaultChannel { get; init; }

    /// <summary>
    /// An internal bool for the context of the server, to indicate that this is the channel the client is currently active in.
    /// </summary>
    [JsonIgnore]
    public bool CurrentChannel { get; set; }

    /// <summary>
    /// Indicates whether the channel acts as a title. Only for clientside formatting.
    /// </summary>
    public bool TitleChannel { get; init; }

    /// <summary>
    /// Indicates whether the channel acts as a spacer. Only for clientside formatting.
    /// </summary>
    public bool SpacerChannel { get; init; }

    /// <summary>
    /// Indicates whether the channel is local. Only important for the client.
    /// </summary>
    public bool LocalChannel { get; init; }

    /// <summary>
    /// A dictionary that maps different strengths to their corresponding values.
    /// Strengths are defined by the <see cref="Strength"/> enum and represent various permissions or capabilities within a channel.
    /// </summary>
    public Dictionary<Strength, long> Strengths { get; init; } = [];

    /// <summary>
    /// The number of slots is used to determine the amount of pre-reserved ram for the channel.
    /// </summary>
    public static byte Slots { get; set; } = 64;

    /// <summary>
    /// Indicates whether the number of slots in a channel is custom (not the default value).
    /// </summary>
    [JsonIgnore]
    public static bool CustomSlotAmount => Slots != 64;

    /// <summary>
    /// Gets or sets the collection of users who are participants in this channel.
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<User> Participants { get; set; } = [];

    /// <summary>
    /// The chat messages associated with the channel.
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<ChatMessage> Chat { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => JsonSerializer.Serialize<Channel>(this);

    internal static Channel? SetFromPacket(string data)
    {
        Channel? temp;
        try
        {
            temp = JsonSerializer.Deserialize<Channel>(data);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError($"Error during {nameof(temp)} conversion", ex);
            return null;
        }

        return temp ?? null;
    }
}