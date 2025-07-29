namespace ZimCom.Core.Models;
public class Group {
    public required string Label { get; set; }
    public bool IsDefault { get; set; } = false;
    public Dictionary<Strength, Int64> Strengths { get; set; } = new Dictionary<Strength, Int64>();
}
