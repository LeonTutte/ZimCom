using System.Net;

using ZimCom.Core.Models;
using ZimCom.Core.Modules.Dynamic.IO;
using ZimCom.Core.Modules.Static;
using ZimCom.Core.Modules.Static.Net;

namespace ZimCom.Core.Modules.Dynamic;
public class DynamicManagerModuleClientExtras : DynamicManagerModule {
    public DynamicManagerModuleClientExtras() : base(true) {

    }
    public void ConnectToServer(string address) {
        if (!String.IsNullOrWhiteSpace(address)) {
            IPAddress.TryParse(address.AsSpan(), out _address);
        }
        if (_address is not null) {
            if (_tcpClient.Connected is false) {
                try {
                    _tcpClient.Connect(_address, _serverPort);
                } catch (Exception ex) {
                    StaticLogModule.LogError("Error during server connect", ex);
                    StaticNetClientEvents.ConnectedToServerFail?.Invoke(this, ex);
                }
                if (_tcpClient.Connected is true) {
                    StaticNetClientEvents.ConnectedToServer?.Invoke(this, new EventArgs());
                    _clientPacketReader = new IO.DynamicIoClientPacketReader(_tcpClient.GetStream());
                    HandleIncomingServerPackets();
                    AttachToClientEvents();
                }
            }
        }
    }

    private void AttachToClientEvents() {
        StaticNetClientEvents.SendMessageToServer += (sender, e) => {
            if (_tcpClient.Connected is true) {
                _tcpClient.Client.Send(e.GetPacket());
            }
        };
        StaticNetClientEvents.UserChangeChannel += (sender, e) => {
            if (_tcpClient.Connected is true) {
                DynamicIoClientPacket packet = new DynamicIoClientPacket();
                packet.WriteOpCode((byte)StaticNetOpCodes.ChangeChannel);
                packet.WriteMessage(e.Item1.ToString());
                packet.WriteMessage(e.Item2.ToString());
                _tcpClient.Client.Send(packet.GetPacketBytes());
            }
        };
    }

    private void HandleIncomingServerPackets() {
        Task.Run(() => {
            while (_tcpClient.Connected is true && _clientPacketReader is not null) {
                byte opCode = _clientPacketReader.ReadByte();
                switch (opCode) {
                    case (byte)StaticNetOpCodes.ServerCode:
                        StaticNetClientEvents.ReceivedServerData?.Invoke(this, Server.SetFromPacket(_clientPacketReader!.ReadMessage()) ?? throw new Exception("Failed to read data"));
                        break;
                    case (byte)StaticNetOpCodes.ChatMessageCode:
                        StaticNetClientEvents.ReceivedMessageFromServer?.Invoke(this, ChatMessage.SetFromPacket(_clientPacketReader!.ReadMessage()) ?? throw new Exception("Failed to read data"));
                        break;
                    case (byte)StaticNetOpCodes.ChangeChannel:
                        StaticNetClientEvents.OtherUserChangeChannel?.Invoke(this, (User.SetFromPacket(_clientPacketReader!.ReadMessage()), Channel.SetFromPacket(_clientPacketReader!.ReadMessage())));
                        break;
                    default:
                        break;
                }
            }
            StaticNetClientEvents.DisconnectedFromServer?.Invoke(this, new EventArgs());
        });
    }

    public void SendUserInfo(User user) {
        if (_tcpClient.Connected is true) {
            _tcpClient.Client.Send(user.GetPacket());
        }
    }
}
