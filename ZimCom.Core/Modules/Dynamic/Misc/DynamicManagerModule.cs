using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static.Misc;

namespace ZimCom.Core.Modules.Dynamic.Misc;

/// <summary>
/// Represents a dynamic manager module responsible for handling server and client operations.
/// </summary>
public class DynamicManagerModule
{
    /// <summary>
    /// Indicates whether the instance is operating as a client.
    /// </summary>
    internal bool AsClient;

    /// <summary>
    /// Represents an instance of the client packet reader used to handle incoming data from the server.
    /// </summary>
    protected internal DynamicPacketReaderModule? ClientPacketReader;

    /// <summary>
    /// Represents the port number used for server communications within the dynamic manager module.
    /// </summary>
    protected const int ServerPort = 46112;

    /// <summary>
    /// Represents the port number used for voice communication.
    /// </summary>
    internal const int VoicePort = 46111;

    /// <summary>
    /// Represents a manager module responsible for handling server and client operations.
    /// This class is the base for more specialized modules.
    /// </summary>
    protected DynamicManagerModule(bool asClient = false)
    {
        InternalServer = GetNewServer();
        if (asClient is false) InternalServer = Server.Load() ?? GetNewServer();
        AsClient = asClient;
    }

    /// <summary>
    /// The local server instance
    /// </summary>
    public Server InternalServer { get; }

    /// <summary>
    /// Determines if a user has enough strength to perform an action on a specified channel.
    /// </summary>
    /// <param name="strength">The type of strength being checked.</param>
    /// <param name="user">The user whose strength is being evaluated.</param>
    /// <param name="channel">The channel on which the action is to be performed.</param>
    /// <returns>True if the user's strength is enough for the specified action on the channel; otherwise, false.</returns>
    public bool CheckUserAgainstChannelStrength(Strength strength, User user, Channel channel)
    {
        StaticLogModule.LogDebug($"Checking {user.Label} against {channel.Label} for {strength}");
        if (channel.TitleChannel || channel.SpacerChannel)
            return false;
        long channelStrength, userStrength = 0;
        if (channel.Strengths.Any(x => x.Key.Equals(strength)))
        {
            channelStrength = channel.Strengths.First(x => x.Key.Equals(strength)).Value;
            StaticLogModule.LogDebug($"{channel.Label} has strength {channelStrength} for {strength}");
        }
        else
        {
            StaticLogModule.LogDebug($"{channel.Label} has no strength for {strength} defined!");
            channelStrength = 0;
        }

        if (InternalServer.UserToGroup.Any(x => x.Key.Equals(user.Id)))
        {
            var groupName = InternalServer.UserToGroup.First(x => x.Key.Equals(user.Id)).Key;
            StaticLogModule.LogDebug($"Found group {groupName} for {user.Label}");
            if (InternalServer.Groups.Any(x => x.Label.Equals(groupName)))
            {
                var group = InternalServer.Groups.First(x => x.Label.Equals(groupName));
                if (group.Strengths.Any(x => x.Key.Equals(strength)))
                {
                    userStrength = group.Strengths.First(x => x.Key.Equals(strength)).Value;
                    StaticLogModule.LogDebug($"{group.Label} has strength {userStrength} for {strength}");
                }
                else
                {
                    StaticLogModule.LogDebug($"{group.Label} has no strength for {strength} defined!");
                    userStrength = 0;
                }
            }
            else
            {
                userStrength = 0;
                StaticLogModule.LogDebug($"Could not find {groupName} in group list of Server");
            }
        }
        else
        {
            userStrength = 0;
            StaticLogModule.LogDebug($"{user.Label} has no group defined!");
        }

        if (userStrength >= channelStrength)
        {
            StaticLogModule.LogDebug($"{user.Label} is allowed to {strength} for {channel.Label}");
            return true;
        }

        StaticLogModule.LogDebug($"{user.Label} was denied to {strength} for {channel.Label}");
        return false;
    }

    /// <summary>
    /// Sends a chat message to a specified channel.
    /// </summary>
    /// <param name="chatMessage">The chat message to be sent.</param>
    /// <param name="channel">The channel to which the message is being sent.</param>
    public void SendChannelMessage(ChatMessage chatMessage, Channel channel)
    {
        var matchedChannel = InternalServer.Channels
            .FirstOrDefault(c => c.Label == channel.Label);

        if (matchedChannel is null) return;
        if (CheckUserAgainstChannelStrength(Strength.ChannelChat, chatMessage.User, channel))
            matchedChannel.Chat.Add(chatMessage);
    }

    /// <summary>
    /// Searches for a user within the channels and returns the channel if found.
    /// </summary>
    /// <param name="user">The user to search for within the channels.</param>
    /// <returns>The channel in which the user is found, or null if the user is not found in any channel.</returns>
    public Channel? FindUserInChannel(User user)
    {
        try
        {
            //return Server.Channels.First(x => x.Participants.Contains(user);
            foreach (var channel in InternalServer.Channels.Where(x => x.Participants.Count > 0))
                //if (channel.Participants.Contains(user)) return channel;
                if (channel.Participants.Any(x => x.Id.Equals(user.Id)))
                    return channel;
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError("Could not find user in channel list", ex);
        }

        return null;
    }

    private Dictionary<Strength, long> GetDefaultStrengthSet()
    {
        return new Dictionary<Strength, long>
        {
            { Strength.UserMove, 0 },
            { Strength.UserRemove, 0 },
            { Strength.UserRemovePermanently, 0 },
            { Strength.ChannelAccess, 0 },
            { Strength.ChannelSpeech, 0 },
            { Strength.ChannelChat, 0 },
            { Strength.FileAccess, 0 },
            { Strength.FileUpload, 0 },
            { Strength.FileDownload, 0 },
            { Strength.FileDelete, 0 }
        };
    }

    private Server GetNewServer() => new()
    {
        Id = Guid.NewGuid(),
        Label = "Default Server",
        Channels =
        [
            new Channel
            {
                Label = "Local Computer",
                Description = "You are not connected",
                DefaultChannel = true,
                LocalChannel = true,
                Strengths = GetDefaultStrengthSet()
            }
        ],
        Groups =
        [
            new Group
            {
                Label = "Default Group",
                IsDefault = true,
                Strengths = GetDefaultStrengthSet()
            }
        ],
        UserToGroup = [],
        BannedUsers = [],
        KnownUsers = []
    };
}