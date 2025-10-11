using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Repositories;

/// <summary>
/// 音声ファイルリポジトリインターフェース
/// </summary>
public interface IAudioRepository
{
    /// <summary>
    /// プロファイル読み込み
    /// </summary>
    void LoadProfile(string profileCsvPath);

    /// <summary>
    /// 駅メロディー取得
    /// </summary>
    string? GetStationMelody(string stationName, int platform, Direction direction);

    /// <summary>
    /// 駅ドア締まりますアナウンス取得
    /// </summary>
    string? GetStationDoorClosing(bool isOddPlatform);

    /// <summary>
    /// 車両メロディー取得
    /// </summary>
    string GetVehicleMelody(Direction direction);

    /// <summary>
    /// 車両ドア締まりますアナウンス取得
    /// </summary>
    string GetVehicleDoorClosing();
}
