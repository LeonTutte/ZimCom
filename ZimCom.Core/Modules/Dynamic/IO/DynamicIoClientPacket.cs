using System.Text;

namespace ZimCom.Core.Modules.Dynamic.IO;

public class DynamicIoClientPacket
{
    private readonly MemoryStream _memoryStream = new();

    public void WriteOpCode(byte opcode)
    {
        _memoryStream.WriteByte(opcode);
    }

    public void WriteMessage(string data)
    {
        var dataLength = data.Length;
        _memoryStream.Write(BitConverter.GetBytes(dataLength));
        _memoryStream.Write(Encoding.UTF8.GetBytes(data));
    }

    public byte[] GetPacketBytes()
    {
        return _memoryStream.ToArray();
    }
}