namespace ZimCom.Core.Modules.Static.Net;

/// <summary>
/// Contains network related bytecodes.
/// </summary>
public enum StaticNetCodes : byte
{
    RegisterCode,
    UnregisterCode,
    UserCode,
    ServerCode,
    ChatMessageCode,
    ChangeChannel
}