using System.Text.Json.Serialization;

public class HitTarget
{
    public int StartTime { get; set; }
    public int EndTime { get; set; } = -1;
    public HitTargetType Type { get; set; } = HitTargetType.Single;
    public HitTargetDirection[] Directions { get; set; } = System.Array.Empty<HitTargetDirection>();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HitTargetType
{
    Single,
    Hold,
    Spam,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HitTargetDirection
{
    Up,
    Down,
    Left,
    Right,
}
