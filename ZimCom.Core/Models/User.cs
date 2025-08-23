using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Models;

/// <summary>
/// Represents a user in the application.
/// </summary>
public partial class User : ObservableObject
{
    /// <summary>
    /// Create a new user, with a unique guid.
    /// </summary>
    /// <param name="label">The name of the user.</param>
    [SetsRequiredMembers]
    public User(string label) => (Id, Label) = (Guid.NewGuid(), label);

    /// <summary>
    /// Gets the unique identifier for the user.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or initializes the label of the user.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets or sets a bool indicating whether the user is muted.
    /// </summary>
    [JsonIgnore]
    [ObservableProperty]
    public partial bool IsMuted { get; set; } = false;

    /// <summary>
    /// Gets or sets a bool indicating whether the user is away.
    /// </summary>
    [JsonIgnore]
    [ObservableProperty]
    public partial bool IsAway { get; set; } = false;

    [ObservableProperty] public partial UserSettings UserSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets a bool indicating whether other users are muted.
    /// </summary>
    [JsonIgnore]
    [ObservableProperty]
    public partial bool HasOthersMuted { get; set; } = false;

    /// <summary>
    /// Saves the object instance to a pre-defined application path as a JSON file.
    /// </summary>
    /// <returns>bool = true if successfully saved</returns>
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

    /// <summary>
    /// Loads a user from the disk using JSON deserialization.
    /// </summary>
    /// <returns>A user object if the load is successful, or null if an error occurs.</returns>
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

    private static string GetFilePath() => Path.Combine(StaticLocalPathModule.GetLocalApplicationFolder(), "user.json");

    /// <inheritdoc />
    public override string ToString() => JsonSerializer.Serialize(this);

    /// <summary>
    /// Converts the current User object into a byte array representing a network packet.
    /// </summary>
    /// <returns>A byte array containing the serialized data of the user.</returns>
    public byte[] GetPacket()
    {
        var packet = new DynamicPacketBuilderModule();
        packet.WriteOperationCode((byte)StaticNetCodes.UserCode);
        packet.WriteMessage(JsonSerializer.Serialize(this));
        return packet.GetPacketBytes();
    }

    /// <summary>
    /// Deserializes a JSON string into a User object.
    /// </summary>
    /// <param name="data">The JSON string to deserialize.</param>
    /// <returns>The deserialized User object, or null if the operation fails.</returns>
    public static User? SetFromPacket(string data)
    {
        User? temp = null;
        try
        {
            temp = JsonSerializer.Deserialize<User>(data);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError($"Error during {nameof(temp)} conversion", ex);
            return null;
        }

        return temp ?? null;
    }
}