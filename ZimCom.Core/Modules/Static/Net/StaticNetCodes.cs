namespace ZimCom.Core.Modules.Static.Net;

/// <summary>
/// Contains network related bytecodes.
/// </summary>
public enum StaticNetCodes : byte
{
    RegisterCode,
    UserCode,
    ServerCode,
    ChatMessageCode,
    ChangeChannel
}

/// <summary>
/// Contains network related Int64-codes.
/// </summary>
public static class StaticNetLCodes
{
    /// <summary>
    /// Represents the default error code used for stream errors in network communication.
    /// </summary>
    public const long GetDefaultStreamErrorCode = 0x0A;

    /// <summary>
    /// Represents the default error code used for closing connections in network communication.
    /// </summary>
    public const long GetDefaultCloseErrorCode = 0x0B;
}