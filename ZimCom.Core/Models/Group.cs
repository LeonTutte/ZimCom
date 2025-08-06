namespace ZimCom.Core.Models;

public class Group
{
    public required string Label { get; set; }
    public bool IsDefault { get; set; } = false;
    public Dictionary<Strength, long> Strengths { get; set; } = new();
}