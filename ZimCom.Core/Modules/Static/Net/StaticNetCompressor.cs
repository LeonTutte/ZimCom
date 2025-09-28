using System.IO.Compression;

namespace ZimCom.Core.Modules.Static.Net;

/// <summary>
/// Provides static helper methods for compressing and decompressing byte arrays using
/// various compression algorithms supported by the .NET framework.
/// </summary>
public static class StaticNetCompressor
{
    /// <summary>
    /// Compresses the specified byte array using the Deflate algorithm.
    /// </summary>
    /// <param name="data">The raw data to compress.</param>
    /// <returns>A new byte array containing the compressed data.</returns>
    public static byte[] DeflateCompress(byte[] data)
    {
        MemoryStream output = new();
        using (DeflateStream deflateStream = new(output, CompressionLevel.SmallestSize))
        {
            deflateStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }
    /// <summary>
    /// Compresses the specified byte array using the Zlib algorithm.
    /// </summary>
    /// <param name="data">The raw data to compress.</param>
    /// <returns>A new byte array containing the compressed data.</returns>
    public static byte[] ZlibCompress(byte[] data)
    {
        MemoryStream output = new();
        using (ZLibStream zLibStream = new(output, CompressionLevel.SmallestSize))
        {
            zLibStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }
    /// <summary>
    /// Compresses the specified byte array using the Brotli algorithm.
    /// </summary>
    /// <param name="data">The raw data to compress.</param>
    /// <returns>A new byte array containing the compressed data.</returns>
    public static byte[] BrotliCompress(byte[] data)
    {
        MemoryStream output = new();
        using (BrotliStream brotliStream = new(output, CompressionLevel.SmallestSize))
        {
            brotliStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }
    /// <summary>
    /// Decompresses a byte array that was compressed using the Brotli algorithm.
    /// </summary>
    /// <param name="data">The compressed data to decompress.</param>
    /// <returns>A new byte array containing the decompressed data.</returns>
    public static byte[] BrotliDecompress(byte[] data)
    {
        MemoryStream input = new(data);
        MemoryStream output = new();
        using (BrotliStream brotliStream = new(input, CompressionMode.Decompress))
        {
            brotliStream.CopyTo(output);
        }

        return output.ToArray();
    }
    /// <summary>
    /// Decompresses a byte array that was compressed using the Deflate algorithm.
    /// </summary>
    /// <param name="data">The compressed data to decompress.</param>
    /// <returns>A new byte array containing the decompressed data.</returns>
    public static byte[] DeflateDecompress(byte[] data)
    {
        MemoryStream input = new(data);
        MemoryStream output = new();
        using (DeflateStream deflateStream = new(input, CompressionMode.Decompress))
        {
            deflateStream.CopyTo(output);
        }

        return output.ToArray();
    }
    /// <summary>
    /// Decompresses a byte array that was compressed using the Zlib algorithm.
    /// </summary>
    /// <param name="data">The compressed data to decompress.</param>
    /// <returns>A new byte array containing the decompressed data.</returns>
    public static byte[] ZlibDecompress(byte[] data)
    {
        MemoryStream input = new(data);
        MemoryStream output = new();
        using (ZLibStream zLibStream = new(input, CompressionMode.Decompress))
        {
            zLibStream.CopyTo(output);
        }

        return output.ToArray();
    }
}