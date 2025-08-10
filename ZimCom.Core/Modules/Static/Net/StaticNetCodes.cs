namespace ZimCom.Core.Modules.Static.Net;

/// <summary>
/// Contains network related bytecodes.
/// </summary>
public enum StaticNetCodes : byte
{
    UserCode = 0,
    ServerCode = 1,
    ChatMessageCode = 2,
    ChangeChannel = 3,
    DefaultStreamErrorCode = 0x0A,
    DefaultCloseErrorCode = 0x0B
}