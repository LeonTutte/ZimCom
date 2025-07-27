namespace ZimCom.Core.Models;

public enum Strength : byte {
    UserMove,
    UserRemove,
    UserRemovePermanently,
    ChannelAccess,
    ChannelSpeech,
    ChannelChat,
    FileAccess,
    FileUpload,
    FileDownload,
    FileDelete,
}
