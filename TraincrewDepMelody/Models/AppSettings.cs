namespace TraincrewDepMelody.Models;

/// <summary>
/// アプリケーション設定
/// </summary>
public class AppSettings
{
    public string CurrentProfile { get; set; } = "profiles/profile_default.csv";
    public string ProfileFile { get; set; } = "profile_default.csv";
    public string StationDefinition { get; set; } = "stations/stations.csv";
    public TopmostMode Topmost { get; set; } = TopmostMode.Always;
    public bool ShowOnPause { get; set; } = true;
    public double Volume { get; set; } = 0.8;
    public bool EnableKeyboard { get; set; } = true;
    public WindowPosition WindowPosition { get; set; } = new WindowPosition { X = 100, Y = 100 };
    public WindowSize WindowSize { get; set; } = new WindowSize { Width = 300, Height = 300 };
    public string? InputKey { get; set; } = "Space";
    public string LogLevel { get; set; } = "Info";
}

public class WindowPosition
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class WindowSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}