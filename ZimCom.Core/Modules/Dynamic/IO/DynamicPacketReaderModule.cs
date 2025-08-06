using System.Net.Sockets;
using System.Text;

namespace ZimCom.Core.Modules.Dynamic.IO;

/// <summary>
/// Represents a dynamic packet reader module that reads data from a network stream.
/// </summary>
public class DynamicPacketReaderModule(NetworkStream networkStream) : BinaryReader(networkStream)
{
    /// <summary>
    /// Reads a message from the network stream with a length prefix of 32 bits.
    /// </summary>
    /// <returns>A string representing the decoded message.</returns>
    public string Read32Message()
    {
        var length = ReadInt32();
        var buffer = new byte[length];
        networkStream.ReadExactly(buffer, 0, length);
        return Encoding.UTF8.GetString(buffer);
    }
}