namespace ZimCom.Core.Models;

/// <summary>
/// Represents various permissions or capabilities within the system.
/// </summary>
public enum Strength : byte
{
    /// <summary>
    /// Can a user move another one?
    /// For user interaction.
    /// </summary>
    UserMove,

    /// <summary>
    /// Can a user be removed temporarily?
    /// </summary>
    UserRemove,

    /// <summary>
    /// Can a user be removed permanently?
    /// </summary>
    UserRemovePermanently,

    /// <summary>
    /// Can a channel be accessed?
    /// </summary>
    ChannelAccess,

    /// <summary>
    /// Can voice be used in the channel?
    /// </summary>
    ChannelSpeech,

    /// <summary>
    /// Can a message be sent to the channel chat?
    /// </summary>
    ChannelChat,

    /// <summary>
    /// Can the file section of the channel be accessed?
    /// </summary>
    FileAccess,

    /// <summary>
    /// Can a file be uploaded?
    /// </summary>
    FileUpload,

    /// <summary>
    /// Can a file be downloaded?
    /// </summary>
    FileDownload,

    /// <summary>
    /// Can a file be deleted?
    /// </summary>
    FileDelete
}