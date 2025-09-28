using System.IO.Compression;

namespace ZimCom.Core.Modules.Static.Net;

public static class StaticNetCompressor
{
    public static byte[] DeflateCompress(byte[] data)
    {
        MemoryStream output = new();
        using (DeflateStream deflateStream = new(output, CompressionLevel.SmallestSize))
        {
            deflateStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    public static byte[] ZlibCompress(byte[] data)
    {
        MemoryStream output = new();
        using (ZLibStream zLibStream = new(output, CompressionLevel.SmallestSize))
        {
            zLibStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }
    
    public static byte[] BrotliCompress(byte[] data)
    {
        MemoryStream output = new();
        using (BrotliStream brotliStream = new(output, CompressionLevel.SmallestSize))
        {
            brotliStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

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