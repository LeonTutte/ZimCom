using System.Net;

using ZimCom.Core.Models;

namespace ZimCom.Core.Modules.Dynamic;
public class DynamicServerManagerModule {
    private Server _server;
    public Server Server {
        get { return _server; }
        set { _server = value; }
    }
    private IPAddress? _address;
    public void SendChannelMessage(ChatMessage chatMessage, Channel channel) {
        var matchedChannel = Server.Channels
                .FirstOrDefault(c => c.Label == channel.Label);

        if (matchedChannel != null) {
            matchedChannel.Chat.Add(chatMessage);
        }
    }

    public void ConnectToServer(string address) {
        if (!String.IsNullOrWhiteSpace(address)) {
            IPAddress.TryParse(address.AsSpan(), out _address);
        }
        if (_address is not null) {
            // Connect via TCP and TCP 44112
            // Retrieve Server Object and set as local
            // Invoke event Connected
        }
    }

    public DynamicServerManagerModule() {
        _server = Server.Load() ?? new Server {
            Id = Guid.NewGuid(),
            Label = "Default Server",
            Channels = new List<Channel> {
                new Channel {
                Label = "Local Computer",
                Description = "You are not connected",
                DefaultChannel = true,
                LocalChannel = true
                },
            },
        };
    }
}
