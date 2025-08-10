namespace ZimCom.Core.Models;

/// <summary>
/// Represents a group within the system, which can have various permissions or capabilities.
/// </summary>
public class Group
{
    /// <summary>
    /// Represents the name of the group.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether the group is default.
    /// </summary>
    public bool IsDefault { get; init; } = false;

    /// <summary>
    /// Gets the dictionary of strengths associated with this group.
    /// </summary>
    /// <remarks>
    /// Each key in the dictionary represents a specific strength (permission or capability) defined by the <see cref="Strength"/> enum, and the corresponding value indicates the level of that strength for the group.
    /// </remarks>
    public Dictionary<Strength, long> Strengths { get; init; } = [];
}