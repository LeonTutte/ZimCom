using System.Diagnostics.CodeAnalysis;
using System.Text;
using Spectre.Console;

namespace ZimCom.Core.Modules.Dynamic.IO;

/// <summary>
/// Represents a dynamic packet reader module that reads data from a network stream.
/// </summary>
/// <remarks>
/// Use the static methods for single packets with only one payload. Initialize the class to read packets with multiple payloads.
/// </remarks>
public class DynamicPacketReaderModule()
{
    /// <summary>
    /// Represents a memory stream used for dynamic packet reading operations.
    /// </summary>
    /// <remarks>
    /// This variable is required and is initialized with a byte array in the constructor.
    /// Used to read and process data in memory without interacting directly with a physical file.
    /// </remarks>
    public required MemoryStream MemoryStream;

    /// <summary>
    /// Provides a mechanism for reading primitive data types and strings from a stream in a binary format,
    /// used specifically for handling dynamic packet reading in the context of the DynamicPacketReaderModule.
    /// </summary>
    public required BinaryReader BinaryReader;

    /// <summary>
    /// Represents a module that dynamically reads packets from a memory stream.
    /// </summary>
    [SetsRequiredMembers]
    public DynamicPacketReaderModule(byte[] data) : this()
    {
        MemoryStream = new(data);
        BinaryReader = new(MemoryStream);
        // Skip opcode
        BinaryReader.ReadByte();
    }

    /// <summary>
    /// Reads a message from the network stream with a length prefix of 32 bits.
    /// </summary>
    /// <returns>A string representing the decoded message.</returns>
    public string Read32Message()
    {
        var length = BinaryReader.ReadInt32();
        var buffer = new byte[length];
        try
        {
            MemoryStream.ReadExactly(buffer, 0, length);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            throw;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>
    /// Reads a 32-bit length-prefixed message from the specified buffer starting at the given position.
    /// The method extracts the length of the message, calculates the offset, and decodes the message using UTF-8 encoding.
    /// </summary>
    /// <param name="buffer">The byte array containing the message data.</param>
    /// <returns>A string representation of the decoded message.</returns>
    public static string ReadDirect32Message(byte[] buffer)
    {
        var packetReaderModule = new DynamicPacketReaderModule(buffer);
        return packetReaderModule.Read32Message();
    }

    /// <summary>
    /// Reads a segment of data from the provided buffer, starting at the specified position, and returns it as a byte array.
    /// </summary>
    /// <param name="buffer">The byte array containing the data to read from.</param>
    /// <returns>A byte array representing the data segment read from the buffer.</returns>
    public static byte[] ReadDirect32Custom(byte[] buffer)
    {
        var packetReaderModule = new DynamicPacketReaderModule(buffer);
        var length = packetReaderModule.BinaryReader.ReadInt32();
        var result = new byte[length];
        packetReaderModule.MemoryStream.ReadExactly(result, 0, length);
        return result;
    }
}