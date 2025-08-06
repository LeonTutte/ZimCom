using System.Net.Sockets;
using System.Text;

namespace ZimCom.Core.Modules.Dynamic.IO;

public class DynamicIoClientPacketReader(NetworkStream networkStream) : BinaryReader(networkStream)
{
    public string ReadMessage()
    {
        var length = ReadInt32();
        var buffer = new byte[length];
        networkStream.ReadExactly(buffer, 0, length);
        return Encoding.UTF8.GetString(buffer);
    }
}