using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Models;

/// <summary>
/// Represents a server.
/// </summary>
public class Server
{
    /// <summary>
    /// Gets or initializes the unique identifier for the server.
    /// This property is read-only after initialization and serves as a unique key for identifying the server instance.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or initializes the label of the server.
    /// This property is required and must be set during the initialization of a new Server instance.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Retrieves the IPv6 address associated with this server.
    /// </summary>
    /// <returns>An IPAddress object representing the server's IPv6 address. If no IPv6 address is found, it resolves the address from the server's hostname using DNS.</returns>
    public static IPAddress GetV6Address() => GetHostAddress(ServerUrl, AddressFamily.InterNetworkV6);

    internal static IPAddress GetLocalAnyAddress() => IPAddress.IPv6Any;

    /// <summary>
    /// Gets the default hostname of the server.
    /// This property is read-only and its value is determined by the <see cref="GetHostName"/> method.
    /// </summary>
    [JsonIgnore]
    public static string HostName { get; } = GetHostName();

    /// <summary>
    /// Gets or sets the URL of the server.
    /// If not set, the server will use its hostname instead.
    /// This property is used to determine the server's address for network operations.
    /// </summary>
    public static string? ServerUrl { get; set; }

    /// <summary>
    /// Gets the collection of channels associated with the server.
    /// This property is read-only and represents a list of <see cref="Channel"/> objects.
    /// </summary>
    public List<Channel> Channels { get; init; } = [];

    /// <summary>
    /// Gets the collection of groups associated with the server.
    /// This property is read-only and is initialized to an empty list. It represents a set of <see cref="Group"/>
    /// objects that define different categories or classifications within the server context.
    /// </summary>
    public List<Group> Groups { get; init; } = [];

    /// <summary>
    /// Gets a list of users who are banned from the server.
    /// This property is read-only, and its value can be initialized during server creation or modified through dedicated methods.
    /// </summary>
    public List<User> BannedUsers { get; init; } = [];

    /// <summary>
    /// Gets or initializes the list of known users for this server.
    /// This property is read-only after initialization and its value should not be modified directly.
    /// </summary>
    public List<User> KnownUsers { get; init; } = [];

    /// <summary>
    /// Gets a dictionary mapping user identifiers to group names.
    /// This property is read-only and initialized as an empty dictionary.
    /// </summary>
    public Dictionary<string, string> UserToGroup { get; init; } = [];

    /// <inheritdoc />
    public override string ToString() =>
        JsonSerializer.Serialize<Server>(this, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Saves the server configuration to a file.
    /// </summary>
    /// <returns>bool = true if the save operation was successful, otherwise false.</returns>
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
    /// Attempts to load a Server object from the disk.
    /// </summary>
    /// <returns>An instance of Server if successful, otherwise null.</returns>
    public static Server? Load()
    {
        Server? tempServer = null;
        try
        {
            var content = File.ReadAllText(GetFilePath());
            if (!string.IsNullOrEmpty(content)) tempServer = JsonSerializer.Deserialize<Server>(content);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Error during server load", ex);
            return null;
        }

        StaticLogModule.LogDebug("Loaded server from disk");
        return tempServer ?? null;
    }

    private static string GetFilePath()
    {
        return Path.Combine(StaticLocalPathModule.GetLocalApplicationFolder(), "server.json");
    }

    private static string GetHostName()
    {
        try
        {
            return Dns.GetHostName();
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Could not retrieve hostname", ex);
        }

        return "127.0.0.1";
    }

    private static IPAddress GetHostAddress(string? hostname, AddressFamily addressFamily)
    {
        return hostname is null ? IPAddress.IPv6Any : Dns.GetHostAddresses(hostname, addressFamily).First();
    }

    /// <summary>
    /// Creates a network packet containing the server's information.
    /// </summary>
    /// <returns>A byte array representing the constructed network packet.</returns>
    public byte[] GetPacket()
    {
        var packet = new DynamicPacketBuilderModule();
        packet.WriteOperationCode((byte)StaticNetCodes.ServerCode);
        packet.WriteMessage(JsonSerializer.Serialize(this));
        return packet.GetPacketBytes();
    }

    /// <summary>
    /// Deserializes a Server object from the given JSON string.
    /// </summary>
    /// <param name="data">A string containing the JSON representation of the Server object.</param>
    /// <returns>A Server object created from the deserialized data, or null if an error occurs during deserialization.</returns>
    public static Server? SetFromPacket(string data)
    {
        Server? temp;
        try
        {
            temp = JsonSerializer.Deserialize<Server>(data);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError($"Error during {nameof(temp)} conversion", ex);
            return null;
        }

        return temp ?? null;
    }
}