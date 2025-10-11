namespace TraincrewDepMelody.Models;

/// <summary>
/// モード種別
/// </summary>
public enum ModeType
{
    Station,
    Vehicle
}

/// <summary>
/// ゲーム状態
/// </summary>
public enum GameStatus
{
    Running,
    Paused,
    Stopped
}

/// <summary>
/// 音声種別
/// </summary>
public enum AudioType
{
    StationMelody,
    StationDoorClosing,
    VehicleMelody,
    VehicleDoorClosing
}

/// <summary>
/// 方向
/// </summary>
public enum Direction
{
    Up,    // 上り (偶数列番)
    Down   // 下り (奇数列番)
}

/// <summary>
/// Topmostモード
/// </summary>
public enum TopmostMode
{
    Always,
    PlayingOnly,
    AtStationOnly,
    None
}