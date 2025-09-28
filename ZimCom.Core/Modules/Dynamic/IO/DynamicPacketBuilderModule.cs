using System.Text;

namespace ZimCom.Core.Modules.Dynamic.IO;

/// <summary>
/// Represents a module for dynamically building network packets.
/// </summary>
public class DynamicPacketBuilderModule
{
    /// <summary>
    /// Represents a memory stream used to construct dynamic packets.
    /// </summary>
    private readonly MemoryStream _memoryStream = new();

    /// <summary>
    /// Writes the operation code or identifer to the memory stream.
    /// </summary>
    /// <param name="opcode">The opcode to be written.</param>
    public void WriteOperationCode(byte opcode)
    {
        _memoryStream.WriteByte(opcode);
    }

    /// <summary>
    /// Writes a UTF-8 message to the memory stream.
    /// </summary>
    /// <param name="data">The message or data to be written. Can be a max byte length of 32 bits</param>
    public void WriteMessage(string data)
    {
        _memoryStream.Write(BitConverter.GetBytes(data.Length));
        _memoryStream.Write(Encoding.UTF8.GetBytes(data));
    }

    public void WriteCusomBytes(byte[] data)
    {
        _memoryStream.Write(BitConverter.GetBytes(data.Length));
        _memoryStream.Write(data);
    }

    public void WriteAudioBytes(byte[] data, int bytesRecorded)
    {
        _memoryStream.Write(BitConverter.GetBytes(bytesRecorded));
        _memoryStream.Write(BitConverter.GetBytes(data.Length));
        _memoryStream.Write(data);
    }

    /// <summary>
    /// Retrieves the bytes from the memory stream.
    /// </summary>
    /// <returns>A byte array containing the data written to the memory stream.</returns>
    public byte[] GetPacketBytes()
    {
        return _memoryStream.ToArray();
    }
}