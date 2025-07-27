namespace ZimCom.Core.Modules.Static;
public static class StaticPacketConverter {
    public static byte[] GetHelloPacket() => [1, 0, 0, 0];
    public static byte[] GetChannelRequestPacket() => [1, 0, 0, 1];
    public static byte[] GetChannelReceivePacket(byte[] bytes) => CombineBytes([1, 0, 0, 1], bytes);

    private static byte[] CombineBytes(byte[] bytes, byte[] bytes2) {
        Array.Copy(bytes2, bytes, bytes2.Length);
        return bytes;
    }
}
