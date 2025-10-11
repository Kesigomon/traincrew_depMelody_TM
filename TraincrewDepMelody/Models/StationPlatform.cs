namespace TraincrewDepMelody.Models;

/// <summary>
/// 駅・番線キー
/// </summary>
public class StationPlatform : IEquatable<StationPlatform>
{
    public string StationName { get; set; } = string.Empty;
    public int Platform { get; set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(StationName, Platform);
    }

    public bool Equals(StationPlatform? other)
    {
        if (other == null) return false;
        return StationName == other.StationName && Platform == other.Platform;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as StationPlatform);
    }
}