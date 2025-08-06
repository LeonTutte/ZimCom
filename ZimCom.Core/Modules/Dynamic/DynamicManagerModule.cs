using System.Net;
using System.Net.Sockets;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static;

namespace ZimCom.Core.Modules.Dynamic;

public class DynamicManagerModule
{
    internal IPAddress? Address;

    internal bool AsClient;
    internal const int ChatPort = 46113;

    internal DynamicIoClientPacketReader? ClientPacketReader;
    internal const int ServerPort = 46112;
    internal readonly TcpClient TcpClient = new();
    internal const int VoicePort = 46111;

    protected DynamicManagerModule(bool asClient = false)
    {
        Server = Server.Load() ?? new Server
        {
            Id = Guid.NewGuid(),
            Label = "Default Server",
            Channels = new List<Channel>
            {
                new()
                {
                    Label = "Local Computer",
                    Description = "You are not connected",
                    DefaultChannel = true,
                    LocalChannel = true,
                    Strengths = GetDefaultStrengthSet()
                }
            },
            Groups = new List<Group>
            {
                new()
                {
                    Label = "Default Group",
                    IsDefault = true,
                    Strengths = GetDefaultStrengthSet()
                }
            },
            UserToGroup = new Dictionary<string, string>(),
            BannedUsers = new List<User>(),
            KnownUsers = new List<User>()
        };
        AsClient = asClient;
    }

    public Server Server { get; }

    /// <summary>
    ///     Checks if the user strength is enough for an action on a channel
    /// </summary>
    /// <param name="strength"></param>
    /// <param name="user"></param>
    /// <param name="channel"></param>
    /// <returns>A bool if the user is strong enough to perform the action</returns>
    public bool CheckUserAgainstChannelStrength(Strength strength, User user, Channel channel)
    {
        StaticLogModule.LogDebug($"Checking {user.Label} against {channel.Label} for {strength}");
        if (channel.TitleChannel|| channel.SpacerChannel)
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

        if (Server.UserToGroup.Any(x => x.Key.Equals(user.Id)))
        {
            var groupName = Server.UserToGroup.First(x => x.Key.Equals(user.Id)).Key;
            StaticLogModule.LogDebug($"Found group {groupName} for {user.Label}");
            if (Server.Groups.Any(x => x.Label.Equals(groupName)))
            {
                var group = Server.Groups.First(x => x.Label.Equals(groupName));
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
                StaticLogModule.LogDebug($"Could not find {groupName} in grouplist of Server");
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

    public void SendChannelMessage(ChatMessage chatMessage, Channel channel)
    {
        var matchedChannel = Server.Channels
            .FirstOrDefault(c => c.Label == channel.Label);

        if (matchedChannel is null) return;
        if (CheckUserAgainstChannelStrength(Strength.ChannelChat, chatMessage.User, channel))
            matchedChannel.Chat.Add(chatMessage);
    }

    public Channel? FindUserInChannel(User user)
    {
        try
        {
            //return Server.Channels.First(x => x.Participents.Contains(user);
            foreach (var channel in Server.Channels.Where(x => x.Participents.Count > 0))
                //if (channel.Participents.Contains(user)) return channel;
                if (channel.Participents.Any(x => x.Id.Equals(user.Id)))
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
}