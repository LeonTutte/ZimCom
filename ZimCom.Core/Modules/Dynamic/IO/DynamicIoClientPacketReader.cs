using System.Net.Sockets;
using System.Text;

namespace ZimCom.Core.Modules.Dynamic.IO;
public class DynamicIoClientPacketReader : BinaryReader {
    private NetworkStream _networkStream;
    public DynamicIoClientPacketReader(NetworkStream networkStream) : base(networkStream) {
        _networkStream = networkStream;
    }

    public string ReadMessage() {
        byte[] buffer;
        int length = ReadInt32();
        buffer = new byte[length];
        _networkStream.ReadExactly(buffer, 0, length);

        string data = Encoding.UTF8.GetString(buffer);
        return data;
    }
}
