using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Repositories;

/// <summary>
/// 駅・番線リポジトリインターフェース
/// </summary>
public interface IStationRepository
{
    /// <summary>
    /// CSVから駅定義読み込み
    /// </summary>
    void LoadFromCsv(string filePath);

    /// <summary>
    /// 在線駅判定
    /// </summary>
    StationInfo? FindStation(List<string> occupiedTracks);

    /// <summary>
    /// 駅在線判定
    /// </summary>
    bool IsAtStation(List<string> occupiedTracks);
}
