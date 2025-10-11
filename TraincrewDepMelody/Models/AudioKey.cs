namespace TraincrewDepMelody.Models;

/// <summary>
/// 音声ファイル キー
/// </summary>
public class AudioKey : IEquatable<AudioKey>
{
    public AudioType Type { get; set; }
    public string? StationName { get; set; }
    public int? Platform { get; set; }
    public bool? IsOdd { get; set; }
    public Direction? Direction { get; set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, StationName, Platform, IsOdd, Direction);
    }

    public bool Equals(AudioKey? other)
    {
        if (other == null) return false;

        return Type == other.Type &&
               StationName == other.StationName &&
               Platform == other.Platform &&
               IsOdd == other.IsOdd &&
               Direction == other.Direction;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as AudioKey);
    }

    public override string ToString()
    {
        return Type switch
        {
            AudioType.StationMelody => $"駅メロディー({StationName} {Platform}番線)",
            AudioType.StationDoorClosing => $"駅ドア締まります({(IsOdd == true ? "奇数" : "偶数")})",
            AudioType.VehicleMelody => $"車両メロディー({Direction})",
            AudioType.VehicleDoorClosing => "車両ドア締まります",
            _ => "Unknown"
        };
    }
}