using System.Net;
using System.Net.Sockets;

using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;

namespace ZimCom.Core.Modules.Dynamic;
public class DynamicManagerModule {
    private Server _server;
    public Server Server {
        get { return _server; }
        set { _server = value; }
    }

    internal bool _asClient = false;
    internal TcpClient _tcpClient;
    internal IPAddress? _address;
    internal int _serverPort = 46112;
    internal int _voicePort = 46111;
    internal int _chatPort = 46113;

    internal DynamicIoClientPacketReader? _clientPacketReader;
    public void SendChannelMessage(ChatMessage chatMessage, Channel channel) {
        Channel? matchedChannel = Server.Channels
                .FirstOrDefault(c => c.Label == channel.Label);

        if (matchedChannel is not null) {
            matchedChannel.Chat.Add(chatMessage);
        }
    }

    public DynamicManagerModule(bool asClient = false) {
        _server = Server.Load() ?? new Server {
            Id = Guid.NewGuid(),
            Label = "Default Server",
            Channels = new List<Channel> {
                new Channel {
                Label = "Local Computer",
                Description = "You are not connected",
                DefaultChannel = true,
                LocalChannel = true,
                Strengths = GetDefaultStrengthSet()
                },
            },
            Groups = new List<Group> {
                new Group {
                    Label = "Default Group",
                    IsDefault = true,
                    Strengths = GetDefaultStrengthSet()
                },
            },
            UserToGroup = new Dictionary<string, string>(),
            BannedUsers = new List<User>(),
            KnownUsers = new List<User>(),
        };
        _asClient = asClient;
        _tcpClient = new TcpClient();
    }

    public Dictionary<Strength, long> GetDefaultStrengthSet() {
        return new Dictionary<Strength, long>() {
                    { Strength.UserMove, 0 },
                    { Strength.UserRemove, 0 },
                    { Strength.UserRemovePermanently, 0 },
                    { Strength.ChannelAccess, 0 },
                    { Strength.ChannelSpeech, 0 },
                    { Strength.ChannelChat, 0 },
                    { Strength.FileAccess, 0 },
                    { Strength.FileUpload, 0 },
                    { Strength.FileDownload, 0 },
                    { Strength.FileDelete, 0 },
                };
    }
}
